namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using System.Text.RegularExpressions;

/// <summary>Implements bulk ordering and order status transitions.</summary>
public sealed class OrderService(IOrderRepository repository, IDateTimeProvider dateTimeProvider) : IOrderService
{
    public async Task<OrderQuotePreviewResponse> PreviewQuoteAsync(OrderQuotePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is not { Count: >= 1 and <= 100 })
            throw new HttpException(400, "A quote must contain between 1 and 100 items.");
        if (request.Items.Select(item => item.VariantId).Distinct().Count() != request.Items.Count)
            throw new HttpException(400, "Each product variant can appear only once in a quote.");
        var shop = await repository.GetShopAsync(request.ShopId, cancellationToken)
                   ?? throw new HttpException(404, "Shop was not found.");
        if (shop.Status != ShopStatus.Active)
            throw new HttpException(409, "The shop is not accepting orders.");
        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToArray();
        var variantIds = request.Items.Select(item => item.VariantId).Distinct().ToArray();
        var products = await repository.GetProductsAsync(productIds, cancellationToken);
        var variants = await repository.GetVariantsAsync(variantIds, cancellationToken);
        if (products.Count != productIds.Length || variants.Count != variantIds.Length)
            throw new HttpException(400, "One or more selected products or variants are unavailable.");

        var productsById = products.ToDictionary(product => product.Id);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        var promotions = await repository.GetActivePromotionsAsync(shop.Id, cancellationToken);
        var lines = new List<OrderQuoteLineResponse>(request.Items.Count);
        decimal subtotal = 0;
        decimal totalDiscount = 0;
        foreach (var item in request.Items)
        {
            var product = productsById[item.ProductId];
            var variant = variantsById[item.VariantId];
            ValidateOrderLine(shop.Id, product, variant, item.Quantity);
            var pricing = PromotionPricingHelper.Calculate(product.BasePrice + variant.PriceAdjustment,
                item.Quantity, product.Id, promotions, dateTimeProvider.UtcNow);
            var lineSubtotal = decimal.Round(pricing.OriginalUnitPrice * item.Quantity, 2,
                MidpointRounding.AwayFromZero);
            var lineTotal = lineSubtotal - pricing.TotalDiscount;
            subtotal += lineSubtotal;
            totalDiscount += pricing.TotalDiscount;
            lines.Add(new OrderQuoteLineResponse(product.Id, variant.Id, product.Name, variant.Name,
                variant.Sku, item.Quantity, pricing.OriginalUnitPrice, pricing.DiscountedUnitPrice,
                lineSubtotal, pricing.TotalDiscount, lineTotal,
                pricing.AppliedRule is null ? null : new AppliedPromotionResponse(pricing.AppliedRule.Id,
                    pricing.AppliedRule.Name, pricing.AppliedRule.DiscountType,
                    pricing.AppliedRule.DiscountValue)));
        }

        subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        totalDiscount = decimal.Round(totalDiscount, 2, MidpointRounding.AwayFromZero);
        var finalTotal = subtotal - totalDiscount;
        var effectiveRate = subtotal == 0 ? 0 : decimal.Round(totalDiscount / subtotal * 100, 2,
            MidpointRounding.AwayFromZero);
        return new OrderQuotePreviewResponse(subtotal, totalDiscount, finalTotal, effectiveRate, lines);
    }

    public async Task<OrderResponse> CreateAsync(Guid customerId, string? idempotencyKey, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new HttpException(400, "Idempotency-Key header is required.");
        var normalizedIdempotencyKey = idempotencyKey.Trim();
        if (!Regex.IsMatch(normalizedIdempotencyKey, "^[A-Za-z0-9._:-]{8,128}$", RegexOptions.CultureInvariant))
            throw new HttpException(400, "Idempotency-Key must contain 8 to 128 letters, digits, dots, underscores, colons, or hyphens.");
        var requestHash = IdempotencyRequestHelper.CreateHash(request);
        var previousOrder = await repository.GetByIdempotencyKeyAsync(customerId, normalizedIdempotencyKey, cancellationToken);
        if (previousOrder is not null)
        {
            if (!string.Equals(previousOrder.IdempotencyRequestHash, requestHash, StringComparison.Ordinal))
                throw new HttpException(409, "This Idempotency-Key was already used with a different order request.");
            return await ToResponseAsync(previousOrder, cancellationToken);
        }

        if (request.Items.Select(item => item.VariantId).Distinct().Count() != request.Items.Count)
            throw new HttpException(400, "Each product variant can appear only once in an order.");
        if (request.DeliveryDateExpected < DateOnly.FromDateTime(dateTimeProvider.UtcNow))
            throw new HttpException(400, "Expected delivery date cannot be in the past.");

        var shop = await repository.GetShopAsync(request.ShopId, cancellationToken)
                   ?? throw new HttpException(404, "Shop was not found.");
        if (shop.Status != ShopStatus.Active)
            throw new HttpException(409, "The shop is not accepting orders.");

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToArray();
        var variantIds = request.Items.Select(item => item.VariantId).Distinct().ToArray();
        var products = await repository.GetProductsAsync(productIds, cancellationToken);
        var variants = await repository.GetVariantsAsync(variantIds, cancellationToken);
        if (products.Count != productIds.Length || variants.Count != variantIds.Length)
            throw new HttpException(400, "One or more selected products or variants are unavailable.");

        var productsById = products.ToDictionary(product => product.Id);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        var promotions = await repository.GetActivePromotionsAsync(shop.Id, cancellationToken);
        var now = dateTimeProvider.UtcNow;
        var items = new List<OrderItem>(request.Items.Count);
        decimal subtotal = 0;
        decimal discountTotal = 0;

        foreach (var requestedItem in request.Items)
        {
            var product = productsById[requestedItem.ProductId];
            var variant = variantsById[requestedItem.VariantId];
            ValidateOrderLine(shop.Id, product, variant, requestedItem.Quantity);

            var pricing = PromotionPricingHelper.Calculate(product.BasePrice + variant.PriceAdjustment,
                requestedItem.Quantity, product.Id, promotions, now);
            var unitPrice = pricing.OriginalUnitPrice;
            var lineTotal = decimal.Round(unitPrice * requestedItem.Quantity, 2, MidpointRounding.AwayFromZero);
            var lineDiscount = pricing.TotalDiscount;
            subtotal += lineTotal;
            discountTotal += lineDiscount;
            items.Add(new OrderItem
            {
                ProductId = product.Id,
                VariantId = variant.Id,
                ProductNameSnapshot = product.Name,
                VariantNameSnapshot = variant.Name,
                VariantSkuSnapshot = variant.Sku,
                Quantity = requestedItem.Quantity,
                UnitPrice = unitPrice,
                PromotionRuleId = pricing.AppliedRule?.Id,
                DiscountAmount = lineDiscount,
                Subtotal = lineTotal - lineDiscount
            });
        }

        subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        discountTotal = decimal.Round(discountTotal, 2, MidpointRounding.AwayFromZero);
        var order = new Order
        {
            CustomerId = customerId,
            ShopId = shop.Id,
            IdempotencyKey = normalizedIdempotencyKey,
            IdempotencyRequestHash = requestHash,
            OrderNumber = $"MC-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
            Subtotal = subtotal,
            DiscountAmount = discountTotal,
            TaxAmount = 0,
            ShippingFee = 0,
            PackagingFee = 0,
            TotalAmount = subtotal - discountTotal,
            Notes = request.Notes?.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(),
            DeliveryDateExpected = request.DeliveryDateExpected,
            Status = OrderStatus.Pending,
            CreatedBy = customerId
        };
        if (!await repository.CreateOrderAsync(order, items, cancellationToken))
        {
            previousOrder = await repository.GetByIdempotencyKeyAsync(customerId, normalizedIdempotencyKey, cancellationToken);
            if (previousOrder is not null)
            {
                if (!string.Equals(previousOrder.IdempotencyRequestHash, requestHash, StringComparison.Ordinal))
                    throw new HttpException(409, "This Idempotency-Key was already used with a different order request.");
                return await ToResponseAsync(previousOrder, cancellationToken);
            }
            throw new HttpException(409, "Inventory changed while placing the order. Review the quantities and try again.");
        }
        return OrderResponseHelper.ToResponse(order, items);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default)
    {
        var orders = supplier
            ? await repository.GetSupplierOrdersAsync(userId, cancellationToken)
            : await repository.GetCustomerOrdersAsync(userId, cancellationToken);
        var packagingByOrder = await repository.GetPackagingSummariesAsync(
            orders.Select(order => order.Id).ToArray(), cancellationToken);
        var results = new List<OrderResponse>(orders.Count);
        foreach (var order in orders)
        {
            packagingByOrder.TryGetValue(order.Id, out var summary);
            results.Add(OrderResponseHelper.ToResponse(order,
                await repository.GetOrderItemsAsync(order.Id, cancellationToken), summary.Packaging,
                summary.RevisionCount));
        }
        return results;
    }

    public async Task<OrderResponse> GetAsync(Guid userId, bool supplier, Guid orderId, bool admin = false, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, "Order was not found.");
        var authorized = admin || (supplier
            ? await repository.IsShopOwnedByUserAsync(order.ShopId, userId, cancellationToken)
            : order.CustomerId == userId);
        if (!authorized)
            throw new HttpException(404, "Order was not found.");
        return await ToResponseAsync(order, cancellationToken);
    }

    public async Task<OrderResponse> CancelAsync(Guid customerId, Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, "Order was not found.");
        if (order.CustomerId != customerId)
            throw new HttpException(404, "Order was not found.");
        if (order.Status != OrderStatus.Pending)
            throw new HttpException(409, "Only pending orders can be cancelled.");
        if (!await repository.CancelPendingOrderAsync(order, customerId, request.Reason.Trim(), cancellationToken))
            throw new HttpException(409, "The order status changed and it can no longer be cancelled.");
        return await ToResponseAsync(order, cancellationToken);
    }

    public async Task<OrderResponse> ConfirmAsync(Guid supplierUserId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, "Order was not found.");
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, "Order was not found.");
        if (order.Status != OrderStatus.Pending)
            throw new HttpException(409, "Only pending orders can be confirmed.");
        if (!await repository.ConfirmPendingOrderAsync(order, supplierUserId, cancellationToken))
            throw new HttpException(409, "The order status changed and it can no longer be confirmed.");
        return await ToResponseAsync(order, cancellationToken);
    }

    public async Task<OrderResponse> RejectAsync(Guid supplierUserId, Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, "Order was not found.");
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, "Order was not found.");
        if (order.Status != OrderStatus.Pending)
            throw new HttpException(409, "Only pending orders can be rejected.");
        if (!await repository.CancelPendingOrderAsync(order, supplierUserId, $"Supplier rejected: {request.Reason.Trim()}", cancellationToken))
            throw new HttpException(409, "The order status changed and it can no longer be rejected.");
        return await ToResponseAsync(order, cancellationToken);
    }

    public async Task<OrderResponse> StartProcessingAsync(Guid supplierUserId, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var packagingStatus = await repository.GetCustomPackagingStatusAsync(orderId, cancellationToken);
        if (packagingStatus.HasValue && packagingStatus != CustomPackagingStatus.Approved)
            throw new HttpException(409,
                Mooncake.EcommercePlatform.Application.Common.Constant.B2BOrderingMessages.PackagingProductionBlocked);
        return await TransitionAsSupplierAsync(supplierUserId, orderId, OrderStatus.Confirmed,
            OrderStatus.Processing, cancellationToken);
    }

    public Task<OrderResponse> MarkShippedAsync(Guid supplierUserId, Guid orderId, CancellationToken cancellationToken = default) =>
        TransitionAsSupplierAsync(supplierUserId, orderId, OrderStatus.Processing, OrderStatus.Shipped, cancellationToken);

    public async Task<OrderResponse> ConfirmDeliveryAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, "Order was not found.");
        if (order.CustomerId != customerId) throw new HttpException(404, "Order was not found.");
        if (order.Status != OrderStatus.Shipped) throw new HttpException(409, "Only shipped orders can be marked delivered.");
        if (!await repository.TransitionOrderAsync(orderId, OrderStatus.Shipped, OrderStatus.Delivered, customerId, cancellationToken))
            throw new HttpException(409, "The order status changed and delivery could not be confirmed.");
        order.Status = OrderStatus.Delivered;
        return await ToResponseAsync(order, cancellationToken);
    }

    private async Task<OrderResponse> TransitionAsSupplierAsync(Guid supplierUserId, Guid orderId, OrderStatus expectedStatus,
        OrderStatus nextStatus, CancellationToken cancellationToken)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, "Order was not found.");
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, "Order was not found.");
        if (order.Status != expectedStatus) throw new HttpException(409, $"Only {expectedStatus} orders can move to {nextStatus}.");
        if (!await repository.TransitionOrderAsync(orderId, expectedStatus, nextStatus, supplierUserId, cancellationToken))
            throw new HttpException(409, "The order status changed and could not be updated.");
        order.Status = nextStatus;
        return await ToResponseAsync(order, cancellationToken);
    }

    private async Task<OrderResponse> ToResponseAsync(Order order, CancellationToken cancellationToken)
    {
        var summaries = await repository.GetPackagingSummariesAsync([order.Id], cancellationToken);
        summaries.TryGetValue(order.Id, out var summary);
        return OrderResponseHelper.ToResponse(order,
            await repository.GetOrderItemsAsync(order.Id, cancellationToken), summary.Packaging,
            summary.RevisionCount);
    }

    private static void ValidateOrderLine(Guid shopId, Product product, ProductVariant variant, int quantity)
    {
        if (product.ShopId != shopId || variant.ProductId != product.Id ||
            product.Status != ProductStatus.Active || product.IsDeleted || variant.IsDeleted)
            throw new HttpException(400,
                "All ordered variants must belong to active products in the selected shop.");
        if (quantity < product.MinOrderQty || product.MaxOrderQty is int maxQuantity && quantity > maxQuantity)
            throw new HttpException(400, $"Quantity for '{product.Name}' is outside the allowed order range.");
        if (variant.StockQty < quantity)
            throw new HttpException(409, $"Insufficient stock for variant '{variant.Sku}'.");
    }
}
