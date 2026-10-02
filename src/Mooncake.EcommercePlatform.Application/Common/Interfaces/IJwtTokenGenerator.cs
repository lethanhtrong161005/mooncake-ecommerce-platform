namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for generating authentication tokens.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Generates a JWT bearer token for the specified user.</summary>
    string GenerateToken(User user, long? customerId = null, long? supplierId = null);
}
