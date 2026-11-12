namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Utils;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

/// <summary>Idempotent test account and catalog seeder for Direct Bulk Order verification phases.</summary>
public sealed class DirectBulkOrderTestSeedTests(ITestOutputHelper output)
{
    private const string DefaultPassword = "Password123!";
    private const string SampleImageUrl = "https://images.unsplash.com/photo-1631206753348-db44968fd440?auto=format&fit=crop&w=800&q=80";

    [Fact(Skip = "Disabled to prevent polluting the live database with mock test data")]
    public async Task SeedDirectBulkOrderTestEnvironmentAsync()
    {
        var factory = new ApplicationDbContextFactory();
        await using var db = factory.CreateDbContext([]);

        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        output.WriteLine($"Applied migrations ({applied.Count}): {string.Join(", ", applied.TakeLast(5))}");
        output.WriteLine($"Pending migrations ({pending.Count}): {string.Join(", ", pending)}");

        var now = DateTime.UtcNow;
        var passwordHash = PasswordHashUtils.Hash(DefaultPassword);

        var custA = await EnsureUserAsync(db, "cust.a@test.mooncake.local", "0988000101",
            "TEST Customer Alpha Corp", UserRole.Customer, passwordHash, now);
        var custB = await EnsureUserAsync(db, "cust.b@test.mooncake.local", "0988000102",
            "TEST Customer Beta Group", UserRole.Customer, passwordHash, now);
        var supA = await EnsureUserAsync(db, "sup.a@test.mooncake.local", "0988000103",
            "TEST Supplier Workshop Alpha", UserRole.Supplier, passwordHash, now);
        var supB = await EnsureUserAsync(db, "sup.b@test.mooncake.local", "0988000104",
            "TEST Supplier Workshop Beta", UserRole.Supplier, passwordHash, now);
        var supNew = await EnsureUserAsync(db, "sup.new@test.mooncake.local", "0988000105",
            "TEST Supplier New Zero Orders", UserRole.Supplier, passwordHash, now);
        var admin = await EnsureUserAsync(db, "admin@test.mooncake.local", "0988000106",
            "TEST Platform Administrator", UserRole.Admin, passwordHash, now);

        await db.SaveChangesAsync();

        await EnsureCustomerProfileAsync(db, custA.Id, "TEST Alpha Enterprise Co., Ltd.", "0109880101",
            "101 Le Loi, District 1, Ho Chi Minh City", now);
        await EnsureCustomerProfileAsync(db, custB.Id, "TEST Beta Holdings JSC", "0109880102",
            "202 Nguyen Hue, District 1, Ho Chi Minh City", now);

        var supProfileA = await EnsureSupplierProfileAsync(db, supA.Id, "TEST Workshop Alpha Bakery Co.",
            "0309880103", "12 Artisan Street, District 7, Ho Chi Minh City", admin.Id, now);
        var supProfileB = await EnsureSupplierProfileAsync(db, supB.Id, "TEST Workshop Beta Confectionery Co.",
            "0309880104", "88 Heritage Road, Ba Dinh, Ha Noi", admin.Id, now);
        var supProfileNew = await EnsureSupplierProfileAsync(db, supNew.Id, "TEST Workshop Empty Newbie Co.",
            "0309880105", "55 Lotus Street, Da Nang", admin.Id, now);

        await db.SaveChangesAsync();

        var (templateId, revisionId) = await EnsureTemplateAsync(db, now);
        var category = await EnsureCategoryAsync(db, now);

        var shopA = await EnsureShopAsync(db, supA.Id, supProfileA.Id, templateId, revisionId,
            "TEST-NguyetPho-ShopA", "test-nguyet-pho-shop-a", "HCM",
            "12 Artisan Street, District 7, Ho Chi Minh City", "0988000103", now);
        var shopB = await EnsureShopAsync(db, supB.Id, supProfileB.Id, templateId, revisionId,
            "TEST-NguyetPho-ShopB", "test-nguyet-pho-shop-b", "HN",
            "88 Heritage Road, Ba Dinh, Ha Noi", "0988000104", now);

        // Clean up any leftover test orders from previous runs for deterministic integration test suites
        var testCustomerIds = new[] { custA.Id, custB.Id };
        var oldTestOrders = await db.Orders.Where(o => testCustomerIds.Contains(o.CustomerId)).Select(o => o.Id).ToListAsync();
        if (oldTestOrders.Count > 0)
        {
            var orderIdsArray = oldTestOrders.ToArray();
            var orderIdStrings = oldTestOrders.Select(id => id.ToString()).ToArray();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM order_items WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM payments WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM payment_parts WHERE obligation_id IN (SELECT id FROM order_payment_schedule WHERE order_id = ANY({orderIdsArray}))");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM order_payment_schedule WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM inventory_reservations WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM product_inventory_movements WHERE source_id = ANY({orderIdStrings})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM packaging_fee_change_requests WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM order_cancellation_requests WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM order_financial_ledger_entries WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM supplier_payout_requests WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM order_refund_cases WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM order_delivery_disputes WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM deliveries WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM custom_packaging_revisions WHERE \"CustomPackagingId\" IN (SELECT id FROM custom_packagings WHERE order_id = ANY({orderIdsArray}))");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM custom_packagings WHERE order_id = ANY({orderIdsArray})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM orders WHERE id = ANY({orderIdsArray})");
        }

