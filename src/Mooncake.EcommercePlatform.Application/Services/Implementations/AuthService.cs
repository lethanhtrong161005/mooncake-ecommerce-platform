namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Auth.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements registration, login, and authentication flows.</summary>
public class AuthService(
    IUserRepository userRepository,
    ICustomerRepository customerRepository,
    ISupplierRepository supplierRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IUserHelper userHelper) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new HttpException(409, $"A user with email '{request.Email}' already exists.");
        }

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = request.Role,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var createdUser = await userRepository.CreateAsync(user, cancellationToken);
        long? customerId = null;
        long? supplierId = null;

        if (request.Role == UserRole.Customer)
        {
            var customer = new Customer
            {
                UserId = createdUser.Id,
                CustomerType = request.CustomerType,
                CompanyName = request.CustomerType == CustomerType.Company ? (request.CompanyName ?? request.FullName) : request.CompanyName,
                TaxCode = request.TaxCode,
                DefaultAddress = request.DefaultAddress,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var createdCustomer = await customerRepository.CreateAsync(customer, cancellationToken);
            customerId = createdCustomer.Id;
        }
        else if (request.Role == UserRole.Supplier)
        {
            var supplier = new Supplier
            {
                UserId = createdUser.Id,
                BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? request.FullName : request.BusinessName,
                Description = request.Description,
                Address = request.Address,
                TaxCode = request.TaxCode,
                IsVerified = false,
                ReputationScore = 100, // Initial reputation score
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var createdSupplier = await supplierRepository.CreateAsync(supplier, cancellationToken);
            supplierId = createdSupplier.Id;
        }

        var token = jwtTokenGenerator.GenerateToken(createdUser, customerId, supplierId);
        return new AuthResponse(token, userHelper.ToResponse(createdUser), createdUser.Role, customerId, supplierId);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken)
                   ?? throw new HttpException(401, "Invalid email or password.");

        if (!user.IsActive)
        {
            throw new HttpException(403, "Account is disabled. Please contact administrator.");
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new HttpException(401, "Invalid email or password.");
        }

        long? customerId = null;
        long? supplierId = null;

        if (user.Role == UserRole.Customer)
        {
            var customer = await customerRepository.GetByUserIdAsync(user.Id, cancellationToken);
            customerId = customer?.Id;
        }
        else if (user.Role == UserRole.Supplier)
        {
            var supplier = await supplierRepository.GetByUserIdAsync(user.Id, cancellationToken);
            supplierId = supplier?.Id;
        }

        var token = jwtTokenGenerator.GenerateToken(user, customerId, supplierId);
        return new AuthResponse(token, userHelper.ToResponse(user), user.Role, customerId, supplierId);
    }

    public async Task<AuthResponse> GetCurrentUserProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new HttpException(404, $"User with id '{userId}' was not found.");

        long? customerId = null;
        long? supplierId = null;

        if (user.Role == UserRole.Customer)
        {
            var customer = await customerRepository.GetByUserIdAsync(user.Id, cancellationToken);
            customerId = customer?.Id;
        }
        else if (user.Role == UserRole.Supplier)
        {
            var supplier = await supplierRepository.GetByUserIdAsync(user.Id, cancellationToken);
            supplierId = supplier?.Id;
        }

        var token = jwtTokenGenerator.GenerateToken(user, customerId, supplierId);
        return new AuthResponse(token, userHelper.ToResponse(user), user.Role, customerId, supplierId);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new HttpException(404, $"User with id '{userId}' was not found.");

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new HttpException(400, "Current password is incorrect.");
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);
    }
}
