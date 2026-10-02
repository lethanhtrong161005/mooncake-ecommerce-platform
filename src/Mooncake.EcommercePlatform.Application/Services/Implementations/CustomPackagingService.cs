namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Implements custom packaging creation and management.</summary>
public class CustomPackagingService(
    ICustomPackagingRepository packagingRepository,
    ICustomerRepository customerRepository,
    ICustomPackagingHelper packagingHelper) : ICustomPackagingService
{
    public async Task<IEnumerable<CustomPackagingResponse>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var list = await packagingRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return list.Select(packagingHelper.ToResponse);
    }

    public async Task<CustomPackagingResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var packaging = await packagingRepository.GetByIdAsync(id, cancellationToken)
                        ?? throw new HttpException(404, $"Custom packaging with id '{id}' was not found.");
        return packagingHelper.ToResponse(packaging);
    }

    public async Task<CustomPackagingResponse> CreateAsync(CreateCustomPackagingRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
                       ?? throw new HttpException(404, $"Customer with id '{request.CustomerId}' was not found.");

        var packaging = new CustomPackaging
        {
            CustomerId = request.CustomerId,
            Name = request.Name,
            LogoUrl = request.LogoUrl,
            DesignNotes = request.DesignNotes,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await packagingRepository.CreateAsync(packaging, cancellationToken);
        return packagingHelper.ToResponse(created);
    }

    public async Task<CustomPackagingResponse> UpdateAsync(long id, UpdateCustomPackagingRequest request, CancellationToken cancellationToken = default)
    {
        var packaging = await packagingRepository.GetByIdAsync(id, cancellationToken)
                        ?? throw new HttpException(404, $"Custom packaging with id '{id}' was not found.");

        packaging.Name = request.Name;
        packaging.LogoUrl = request.LogoUrl;
        packaging.DesignNotes = request.DesignNotes;
        packaging.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await packagingRepository.UpdateAsync(packaging, cancellationToken);
        return packagingHelper.ToResponse(updated);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var packaging = await packagingRepository.GetByIdAsync(id, cancellationToken)
                        ?? throw new HttpException(404, $"Custom packaging with id '{id}' was not found.");

        await packagingRepository.DeleteAsync(packaging.Id, cancellationToken);
    }
}