        await db.SaveChangesAsync();

        var prodA1 = await EnsureProductAsync(db, shopA, supProfileA.Id, category,
            "TEST-Imperial Golden Mooncake Gift Box",
            "Luxury corporate mooncake box supporting bespoke logo printing and gift sleeves.",
            500000m, 5, 5000, true, now);
        var varA1V1 = await EnsureVariantAsync(db, prodA1.Id, shopA.Id,
            "4-Piece Abalone Mixed Nuts Box", "TEST-SA-P1-V1", 0m, 2000, 720, now);
        var varA1V2 = await EnsureVariantAsync(db, prodA1.Id, shopA.Id,
            "6-Piece Salted Egg Lava Box", "TEST-SA-P1-V2", 250000m, 2000, 960, now);
        var varA1Race = await EnsureVariantAsync(db, prodA1.Id, shopA.Id,
            "Limited Royal Edition Box", "TEST-SA-P1-RACE", 500000m, 10, 1200, now);

        var prodA2 = await EnsureProductAsync(db, shopA, supProfileA.Id, category,
            "TEST-Classic Lotus Seed Standard Box",
            "Traditional corporate gift box distributed in standard workshop packaging only.",
            300000m, 10, 3000, false, now);
        var varA2V1 = await EnsureVariantAsync(db, prodA2.Id, shopA.Id,
            "4-Piece White Lotus Single Yolk", "TEST-SA-P2-V1", 0m, 1000, 680, now);
        var varA2V2 = await EnsureVariantAsync(db, prodA2.Id, shopA.Id,
            "4-Piece Matcha Macadamia", "TEST-SA-P2-V2", 50000m, 1000, 680, now);

        var prodB1 = await EnsureProductAsync(db, shopB, supProfileB.Id, category,
            "TEST-Hanoi Autumn Heritage Box",
            "Signature Northern mooncake gift box from Workshop Beta.",
            450000m, 5, 2000, true, now);
        var varB1V1 = await EnsureVariantAsync(db, prodB1.Id, shopB.Id,
            "4-Piece Traditional Baked & Snowskin", "TEST-SB-P1-V1", 0m, 800, 700, now);

        await EnsurePromotionsAsync(db, shopA.Id, now);
        await EnsurePackagingTemplatesAsync(db, shopA.Id, now);
        await EnsurePackagingTemplatesAsync(db, shopB.Id, now);
        await EnsureShopOrderPolicyAsync(db, shopA.Id, supA.Id, depositEnabled: true, minDepositTotal: 5_000_000m,
            depositPercent: 30m, taxIncluded: true, taxRate: 8m, isHcm: true, now);
        await EnsureShopOrderPolicyAsync(db, shopB.Id, supB.Id, depositEnabled: false, minDepositTotal: 0m,
            depositPercent: 30m, taxIncluded: true, taxRate: 8m, isHcm: false, now);
        await EnsureSupplierPayoutAccountAsync(db, supA.Id, "VCB", "Vietcombank", "1029384756",
            "TEST WORKSHOP ALPHA BAKERY CO", "CN Nam Sai Gon", now);
        await EnsureSupplierPayoutAccountAsync(db, supB.Id, "TCB", "Techcombank", "1903888999",
            "TEST WORKSHOP BETA CONFECTIONERY", "CN Ba Dinh", now);

        await db.SaveChangesAsync();

        var manifest = new
        {
            appliedMigrationsCount = applied.Count,
            pendingMigrations = pending,
            accounts = new
            {
                custA = new { id = custA.Id, phone = custA.Phone, email = custA.Email, password = DefaultPassword },
                custB = new { id = custB.Id, phone = custB.Phone, email = custB.Email, password = DefaultPassword },
                supA = new { id = supA.Id, phone = supA.Phone, email = supA.Email, password = DefaultPassword, profileId = supProfileA.Id, shopId = shopA.Id, shopSlug = shopA.Slug },
                supB = new { id = supB.Id, phone = supB.Phone, email = supB.Email, password = DefaultPassword, profileId = supProfileB.Id, shopId = shopB.Id, shopSlug = shopB.Slug },
                supNew = new { id = supNew.Id, phone = supNew.Phone, email = supNew.Email, password = DefaultPassword, profileId = supProfileNew.Id },
                admin = new { id = admin.Id, phone = admin.Phone, email = admin.Email, password = DefaultPassword }
            },
            catalog = new
            {
                shopAProductPackaging = new { productId = prodA1.Id, variant1Id = varA1V1.Id, variant2Id = varA1V2.Id, raceVariantId = varA1Race.Id },
                shopAProductStandard = new { productId = prodA2.Id, variant1Id = varA2V1.Id, variant2Id = varA2V2.Id },
                shopBProduct = new { productId = prodB1.Id, variant1Id = varB1V1.Id }
            }
        };

