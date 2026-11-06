namespace Mooncake.EcommercePlatform.Verification.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

public sealed class InsertCustomerAccountTest(ITestOutputHelper output)
{
    [Fact]
    public async Task InsertOrUpdateCustomerAccountAsync()
    {
        var factory = new ApplicationDbContextFactory();
        await using var db = factory.CreateDbContext([]);

        const string phone = "0987654321";
        const string email = "customer.b2b@mooncake.vn";
        const string rawPassword = "Password123!";
        const string fullName = "Nguyễn Văn Doanh Nghiệp (Test Customer)";

        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone || u.Email == email);
        var passwordHash = PasswordHashUtils.Hash(rawPassword);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Phone = phone,
                Email = email,
                PasswordHash = passwordHash,
                Role = UserRole.Customer,
                RequestedRole = UserRole.Customer,
                IsActive = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            output.WriteLine($"Adding new customer: ID={user.Id}, Phone={phone}");
        }
        else
        {
            user.FullName = fullName;
            user.Phone = phone;
            user.Email = email;
            user.PasswordHash = passwordHash;
            user.Role = UserRole.Customer;
            user.IsActive = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            output.WriteLine($"Updating existing user to customer: ID={user.Id}, Phone={phone}");
        }

        // Update supplier accounts password
        var sup1 = await db.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse("893f708e-8e19-4911-b328-9aeef308ed02"));
        if (sup1 != null) { sup1.PasswordHash = passwordHash; sup1.Role = UserRole.Supplier; sup1.IsActive = true; }

        var sup2 = await db.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse("2f716c60-0f0e-47fc-9909-d9ebdf6d2198"));
        if (sup2 != null) { sup2.Phone = "0356088748"; sup2.PasswordHash = passwordHash; sup2.Role = UserRole.Supplier; sup2.IsActive = true; }

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Phone == "0901234569");
        if (admin != null) { admin.PasswordHash = passwordHash; admin.IsActive = true; }

        await db.SaveChangesAsync();
        output.WriteLine("Updated passwords for Supplier (0356088749, 0356088748) and Admin (0901234569) to Password123!");

        Assert.Equal(UserRole.Customer, user.Role);
        Assert.True(PasswordHashUtils.Verify(rawPassword, user.PasswordHash, out _));
    }
}

