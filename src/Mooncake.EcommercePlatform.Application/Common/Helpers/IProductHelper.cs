namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for Product, Variant, and Gallery Image mapping helpers.</summary>
public interface IProductHelper
{
    ProductResponse ToResponse(Product product);
    ProductDetailResponse ToDetailResponse(Product product);
    ProductVariantResponse ToVariantResponse(ProductVariant variant);
    ProductImageResponse ToImageResponse(ProductImage image);
}