        output.WriteLine("=== SEED_MANIFEST_JSON_START ===");
        output.WriteLine(JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        output.WriteLine("=== SEED_MANIFEST_JSON_END ===");

        Assert.Empty(pending);
    }

    [Fact(Skip = "Disabled to prevent polluting the live database with mock test data")]
    public async Task SeedSearchPageTestCatalogAsync()
    {
        var factory = new ApplicationDbContextFactory();
        await using var db = factory.CreateDbContext([]);

        var now = DateTime.UtcNow;
        var passwordHash = PasswordHashUtils.Hash(DefaultPassword);

        var admin = await EnsureUserAsync(db, "admin@test.mooncake.local", "0988000106",
            "TEST Platform Administrator", UserRole.Admin, passwordHash, now);
        var supA = await EnsureUserAsync(db, "sup.a@test.mooncake.local", "0988000103",
            "TEST Supplier Workshop Alpha", UserRole.Supplier, passwordHash, now);
        var supB = await EnsureUserAsync(db, "sup.b@test.mooncake.local", "0988000104",
            "TEST Supplier Workshop Beta", UserRole.Supplier, passwordHash, now);
        var supC = await EnsureUserAsync(db, "sup.c@test.mooncake.local", "0988000107",
            "TEST Supplier Workshop Gamma Da Nang", UserRole.Supplier, passwordHash, now);

        await db.SaveChangesAsync();

        var supProfileA = await EnsureSupplierProfileAsync(db, supA.Id, "TEST Workshop Alpha Bakery Co.",
            "0309880103", "12 Artisan Street, District 7, Ho Chi Minh City", admin.Id, now);
        supProfileA.ReputationScore = 98m;

        var supProfileB = await EnsureSupplierProfileAsync(db, supB.Id, "TEST Workshop Beta Confectionery Co.",
            "0309880104", "88 Heritage Road, Ba Dinh, Ha Noi", admin.Id, now);
        supProfileB.ReputationScore = 91m;

        var supProfileC = await EnsureSupplierProfileAsync(db, supC.Id, "TEST Workshop Gamma Central Heritage",
            "0309880107", "36 Bach Dang Street, Hai Chau, Da Nang", admin.Id, now);
        supProfileC.ReputationScore = 84m;

        await db.SaveChangesAsync();

        var (templateId, revisionId) = await EnsureTemplateAsync(db, now);

        var catNuong = await EnsureNamedCategoryAsync(db, "Bánh nướng truyền thống", "Bánh nướng vỏ vàng óng nhân thập cẩm, hạt sen", 1, now);
        var catDeo = await EnsureNamedCategoryAsync(db, "Bánh dẻo cao cấp", "Bánh dẻo tuyết lạnh và dẻo truyền thống hương hoa bưởi", 2, now);
        var catVip = await EnsureNamedCategoryAsync(db, "Hộp quà doanh nghiệp VIP", "Bộ sưu tập sơn mài, gỗ khắc laser cao cấp cho đối tác", 3, now);
        var catChay = await EnsureNamedCategoryAsync(db, "Bánh chay thực dưỡng", "Bánh ít đường, hạt dinh dưỡng hữu cơ tốt cho sức khỏe", 4, now);

        var shopA = await EnsureShopAsync(db, supA.Id, supProfileA.Id, templateId, revisionId,
            "TEST-NguyetPho-ShopA", "test-nguyet-pho-shop-a", "HCM",
            "12 Artisan Street, District 7, Ho Chi Minh City", "0988000103", now);
        var shopB = await EnsureShopAsync(db, supB.Id, supProfileB.Id, templateId, revisionId,
            "TEST-NguyetPho-ShopB", "test-nguyet-pho-shop-b", "HN",
            "88 Heritage Road, Ba Dinh, Ha Noi", "0988000104", now);
        var shopC = await EnsureShopAsync(db, supC.Id, supProfileC.Id, templateId, revisionId,
            "TEST-NguyetPho-ShopC-DaNang", "test-nguyet-pho-shop-c-dn", "DN",
            "36 Bach Dang Street, Hai Chau, Da Nang", "0988000107", now);

        await db.SaveChangesAsync();

        var categories = new[] { catNuong, catDeo, catVip, catChay };
        var shops = new[]
        {
            (Shop: shopA, Profile: supProfileA),
            (Shop: shopB, Profile: supProfileB),
            (Shop: shopC, Profile: supProfileC)
        };

        var prices = new decimal[]
        {
            85000m, 120000m, 150000m, 180000m, 220000m, 260000m,
            320000m, 380000m, 450000m, 520000m, 580000m, 650000m,
            720000m, 790000m, 850000m, 920000m, 960000m, 990000m,
            1150000m, 1280000m, 1450000m, 1650000m, 1850000m, 2100000m,
            2450000m, 2800000m, 3200000m, 3600000m,
            195000m, 240000m, 310000m, 410000m, 540000m, 680000m, 880000m, 1550000m
        };

        var images = new[]
        {
            "https://images.unsplash.com/photo-1631206753348-db44968fd440?auto=format&fit=crop&w=800&q=80",
            "https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=800&q=80",
            "https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=800&q=80",
            "https://images.unsplash.com/photo-1558961363-fa8fdf82db35?auto=format&fit=crop&w=800&q=80"
        };

        for (var i = 0; i < 36; i++)
        {
            var cat = categories[i % categories.Length];
            var (shop, profile) = shops[i % shops.Length];
            var basePrice = prices[i];
            var minOrderQty = (i % 5) switch { 0 => 5, 1 => 10, 2 => 20, 3 => 50, _ => 100 };
            var supportsPackaging = i % 2 == 0;
            var inStock = i < 32; // Last 4 products out of stock
            var rating = (i % 6) == 5 ? (decimal?)null : decimal.Round(4.1m + (i % 9) * 0.1m, 1);
            var reviewCount = rating.HasValue ? 8 + (i * 3) : 0;
            var soldCount = 50L + (i * 25L);
            var imgUrl = images[i % images.Length];
            var code = $"P{i + 1:D2}";
            var flavorTag = (i % 4) switch
            {
                0 => "Hoàng Kim Bào Ngư",
                1 => "Tuyết Liên Hạt Sen",
                2 => "Sơn Mài Long Phụng VIP",
                _ => "Hạt Dinh Dưỡng Thực Dưỡng"
            };
            var name = $"TEST-SEARCH-{code} Hộp Bánh {flavorTag} ({shop.RegionCode})";
            var desc = $"Dòng bánh trung thu doanh nghiệp {flavorTag} từ {shop.Name}, giá gốc {basePrice:N0}đ.";

            var product = await EnsureRichSearchProductAsync(
                db, shop, profile, cat, name, desc, basePrice, minOrderQty, 5000,
                supportsPackaging, inStock, rating, reviewCount, soldCount, imgUrl,
                now.AddHours(-i));

            await EnsureVariantAsync(
                db, product.Id, shop.Id,
                $"Phiên bản tiêu chuẩn {code}",
                $"TEST-SRCH-{shop.RegionCode}-{code}",
                0m,
                inStock ? 500 : 0,
                680,
                now.AddHours(-i));
        }

        await db.SaveChangesAsync();
        output.WriteLine("Seeded 36 TEST-SEARCH products across 4 categories and 3 shops.");
    }

