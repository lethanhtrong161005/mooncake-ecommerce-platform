namespace Mooncake.EcommercePlatform.Verification.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

public sealed class InspectSupplierOrdersTest(ITestOutputHelper output)
{
    [Fact]
    public async Task InspectSupplierAndOrdersAsync()
    {
        var factory = new ApplicationDbContextFactory();
        await using var db = factory.CreateDbContext([]);

        // 1. Check all shops
        var shops = await db.Shops.AsNoTracking().ToListAsync();
        output.WriteLine($"=== SHOPS ({shops.Count}) ===");
        foreach (var s in shops)
        {
            output.WriteLine($"Shop: ID={s.Id}, Name={s.Name}, Slug={s.Slug}, SupplierId={s.SupplierId}, IsDeleted={s.IsDeleted}");
        }

        // 2. Check SupplierProfiles
        var profiles = await db.SupplierProfiles.AsNoTracking().ToListAsync();
        output.WriteLine($"\n=== SUPPLIER PROFILES ({profiles.Count}) ===");
        foreach (var p in profiles)
        {
            output.WriteLine($"Profile: ID={p.Id}, UserId={p.UserId}, CompanyName={p.CompanyName}, Status={p.VerificationStatus}, IsDeleted={p.IsDeleted}");
        }

        // 3. Check All Users
        var allUsers = await db.Users.AsNoTracking().ToListAsync();
        output.WriteLine($"\n=== ALL USERS ({allUsers.Count}) ===");
        foreach (var u in allUsers)
        {
            output.WriteLine($"User: ID={u.Id}, Phone={u.Phone}, Email={u.Email}, FullName={u.FullName}, Role={u.Role}, HasPassword={!string.IsNullOrEmpty(u.PasswordHash)}, IsActive={u.IsActive}");
        }

        // 4. Check Orders
        var orders = await db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt).Take(10).ToListAsync();
        output.WriteLine($"\n=== LATEST ORDERS ({orders.Count}) ===");
        foreach (var o in orders)
        {
            output.WriteLine($"Order: ID={o.Id}, Number={o.OrderNumber}, ShopId={o.ShopId}, CustomerId={o.CustomerId}, Status={o.Status}, Total={o.TotalAmount}");
        }

        // 5. Test the exact join from OrderRepository.GetSupplierOrdersAsync
        var suppliers = allUsers.Where(u => u.Role == Mooncake.EcommercePlatform.Domain.Enums.UserRole.Supplier).ToList();
        if (suppliers.Count > 0)
        {
            foreach (var sup in suppliers)
            {
                var supOrders = await (from order in db.Orders.AsNoTracking()
                                       join shop in db.Shops on order.ShopId equals shop.Id
                                       join profile in db.SupplierProfiles on shop.SupplierId equals profile.Id
                                       where profile.UserId == sup.Id && !profile.IsDeleted && !shop.IsDeleted && !order.IsDeleted
                                       orderby order.CreatedAt descending
                                       select order).ToListAsync();
                output.WriteLine($"\nSupplier User ID={sup.Id} ({sup.Phone ?? sup.Email}) has {supOrders.Count} orders matching join!");
            }
        }
    }
}
