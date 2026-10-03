namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>
/// Implements multi-shop order splitting, price and promotion calculations,
/// inventory stock mutation, order lifecycle transitions, and bulk order deposit handling.
/// </summary>
public class OrderService(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IShopRepository shopRepository,
    IProductRepository productRepository,
    IPromotionRuleRepository promotionRuleRepository,
    ICustomPackagingRepository customPackagingRepository,
    INotificationRepository notificationRepository,
    IOrderHelper orderHelper) : IOrderService
{
    private const decimal StandardShippingFeePerShop = 30000m;
    private const int BulkOrderQuantityThreshold = 50;
    private const decimal BulkOrderAmountThreshold = 5000000m;
    private const decimal BulkOrderDepositRate = 0.30m; // 30% deposit for large orders

    public async Task<CalculateOrderResponse> CalculateAsync(CalculateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new HttpException(400, "Cart is empty. Please add items to calculate order totals.");
        }

        var shopOrderCalculations = await CalculateInternalAsync(request.Items, cancellationToken);

        var grandSubtotal = shopOrderCalculations.Sum(s => s.Subtotal);
        var grandDiscountTotal = shopOrderCalculations.Sum(s => s.DiscountTotal);
        var grandShippingFee = shopOrderCalculations.Sum(s => s.ShippingFee);
        var grandTotalAmount = shopOrderCalculations.Sum(s => s.TotalAmount);
        var grandDepositRequired = shopOrderCalculations.Sum(s => s.DepositRequired);

        return new CalculateOrderResponse(
            shopOrderCalculations,
            grandSubtotal,
            grandDiscountTotal,
            grandShippingFee,
            grandTotalAmount,
            grandDepositRequired
        );
    }

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutOrderRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
                       ?? throw new HttpException(404, $"Customer with id '{request.CustomerId}' was not found.");

        if (request.Items.Count == 0)
        {
            throw new HttpException(400, "Cannot checkout an empty cart.");
        }

        // 1. Calculate pricing, packaging fees, and promotion rules grouped per shop
        var shopCalculations = await CalculateInternalAsync(request.Items, cancellationToken);

        // 2. Validate stock availability for all items before any mutation
        foreach (var shopCalc in shopCalculations)
        {
            foreach (var item in shopCalc.Items)
            {
                var variant = await productRepository.GetVariantByIdAsync(item.VariantId, cancellationToken)
                              ?? throw new HttpException(404, $"Variant with id '{item.VariantId}' was not found.");

                if (variant.StockQuantity < item.Quantity)
                {
                    throw new HttpException(400, $"Insufficient stock for '{variant.Name}'. Available: {variant.StockQuantity}, Requested: {item.Quantity}.");
                }
            }
        }

        // 3. Create a separate Order for each shop (Multi-shop order splitting)
        var createdOrders = new List<OrderResponse>();

        foreach (var shopCalc in shopCalculations)
        {
            var initialStatus = shopCalc.DepositRequired > 0 ? OrderStatus.AwaitingDeposit : OrderStatus.Pending;

            var order = new Order
            {
                CustomerId = request.CustomerId,
                ShopId = shopCalc.ShopId,
                Status = initialStatus,
                Subtotal = shopCalc.Subtotal,
                DiscountTotal = shopCalc.DiscountTotal,
                ShippingFee = shopCalc.ShippingFee,
                TotalAmount = shopCalc.TotalAmount,
                DepositRequired = shopCalc.DepositRequired,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                ShippingAddress = request.ShippingAddress,
                RequiredDeliveryDate = request.RequiredDeliveryDate,
                Note = request.Note,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            // Create OrderItems and deduct stock
            foreach (var itemCalc in shopCalc.Items)
            {
                var variant = await productRepository.GetVariantByIdAsync(itemCalc.VariantId, cancellationToken)!;
                variant!.StockQuantity -= itemCalc.Quantity;
                variant.UpdatedAtUtc = DateTime.UtcNow;
                await productRepository.UpdateVariantAsync(variant, cancellationToken);

                var orderItem = new OrderItem
                {
                    VariantId = itemCalc.VariantId,
                    PromotionRuleId = itemCalc.PromotionRuleId,
                    CustomPackagingId = itemCalc.CustomPackagingId,
                    Quantity = itemCalc.Quantity,
                    UnitPrice = itemCalc.UnitPrice,
                    PackagingFee = itemCalc.PackagingFee,
                    DiscountAmount = itemCalc.DiscountAmount,
                    LineTotal = itemCalc.LineTotal,
                    CreatedAtUtc = DateTime.UtcNow
                };

                order.OrderItems.Add(orderItem);
            }

            var savedOrder = await orderRepository.CreateOrderAsync(order, cancellationToken);
            var loadedOrder = await orderRepository.GetByIdAsync(savedOrder.Id, cancellationToken);
            var orderResponse = orderHelper.ToResponse(loadedOrder ?? savedOrder);
            createdOrders.Add(orderResponse);

            // Send notification to customer
            await notificationRepository.CreateAsync(new Notification
            {
                UserId = customer.UserId,
                Type = "order_placed",
                Title = $"Order #{savedOrder.Id} Placed Successfully",
                Body = $"Your order from {shopCalc.ShopName} for {shopCalc.TotalAmount:N0} VND has been placed. Current status: {initialStatus}.",
                EntityType = "order",
                EntityId = savedOrder.Id,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            // Send notification to shop supplier
            var shop = await shopRepository.GetByIdAsync(shopCalc.ShopId, cancellationToken);
            if (shop?.Supplier is not null)
            {
                await notificationRepository.CreateAsync(new Notification
                {
                    UserId = shop.Supplier.UserId,
                    Type = "new_order_received",
                    Title = $"New Order #{savedOrder.Id} Received",
                    Body = $"You have a new order from {request.ReceiverName} totaling {shopCalc.TotalAmount:N0} VND.",
                    EntityType = "order",
                    EntityId = savedOrder.Id,
                    CreatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        var grandTotal = createdOrders.Sum(o => o.TotalAmount);
        var grandDeposit = createdOrders.Sum(o => o.DepositRequired);

        return new CheckoutResponse(
            createdOrders,
            createdOrders.Count,
            grandTotal,
            grandDeposit
        );
    }

    public async Task<IEnumerable<OrderResponse>> GetOrdersAsync(long? customerId = null, long? shopId = null, OrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetOrdersAsync(customerId, shopId, status, cancellationToken);
        return orders.Select(orderHelper.ToResponse);
    }

    public async Task<OrderResponse> GetOrderByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new HttpException(404, $"Order with id '{id}' was not found.");
        return orderHelper.ToResponse(order);
    }

    public async Task<OrderResponse> UpdateStatusAsync(long id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new HttpException(404, $"Order with id '{id}' was not found.");

        ValidateStatusTransition(order.Status, request.Status);

        // If order is cancelled, restore stock for all variants
        if (request.Status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            foreach (var item in order.OrderItems)
            {
                var variant = await productRepository.GetVariantByIdAsync(item.VariantId, cancellationToken);
                if (variant is not null)
                {
                    variant.StockQuantity += item.Quantity;
                    variant.UpdatedAtUtc = DateTime.UtcNow;
                    await productRepository.UpdateVariantAsync(variant, cancellationToken);
                }
            }
        }

        order.Status = request.Status;
        order.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await orderRepository.UpdateOrderAsync(order, cancellationToken);

        // Notify customer of status change
        if (order.Customer is not null)
        {
            await notificationRepository.CreateAsync(new Notification
            {
                UserId = order.Customer.UserId,
                Type = "order_status_updated",
                Title = $"Order #{order.Id} Status Updated",
                Body = $"Your order status is now: {request.Status}." + (string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : $" Reason: {request.Reason}"),
                EntityType = "order",
                EntityId = order.Id,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        return orderHelper.ToResponse(updated);
    }

    public async Task<OrderResponse> PayDepositAsync(long id, PayDepositRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken)
                    ?? throw new HttpException(404, $"Order with id '{id}' was not found.");

        if (order.Status != OrderStatus.AwaitingDeposit)
        {
            throw new HttpException(400, $"Order #{id} is in status '{order.Status}' and does not require deposit.");
        }

        if (request.Amount < order.DepositRequired)
        {
            throw new HttpException(400, $"Deposit amount ({request.Amount:N0} VND) is less than required ({order.DepositRequired:N0} VND).");
        }

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await orderRepository.UpdateOrderAsync(order, cancellationToken);

        if (order.Customer is not null)
        {
            await notificationRepository.CreateAsync(new Notification
            {
                UserId = order.Customer.UserId,
                Type = "deposit_confirmed",
                Title = $"Deposit Confirmed for Order #{order.Id}",
                Body = $"Your deposit of {request.Amount:N0} VND has been verified. Order is now Confirmed.",
                EntityType = "order",
                EntityId = order.Id,
                CreatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        return orderHelper.ToResponse(updated);
    }

    private async Task<List<CalculatedShopOrderResponse>> CalculateInternalAsync(List<CartItemRequest> items, CancellationToken cancellationToken)
    {
        // 1. Gather all variant, product, and shop details
        var enrichedItems = new List<(CartItemRequest Request, ProductVariant Variant, Product Product, CustomPackaging? Packaging)>();

        foreach (var item in items)
        {
            var variant = await productRepository.GetVariantByIdAsync(item.VariantId, cancellationToken)
                          ?? throw new HttpException(404, $"Product variant with id '{item.VariantId}' was not found.");

            var product = await productRepository.GetByIdAsync(variant.ProductId, cancellationToken)
                          ?? throw new HttpException(404, $"Product for variant '{variant.Name}' was not found.");

            CustomPackaging? packaging = null;
            if (item.CustomPackagingId.HasValue)
            {
                packaging = await customPackagingRepository.GetByIdAsync(item.CustomPackagingId.Value, cancellationToken)
                            ?? throw new HttpException(404, $"Custom packaging with id '{item.CustomPackagingId.Value}' was not found.");
            }

            enrichedItems.Add((item, variant, product, packaging));
        }

        // 2. Group items by ShopId
        var shopGroups = enrichedItems.GroupBy(x => x.Product.ShopId);
        var result = new List<CalculatedShopOrderResponse>();

        foreach (var group in shopGroups)
        {
            var shopId = group.Key;
            var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken)
                       ?? throw new HttpException(404, $"Shop with id '{shopId}' was not found.");

            // Fetch active promotions for this shop
            var activePromos = (await promotionRuleRepository.GetByShopIdAsync(shopId, activeOnly: true, cancellationToken)).ToList();

            var calculatedItems = new List<CalculatedItemResponse>();
            var shopTotalQuantity = 0;

            foreach (var (req, variant, product, packaging) in group)
            {
                shopTotalQuantity += req.Quantity;

                var unitPrice = variant.Price;
                var packagingFee = packaging is not null ? (product.CustomPackagingFee ?? 0) : 0;
                var baseItemTotal = req.Quantity * (unitPrice + packagingFee);

                // Find matching promotion rule for product or shop where quantity >= minQuantity
                var matchingRule = activePromos
                    .Where(r => (r.ProductId == null || r.ProductId == product.Id) && req.Quantity >= r.MinQuantity)
                    .OrderByDescending(r => r.MinQuantity)
                    .FirstOrDefault();

                decimal discountAmount = 0;
                if (matchingRule is not null)
                {
                    discountAmount = matchingRule.DiscountType switch
                    {
                        DiscountType.Percent => Math.Round(req.Quantity * unitPrice * (matchingRule.DiscountPercent!.Value / 100m), 2),
                        DiscountType.FixedAmount => Math.Min(matchingRule.DiscountAmount!.Value, baseItemTotal),
                        DiscountType.BuyXGetY => Math.Min(matchingRule.FreeQuantity!.Value * unitPrice, baseItemTotal),
                        _ => 0
                    };
                }

                // Ensure lineTotal matches DB constraint: line_total = quantity * (unit_price + packaging_fee) - discount_amount
                var lineTotal = baseItemTotal - discountAmount;

                calculatedItems.Add(new CalculatedItemResponse(
                    variant.Id,
                    variant.Name,
                    variant.Sku,
                    product.Id,
                    product.Name,
                    shop.Id,
                    shop.Name,
                    req.Quantity,
                    unitPrice,
                    packagingFee,
                    matchingRule?.Id,
                    matchingRule?.Name,
                    discountAmount,
                    lineTotal,
                    packaging?.Id,
                    packaging?.Name
                ));
            }

            var subtotal = calculatedItems.Sum(i => i.Quantity * (i.UnitPrice + i.PackagingFee));
            var discountTotal = calculatedItems.Sum(i => i.DiscountAmount);
            var shippingFee = StandardShippingFeePerShop;
            var totalAmount = subtotal - discountTotal + shippingFee;

            // Bulk order detection
            var isBulkOrder = shopTotalQuantity >= BulkOrderQuantityThreshold || totalAmount >= BulkOrderAmountThreshold;
            var depositRequired = isBulkOrder ? Math.Round(totalAmount * BulkOrderDepositRate, 2) : 0m;

            result.Add(new CalculatedShopOrderResponse(
                shop.Id,
                shop.Name,
                calculatedItems,
                subtotal,
                discountTotal,
                shippingFee,
                totalAmount,
                depositRequired,
                isBulkOrder
            ));
        }

        return result;
    }

    private static void ValidateStatusTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next) return;

        var isValid = (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.AwaitingDeposit, OrderStatus.Confirmed) => true,
            (OrderStatus.AwaitingDeposit, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.Preparing, OrderStatus.Shipping) => true,
            (OrderStatus.Preparing, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipping, OrderStatus.Delivered) => true,
            (OrderStatus.Delivered, OrderStatus.Completed) => true,
            _ => false
        };

        if (!isValid)
        {
            throw new HttpException(400, $"Invalid order status transition from '{current}' to '{next}'.");
        }
    }
}