    private static async Task<Category> EnsureNamedCategoryAsync(
        ApplicationDbContext db, string name, string description, int sortOrder, DateTime now)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted);
        if (category is null)
        {
            category = new Category
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                SortOrder = sortOrder,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        }
        return category;
    }

    private static async Task<Product> EnsureRichSearchProductAsync(
        ApplicationDbContext db, Shop shop, SupplierProfile profile, Category category,
        string name, string description, decimal basePrice, int minQty, int maxQty,
        bool supportsPackaging, bool inStock, decimal? avgRating, int reviewCount, long soldCount,
        string imageUrl, DateTime publishedAt)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var product = await db.Products.FirstOrDefaultAsync(p => p.ShopId == shop.Id && p.Name == name);
        var effectiveStatus = inStock ? ProductStatus.Active : ProductStatus.OutOfStock;

        if (product is null)
        {
            product = new Product
            {
                Id = Guid.NewGuid(),
                Version = 1,
                ShopId = shop.Id,
                CategoryId = category.Id,
                Name = name,
                NormalizedName = normalized,
                Description = description,
                Ingredients = "Hạt sen Huế, trứng muối thượng hạng, bào ngư, hạt mắc ca, vỏ nướng truyền thống",
                BasePrice = basePrice,
                MinOrderQty = minQty,
                MaxOrderQty = maxQty,
                Unit = "Hộp",
                SupportsCustomPackaging = supportsPackaging,
                Status = effectiveStatus,
                IsPublished = true,
                PublishedAtUtc = publishedAt,
                CreatedAt = publishedAt,
                UpdatedAt = publishedAt
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }
        else
        {
            product.CategoryId = category.Id;
            product.BasePrice = basePrice;
            product.MinOrderQty = minQty;
            product.MaxOrderQty = maxQty;
            product.Unit = "Hộp";
            product.SupportsCustomPackaging = supportsPackaging;
            product.Status = effectiveStatus;
            product.IsPublished = true;
            product.PublishedAtUtc = publishedAt;
            product.IsDeleted = false;
            product.UpdatedAt = publishedAt;
        }

        var hasPrimaryImage = await db.ProductImages.AnyAsync(i => i.ProductId == product.Id && i.IsPrimary && !i.IsDeleted);
        if (!hasPrimaryImage)
        {
            db.ProductImages.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImageUrl = imageUrl,
                IsPrimary = true,
                SortOrder = 0,
                CreatedAt = publishedAt,
                UpdatedAt = publishedAt
            });
        }

        var searchDoc = await db.ProductSearchDocuments.FirstOrDefaultAsync(d => d.ProductId == product.Id);
        if (searchDoc is null)
        {
            db.ProductSearchDocuments.Add(new ProductSearchDocument
            {
                ProductId = product.Id,
                ShopId = shop.Id,
                SupplierProfileId = profile.Id,
                CategoryId = category.Id,
                SourceVersion = product.Version,
                Name = product.Name,
                Description = product.Description,
                ShopName = shop.Name ?? shop.Slug,
                ShopSlug = shop.Slug,
                CategoryName = category.Name,
                SearchText = $"{product.Name} {description} {shop.Name} {category.Name} {shop.RegionCode}",
                PrimaryImageUrl = imageUrl,
                MinPrice = basePrice,
                MaxPrice = basePrice,
                MinOrderQty = minQty,
                Unit = "Hộp",
                SupportsCustomPackaging = supportsPackaging,
                EffectiveStatus = effectiveStatus,
                ReputationScore = profile.ReputationScore,
                AverageRating = avgRating,
                ReviewCount = reviewCount,
                SoldCount = soldCount,
                RegionCode = shop.RegionCode,
                CreatedAtUtc = publishedAt,
                UpdatedAtUtc = publishedAt,
                ProjectedAtUtc = publishedAt
            });
        }
        else
        {
            searchDoc.CategoryId = category.Id;
            searchDoc.CategoryName = category.Name;
            searchDoc.Name = product.Name;
            searchDoc.Description = product.Description;
            searchDoc.ShopName = shop.Name ?? shop.Slug;
            searchDoc.ShopSlug = shop.Slug;
            searchDoc.SearchText = $"{product.Name} {description} {shop.Name} {category.Name} {shop.RegionCode}";
            searchDoc.PrimaryImageUrl = imageUrl;
            searchDoc.MinPrice = basePrice;
            searchDoc.MaxPrice = basePrice;
            searchDoc.MinOrderQty = minQty;
            searchDoc.Unit = "Hộp";
            searchDoc.SupportsCustomPackaging = supportsPackaging;
            searchDoc.EffectiveStatus = effectiveStatus;
            searchDoc.ReputationScore = profile.ReputationScore;
            searchDoc.AverageRating = avgRating;
            searchDoc.ReviewCount = reviewCount;
            searchDoc.SoldCount = soldCount;
            searchDoc.RegionCode = shop.RegionCode;
            searchDoc.UpdatedAtUtc = publishedAt;
            searchDoc.ProjectedAtUtc = publishedAt;
        }

        return product;
    }

    private static async Task<User> EnsureUserAsync(ApplicationDbContext db, string email, string phone,
        string fullName, UserRole role, string passwordHash, DateTime now)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email || u.Phone == phone);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Phone = phone,
                FullName = fullName,
                Role = role,
                RequestedRole = role,
                PasswordHash = passwordHash,
                IsActive = true,
                IsDeleted = false,
                EmailVerifiedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Users.Add(user);
        }
        else
        {
            user.Email = email;
            user.Phone = phone;
            user.FullName = fullName;
            user.Role = role;
            user.RequestedRole = role;
            user.PasswordHash = passwordHash;
            user.IsActive = true;
            user.IsDeleted = false;
            user.EmailVerifiedAt ??= now;
            user.UpdatedAt = now;
        }

        return user;
    }

    private static async Task EnsureCustomerProfileAsync(ApplicationDbContext db, Guid userId, string companyName,
        string taxCode, string billingAddress, DateTime now)
    {
        var profile = await db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
        {
            db.CustomerProfiles.Add(new CustomerProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyName = companyName,
                TaxCode = taxCode,
                BillingAddress = billingAddress,
                ContactPerson = "Procurement Lead",
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            profile.CompanyName = companyName;
            profile.TaxCode = taxCode;
            profile.BillingAddress = billingAddress;
            profile.IsDeleted = false;
            profile.UpdatedAt = now;
        }
    }

    private static async Task<SupplierProfile> EnsureSupplierProfileAsync(ApplicationDbContext db, Guid userId,
        string companyName, string taxCode, string address, Guid reviewerId, DateTime now)
    {
        var profile = await db.SupplierProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
        {
            profile = new SupplierProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Version = 1,
                CompanyName = companyName,
                TaxCode = taxCode,
                Address = address,
                Description = "Verified enterprise mooncake workshop for automated testing.",
                ReputationScore = 95m,
                VerificationStatus = SupplierVerificationStatus.Verified,
                VerifiedAt = now,
                ReviewedAt = now,
                ReviewedByUserId = reviewerId,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.SupplierProfiles.Add(profile);
        }
        else
        {
            profile.CompanyName = companyName;
            profile.TaxCode = taxCode;
            profile.Address = address;
            profile.VerificationStatus = SupplierVerificationStatus.Verified;
            profile.VerifiedAt ??= now;
            profile.RevokedAtUtc = null;
            profile.RevokedByUserId = null;
            profile.RevocationReason = null;
            profile.IsDeleted = false;
            profile.UpdatedAt = now;
        }

        return profile;
    }

    private static async Task<(Guid TemplateId, Guid RevisionId)> EnsureTemplateAsync(ApplicationDbContext db, DateTime now)
    {
        var existingRevision = await db.ShopTemplateRevisions.AsNoTracking().FirstOrDefaultAsync();
        if (existingRevision is not null)
            return (existingRevision.TemplateId, existingRevision.Id);

        var templateId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();
        db.ShopTemplates.Add(new ShopTemplate
        {
            Id = templateId,
            Code = "test-royal-heritage",
            Version = 1,
            Name = "TEST Royal Heritage Storefront",
            IsActive = false,
            SortOrder = 1,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();

        db.ShopTemplateRevisions.Add(new ShopTemplateRevision
        {
            Id = revisionId,
            TemplateId = templateId,
            Revision = 1,
            RendererKey = "royal-heritage",
            SchemaVersion = 1,
            CssVariables = "{}",
            LayoutConfig = "{}",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
        return (templateId, revisionId);
    }

    private static async Task<Category> EnsureCategoryAsync(ApplicationDbContext db, DateTime now)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => !c.IsDeleted);
        if (category is not null)
            return category;

        category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "TEST Corporate Gift Sets",
            Description = "Enterprise mooncake gift collections",
            SortOrder = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static async Task<Shop> EnsureShopAsync(ApplicationDbContext db, Guid ownerUserId, Guid supplierProfileId,
        Guid templateId, Guid revisionId, string name, string slug, string regionCode, string address, string phone, DateTime now)
    {
        var shop = await db.Shops.FirstOrDefaultAsync(s => s.Slug == slug || s.SupplierId == supplierProfileId);
        if (shop is null)
        {
            shop = new Shop
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                SupplierId = supplierProfileId,
                TemplateId = templateId,
                TemplateRevisionId = revisionId,
                Version = 1,
                Name = name,
                Slug = slug,
                Description = $"{name} official B2B storefront",
                PublicAddress = address,
                PublicContactPhone = phone,
                RegionCode = regionCode,
                CustomColors = "{}",
                Status = ShopStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Shops.Add(shop);
        }
        else
        {
            shop.OwnerUserId = ownerUserId;
            shop.SupplierId = supplierProfileId;
            shop.Name = name;
            shop.Slug = slug;
            shop.PublicAddress = address;
            shop.PublicContactPhone = phone;
            shop.RegionCode = regionCode;
            shop.Status = ShopStatus.Active;
            shop.PreSuspensionStatus = null;
            shop.SuspendedAtUtc = null;
            shop.SuspendedByUserId = null;
            shop.SuspensionReason = null;
            shop.IsDeleted = false;
            shop.UpdatedAt = now;
        }

        return shop;
    }

    private static async Task<Product> EnsureProductAsync(ApplicationDbContext db, Shop shop, Guid supplierProfileId,
        Category category, string name, string description, decimal basePrice, int minQty, int maxQty,
        bool supportsPackaging, DateTime now)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var product = await db.Products.FirstOrDefaultAsync(p => p.ShopId == shop.Id && p.Name == name);
        if (product is null)
        {
            product = new Product
            {
                Id = Guid.NewGuid(),
                Version = 1,
                ShopId = shop.Id,
                CategoryId = category.Id,
                Name = name,
                NormalizedName = normalized,
                Description = description,
                Ingredients = "Lotus seed paste, salted egg yolk, roasted nuts, golden syrup crust",
                BasePrice = basePrice,
                MinOrderQty = minQty,
                MaxOrderQty = maxQty,
                Unit = "Box",
                SupportsCustomPackaging = supportsPackaging,
                Status = ProductStatus.Active,
                IsPublished = true,
                PublishedAtUtc = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }
        else
        {
            product.BasePrice = basePrice;
            product.MinOrderQty = minQty;
            product.MaxOrderQty = maxQty;
            product.SupportsCustomPackaging = supportsPackaging;
            product.Status = ProductStatus.Active;
            product.IsPublished = true;
            product.PublishedAtUtc ??= now;
            product.IsDeleted = false;
            product.UpdatedAt = now;
        }

        var hasPrimaryImage = await db.ProductImages.AnyAsync(i => i.ProductId == product.Id && i.IsPrimary && !i.IsDeleted);
        if (!hasPrimaryImage)
        {
            db.ProductImages.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImageUrl = SampleImageUrl,
                IsPrimary = true,
                SortOrder = 0,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        var searchDoc = await db.ProductSearchDocuments.FirstOrDefaultAsync(d => d.ProductId == product.Id);
        if (searchDoc is null)
        {
            db.ProductSearchDocuments.Add(new ProductSearchDocument
            {
                ProductId = product.Id,
                ShopId = shop.Id,
                SupplierProfileId = supplierProfileId,
                CategoryId = category.Id,
                SourceVersion = product.Version,
                Name = product.Name,
                Description = product.Description,
                ShopName = shop.Name ?? shop.Slug,
                ShopSlug = shop.Slug,
                CategoryName = category.Name,
                SearchText = $"{product.Name} {shop.Name} {category.Name} mooncake test",
                PrimaryImageUrl = SampleImageUrl,
                MinPrice = basePrice,
                MaxPrice = basePrice + 500000m,
                MinOrderQty = minQty,
                Unit = "Box",
                SupportsCustomPackaging = supportsPackaging,
                EffectiveStatus = ProductStatus.Active,
                ReputationScore = 95m,
                RegionCode = shop.RegionCode,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ProjectedAtUtc = now
            });
        }
        else
        {
            searchDoc.EffectiveStatus = ProductStatus.Active;
            searchDoc.MinPrice = basePrice;
            searchDoc.SupportsCustomPackaging = supportsPackaging;
            searchDoc.UpdatedAtUtc = now;
            searchDoc.ProjectedAtUtc = now;
        }

        return product;
    }

    private static async Task<ProductVariant> EnsureVariantAsync(ApplicationDbContext db, Guid productId, Guid shopId,
        string name, string sku, decimal priceAdjustment, int stockQty, int weightGram, DateTime now)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.ShopId == shopId && v.NormalizedSku == normalizedSku);
        if (variant is null)
        {
            variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ShopId = shopId,
                Name = name,
                Sku = sku,
                NormalizedSku = normalizedSku,
                CrustType = "Traditional Baked",
                Flavor = "Savory Sweet",
                Filling = name,
                SizeLabel = "Corporate Gift Set",
                WeightGram = weightGram,
                IsGiftSet = true,
                PriceAdjustment = priceAdjustment,
                StockQty = stockQty,
                InventoryVersion = 1,
                ImageUrl = SampleImageUrl,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.ProductVariants.Add(variant);
        }
        else
        {
            variant.ProductId = productId;
            variant.Name = name;
            variant.PriceAdjustment = priceAdjustment;
            variant.StockQty = stockQty;
            variant.IsDeleted = false;
            variant.UpdatedAt = now;
        }

        return variant;
    }

    private static async Task EnsurePromotionsAsync(ApplicationDbContext db, Guid shopId, DateTime now)
    {
        var existing = await db.PromotionRules.Where(r => r.ShopId == shopId && !r.IsDeleted).ToListAsync();
        if (existing.Count > 0) return;

        db.PromotionRules.AddRange(
            new PromotionRule
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "TEST Tier 1 (20-99 Boxes - 5% Off)",
                Description = "5% volume discount for corporate orders of 20 to 99 boxes",
                MinQty = 20,
                MaxQty = 99,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 5m,
                IsActive = true,
                StartDate = now.AddDays(-7),
                EndDate = now.AddMonths(6),
                CreatedAt = now,
                UpdatedAt = now
            },
            new PromotionRule
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "TEST Tier 2 (100+ Boxes - 10% Off)",
                Description = "10% volume discount for corporate orders of 100 boxes or more",
                MinQty = 100,
                MaxQty = null,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                IsActive = true,
                StartDate = now.AddDays(-7),
                EndDate = now.AddMonths(6),
                CreatedAt = now,
                UpdatedAt = now
            });
    }

    private static async Task EnsurePackagingTemplatesAsync(ApplicationDbContext db, Guid shopId, DateTime now)
    {
        var exists = await db.PackagingTemplates.AnyAsync(t => t.ShopId == shopId && !t.IsDeleted);
        if (exists) return;

        db.PackagingTemplates.AddRange(
            new PackagingTemplate
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "TEST Royal Lacquer Gift Box",
                Description = "Handcrafted crimson lacquer box with hot-foil gold logo stamping",
                Material = "Wood & Lacquer",
                Dimensions = "32x32x9 cm",
                Capacity = 4,
                BasePrice = 85000m,
                SampleImageUrl = SampleImageUrl,
                Technique = "Gold Foil Stamping",
                MinOrderQty = 10,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new PackagingTemplate
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                Name = "TEST Imperial Velvet Rigid Box",
                Description = "Two-tier rigid velvet gift box with custom laser-engraved brass plate",
                Material = "Velvet & Brass",
                Dimensions = "36x28x14 cm",
                Capacity = 6,
                BasePrice = 120000m,
                SampleImageUrl = SampleImageUrl,
                Technique = "Laser Brass Plate",
                MinOrderQty = 20,
                IsActive = true,
                SortOrder = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
    }

    private static async Task EnsureShopOrderPolicyAsync(ApplicationDbContext db, Guid shopId, Guid supplierUserId,
        bool depositEnabled, decimal minDepositTotal, decimal depositPercent, bool taxIncluded, decimal taxRate,
        bool isHcm, DateTime now)
    {
        var policy = await db.ShopOrderPolicies.FirstOrDefaultAsync(p => p.ShopId == shopId && !p.IsDeleted);
        if (policy is null)
        {
            db.ShopOrderPolicies.Add(new ShopOrderPolicy
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                DepositEnabled = depositEnabled,
                MinimumOrderTotalForDeposit = minDepositTotal,
                DepositPercent = depositPercent,
                TaxIncluded = taxIncluded,
                TaxRate = taxRate,
                AllowSelfPickup = true,
                RequireShippingZoneSelection = false,
                Version = 1L,
                CreatedBy = supplierUserId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            policy.DepositEnabled = depositEnabled;
            policy.MinimumOrderTotalForDeposit = minDepositTotal;
            policy.DepositPercent = depositPercent;
            policy.TaxIncluded = taxIncluded;
            policy.TaxRate = taxRate;
            policy.AllowSelfPickup = true;
            policy.RequireShippingZoneSelection = false;
            policy.UpdatedAt = now;
        }

        var existingRates = await db.ShopShippingRates.Where(r => r.ShopId == shopId && !r.IsDeleted).ToListAsync();
        if (existingRates.Count > 0) return;

        if (isHcm)
        {
            db.ShopShippingRates.AddRange(
                new ShopShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    RegionCode = "HCM_INNER",
                    RegionName = "TP.HCM Noi Thanh",
                    FeeAmount = 150000m,
                    FreeShippingThreshold = 20000000m,
                    EstimatedDays = 1,
                    IsSupported = true,
                    CreatedBy = supplierUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new ShopShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    RegionCode = "SOUTH_PROVINCE",
                    RegionName = "Mien Nam Lien Tinh",
                    FeeAmount = 350000m,
                    FreeShippingThreshold = 50000000m,
                    EstimatedDays = 3,
                    IsSupported = true,
                    CreatedBy = supplierUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new ShopShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    RegionCode = "SELF_PICKUP",
                    RegionName = "Nhan Truc Tiep Tai Xuong",
                    FeeAmount = 0m,
                    FreeShippingThreshold = null,
                    EstimatedDays = 0,
                    IsSupported = true,
                    CreatedBy = supplierUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
        }
        else
        {
            db.ShopShippingRates.AddRange(
                new ShopShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    RegionCode = "HN_INNER",
                    RegionName = "Ha Noi Noi Thanh",
                    FeeAmount = 120000m,
                    FreeShippingThreshold = 15000000m,
                    EstimatedDays = 1,
                    IsSupported = true,
                    CreatedBy = supplierUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new ShopShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShopId = shopId,
                    RegionCode = "SELF_PICKUP",
                    RegionName = "Nhan Truc Tiep Tai Xuong",
                    FeeAmount = 0m,
                    FreeShippingThreshold = null,
                    EstimatedDays = 0,
                    IsSupported = true,
                    CreatedBy = supplierUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
        }
    }

    private static async Task EnsureSupplierPayoutAccountAsync(ApplicationDbContext db, Guid supplierUserId,
        string bankCode, string bankName, string accountNumber, string holderName, string branchName, DateTime now)
    {
        var existing = await db.SupplierPayoutAccounts.FirstOrDefaultAsync(a => a.SupplierUserId == supplierUserId && !a.IsDeleted);
        if (existing is null)
        {
            db.SupplierPayoutAccounts.Add(new SupplierPayoutAccount
            {
                Id = Guid.NewGuid(),
                SupplierUserId = supplierUserId,
                BankCode = bankCode,
                BankName = bankName,
                AccountNumber = accountNumber,
                AccountHolderName = holderName,
                BranchName = branchName,
                IsVerifiedSimulated = true,
                Version = 1L,
                CreatedBy = supplierUserId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.BankCode = bankCode;
            existing.BankName = bankName;
            existing.AccountNumber = accountNumber;
            existing.AccountHolderName = holderName;
            existing.BranchName = branchName;
            existing.IsVerifiedSimulated = true;
            existing.UpdatedAt = now;
        }
    }
}
