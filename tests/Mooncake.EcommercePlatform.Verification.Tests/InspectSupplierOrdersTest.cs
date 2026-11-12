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

    [Fact]
    public async Task InspectTestSearchProductsAsync()
    {
        var factory = new ApplicationDbContextFactory();
        await using var db = factory.CreateDbContext([]);

        var testProds = await db.Products.AsNoTracking()
            .Where(p => p.Name.StartsWith("TEST-SEARCH") || p.Name.Contains("TEST"))
            .OrderBy(p => p.Name)
            .ToListAsync();

        output.WriteLine($"=== TEST PRODUCTS IN DATABASE ({testProds.Count}) ===");
        foreach (var p in testProds)
        {
            output.WriteLine($"ID={p.Id}, Name='{p.Name}', Published={p.IsPublished}, Deleted={p.IsDeleted}, CreatedAt={p.CreatedAt}");
        }

        var p32 = testProds.FirstOrDefault(p => p.Name.Contains("P32"));
        if (p32 != null)
        {
            output.WriteLine($"\n=== DETAILS FOR P32 ({p32.Id}) ===");
            output.WriteLine($"Name: {p32.Name}");
            output.WriteLine($"Desc: {p32.Description}");
            output.WriteLine($"Ingredients: {p32.Ingredients}");
            output.WriteLine($"BasePrice: {p32.BasePrice}");
            var imgs = await db.ProductImages.AsNoTracking().Where(i => i.ProductId == p32.Id).ToListAsync();
            output.WriteLine($"Images ({imgs.Count}):");
            foreach (var img in imgs)
            {
                output.WriteLine($"  ID={img.Id}, Url={img.ImageUrl}, Primary={img.IsPrimary}, Deleted={img.IsDeleted}");
            }
        }
    }

    private async Task InspectTargetProductAsync()
    {
        var factory = new ApplicationDbContextFactory();
        await using var db = factory.CreateDbContext([]);

        var targetId = Guid.Parse("2d688fba-07a5-4917-acd7-ad981ddf0274");
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == targetId);
        output.WriteLine($"=== TARGET PRODUCT {targetId} ===");
        if (product == null)
        {
            output.WriteLine("Product NOT FOUND in db.Products table!");
            var count = await db.Products.CountAsync();
            output.WriteLine($"Total products in db: {count}");
            var sample = await db.Products.AsNoTracking().Take(5).ToListAsync();
            foreach (var p in sample)
            {
                output.WriteLine($"Sample Product: ID={p.Id}, Name={p.Name}, IsPublished={p.IsPublished}, IsDeleted={p.IsDeleted}, ShopId={p.ShopId}");
            }
            return;
        }

        output.WriteLine($"Product: Name={product.Name}, IsPublished={product.IsPublished}, IsDeleted={product.IsDeleted}, ShopId={product.ShopId}, CategoryId={product.CategoryId}, BasePrice={product.BasePrice}, CreatedAt={product.CreatedAt}, CreatedBy={product.CreatedBy}");

        var shop = await db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == product.ShopId);
        if (shop == null)
        {
            output.WriteLine($"Shop {product.ShopId} NOT FOUND!");
        }
        else
        {
            output.WriteLine($"Shop: ID={shop.Id}, Name={shop.Name}, Status={shop.Status}, IsDeleted={shop.IsDeleted}, SupplierId={shop.SupplierId}, OwnerUserId={shop.OwnerUserId}");

            var profile = await db.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == shop.SupplierId);
            if (profile == null)
            {
                output.WriteLine($"SupplierProfile with ID={shop.SupplierId} NOT FOUND!");
                var profileByUser = await db.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == shop.OwnerUserId);
                output.WriteLine($"SupplierProfile with UserId={shop.OwnerUserId} found: {(profileByUser != null ? $"ID={profileByUser.Id}, Status={profileByUser.VerificationStatus}" : "NOT FOUND")}");
            }
            else
            {
                output.WriteLine($"SupplierProfile: ID={profile.Id}, Status={profile.VerificationStatus}, IsDeleted={profile.IsDeleted}, UserId={profile.UserId}");
            }

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == shop.OwnerUserId);
            if (user == null)
            {
                output.WriteLine($"User with OwnerUserId={shop.OwnerUserId} NOT FOUND!");
            }
            else
            {
                output.WriteLine($"User: ID={user.Id}, Role={user.Role}, IsActive={user.IsActive}, IsDeleted={user.IsDeleted}");
            }
        }

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == product.CategoryId);
        if (category == null)
        {
            output.WriteLine($"Category {product.CategoryId} NOT FOUND!");
        }
        else
        {
            output.WriteLine($"Category: ID={category.Id}, Name={category.Name}, IsDeleted={category.IsDeleted}");
        }

        var variants = await db.ProductVariants.AsNoTracking().Where(v => v.ProductId == targetId).ToListAsync();
        output.WriteLine($"Variants count: {variants.Count}");
        foreach (var v in variants)
        {
            output.WriteLine($"Variant: ID={v.Id}, Name={v.Name}, IsDeleted={v.IsDeleted}, PriceAdjustment={v.PriceAdjustment}, StockQty={v.StockQty}");
        }

        var images = await db.ProductImages.AsNoTracking().Where(img => img.ProductId == targetId).ToListAsync();
        output.WriteLine($"Images count: {images.Count}");
        foreach (var img in images)
        {
            output.WriteLine($"Image: ID={img.Id}, Url={img.ImageUrl}, IsPrimary={img.IsPrimary}, IsDeleted={img.IsDeleted}");
        }

        var searchDoc = await db.ProductSearchDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.ProductId == targetId);
        output.WriteLine($"SearchDoc: {(searchDoc != null ? $"Found: MinPrice={searchDoc.MinPrice}, MaxPrice={searchDoc.MaxPrice}, PrimaryImg={searchDoc.PrimaryImageUrl}" : "NOT FOUND")}");

        // Check all products and their variant counts
        var allProducts = await db.Products.AsNoTracking().Where(p => !p.IsDeleted).ToListAsync();
        output.WriteLine($"\n=== ALL NON-DELETED PRODUCTS ({allProducts.Count}) ===");
        var allVariants = await db.ProductVariants.AsNoTracking().Where(v => !v.IsDeleted).ToListAsync();
        var variantsByProd = allVariants.GroupBy(v => v.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        int zeroVariantsCount = 0;
        int hasVariantsCount = 0;
        foreach (var p in allProducts)
        {
            var varList = variantsByProd.TryGetValue(p.Id, out var vl) ? vl : [];
            if (varList.Count == 0) zeroVariantsCount++;
            else hasVariantsCount++;
            output.WriteLine($"Product: ID={p.Id}, Name='{p.Name}', Published={p.IsPublished}, Variants={varList.Count}");
        }
        // Seed default variant for all products with 0 variants
        var prodsWithoutVariants = allProducts.Where(p => !variantsByProd.ContainsKey(p.Id) || variantsByProd[p.Id].Count == 0).ToList();
        output.WriteLine($"\n=== SEEDING VARIANTS FOR {prodsWithoutVariants.Count} PRODUCTS ===");
        if (prodsWithoutVariants.Count > 0)
        {
            var allImages = await db.ProductImages.AsNoTracking().Where(img => !img.IsDeleted).ToListAsync();
            var imagesByProd = allImages.GroupBy(img => img.ProductId).ToDictionary(g => g.Key, g => g.ToList());

            var newVariants = new List<Mooncake.EcommercePlatform.Domain.Entities.ProductVariant>();
            var seedTime = DateTime.UtcNow;

            foreach (var p in prodsWithoutVariants)
            {
                var prodImages = imagesByProd.TryGetValue(p.Id, out var imgs) ? imgs : [];
                var primaryImg = prodImages.FirstOrDefault(i => i.IsPrimary) ?? prodImages.FirstOrDefault();
                var sku = $"SKU-STD-{p.Id.ToString()[..8].ToUpperInvariant()}";
                var isGift = p.Name.Contains("Hộp", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Bộ", StringComparison.OrdinalIgnoreCase);

                var variant = new Mooncake.EcommercePlatform.Domain.Entities.ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.Id,
                    ShopId = p.ShopId,
                    Name = isGift ? "Hộp tiêu chuẩn" : "Bánh tiêu chuẩn",
                    Sku = sku,
                    NormalizedSku = sku,
                    CrustType = "Tiêu chuẩn",
                    Flavor = "Truyền thống",
                    Filling = string.IsNullOrEmpty(p.Ingredients) ? null : (p.Ingredients.Length > 90 ? p.Ingredients[..90] : p.Ingredients),
                    SizeLabel = "Tiêu chuẩn",
                    WeightGram = 200,
                    IsGiftSet = isGift,
                    PriceAdjustment = 0m,
                    StockQty = 500,
                    InventoryVersion = 1,
                    ImageUrl = primaryImg?.ImageUrl,
                    CreatedAt = seedTime,
                    UpdatedAt = seedTime,
                    IsDeleted = false
                };
                newVariants.Add(variant);
            }

            db.ProductVariants.AddRange(newVariants);
            var saved = await db.SaveChangesAsync();
            output.WriteLine($"Successfully seeded {newVariants.Count} product variants ({saved} rows saved)!");
        }

        // Re-verify target product
        var finalVariants = await db.ProductVariants.AsNoTracking().Where(v => v.ProductId == targetId).ToListAsync();
        output.WriteLine($"Target product {targetId} final variants count: {finalVariants.Count}");
        foreach (var v in finalVariants)
        {
            output.WriteLine($"  Variant: ID={v.Id}, Name={v.Name}, Sku={v.Sku}, PriceAdj={v.PriceAdjustment}, Stock={v.StockQty}, ImageUrl={v.ImageUrl}");
        }

        // Test GetPublicAsync through repository
        var repo = new Mooncake.EcommercePlatform.Infrastructure.Repositories.ProductCatalogRepository(
            db, new Mooncake.EcommercePlatform.Infrastructure.Services.DateTimeProvider());
        var publicDetail = await repo.GetPublicAsync(targetId, default);
        output.WriteLine($"\n=== REPO.GETPUBLICASYNC RESULT ===");
        if (publicDetail == null)
        {
            output.WriteLine("GetPublicAsync RETURNED NULL!");
        }
        else
        {
            output.WriteLine($"SUCCESS! Product: Name='{publicDetail.Name}', EffectiveStatus={publicDetail.EffectiveStatus}, BasePrice={publicDetail.BasePrice}, Variants={publicDetail.Variants.Count}, Images={publicDetail.Images.Count}, Shop='{publicDetail.Shop.Name}'");
        }

        // Test root query
        var root = await (from p in db.Products.AsNoTracking()
                          join s in db.Shops.AsNoTracking() on p.ShopId equals s.Id
                          join prof in db.SupplierProfiles.AsNoTracking() on s.SupplierId equals prof.Id
                          join u in db.Users.AsNoTracking() on s.OwnerUserId equals u.Id
                          join c in db.Categories.AsNoTracking() on p.CategoryId equals c.Id
                          where p.Id == targetId && p.IsPublished && !p.IsDeleted && !s.IsDeleted &&
                            s.Status == Mooncake.EcommercePlatform.Domain.Enums.ShopStatus.Active && !prof.IsDeleted && prof.VerificationStatus == Mooncake.EcommercePlatform.Domain.Enums.SupplierVerificationStatus.Verified &&
                            u.IsActive && !u.IsDeleted && u.Role == Mooncake.EcommercePlatform.Domain.Enums.UserRole.Supplier && !c.IsDeleted
                          select new { p, s, prof, c }).SingleOrDefaultAsync();
        output.WriteLine($"Root query result: {(root != null ? "MATCHED" : "NULL (FAILED)")}");
    }
}
