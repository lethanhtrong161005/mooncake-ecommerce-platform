using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Npgsql;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.Verification.Tests;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_PG_CONNECTION_STRING")))
    throw new InvalidOperationException("Set TEST_PG_CONNECTION_STRING to a disposable loopback PostgreSQL server with pgvector.");
await using var fixtureLifetime = new FixtureLifetime();
var fixture = fixtureLifetime.Fixture;
await fixture.InitializeLegacyAsync();
await using (var seed = fixture.Create()) {
    await seed.Database.MigrateAsync();
    await seed.Database.ExecuteSqlRawAsync("""
        INSERT INTO users(id,email,role,requested_role,is_active)
        SELECT md5('bench-user-'||i)::uuid, 'bench-'||i||'@example.test', 'Supplier'::user_role, 'Supplier'::user_role, true FROM generate_series(1,10000) i;
        INSERT INTO users(id,email,role,is_active) VALUES (md5('bench-admin')::uuid,'bench-admin@example.test','Admin',true);
        INSERT INTO supplier_profiles(id,user_id,company_name,verification_status,reputation_score)
        SELECT md5('bench-profile-'||i)::uuid, md5('bench-user-'||i)::uuid, 'Benchmark Bakery '||i, 'Verified',90 FROM generate_series(1,10000) i;
        INSERT INTO shops(id,supplier_id,owner_user_id,name,slug,status,template_id,template_revision_id)
        SELECT md5('bench-shop-'||i)::uuid,md5('bench-profile-'||i)::uuid,md5('bench-user-'||i)::uuid,
        'Benchmark Bakery '||i,'benchmark-shop-'||i,'Active','90000000-0000-4000-8000-000000000001','91000000-0000-4000-8000-000000000001'
        FROM generate_series(1,10000) i;
        INSERT INTO categories(id,name) VALUES(md5('bench-category')::uuid,'Benchmark Category');
        INSERT INTO products(id,shop_id,category_id,name,base_price,status)
        SELECT md5('bench-product-'||i)::uuid, md5('bench-shop-'||(CASE WHEN i<=50000 THEN 1 ELSE 2+(i%9999) END))::uuid,
          md5('bench-category')::uuid,'Benchmark Product '||i,10000+(i%1000)*100,'Active' FROM generate_series(1,100000) i;
        INSERT INTO product_variants(id,product_id,name,sku,price_adjustment,stock_qty,filling,flavor,weight_gram)
        SELECT md5('bench-variant-'||i||'-'||v)::uuid,md5('bench-product-'||i)::uuid,'Variant '||v,'BENCH-'||i||'-'||v,
          v*1000,100,'Lotus','Traditional',150 FROM generate_series(1,100000) i CROSS JOIN generate_series(1,3) v;
        INSERT INTO product_images(id,product_id,image_url,is_primary,sort_order)
        SELECT md5('bench-image-'||i)::uuid,md5('bench-product-'||i)::uuid,'https://example.test/product.png',true,0 FROM generate_series(1,100000) i;
        ANALYZE;
        """);
}
var counter = new CaptureQueries();
await using var db = fixture.Create(counter);
var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(() => DateTime.UtcNow);
var service = new ShopService(new ShopRepository(db), clock.Object, new ShopCursorProtector(new EphemeralDataProtectionProvider(), clock.Object), Mock.Of<IShopMediaGateway>());
var owner = await db.Users.Where(x => x.Email == "bench-1@example.test").Select(x => x.Id).SingleAsync();
var admin = await db.Users.Where(x => x.Email == "bench-admin@example.test").Select(x => x.Id).SingleAsync();
var measurements = new List<object>();
var plans = new List<object>();
async Task Measure(string operation, Func<Task> action, bool explain = false) {
    await action(); // Exclude compilation and first connection warmup.
    var times = new List<double>(); var reads = new List<int>();
    for (var i = 0; i < 20; i++) {
        counter.Reads = 0; var watch = Stopwatch.StartNew(); await action(); watch.Stop();
        times.Add(watch.Elapsed.TotalMilliseconds); reads.Add(counter.Reads);
    }
    measurements.Add(new { operation, samples = times.Count, p50Ms = Math.Round(times.Order().ElementAt(9), 2),
        p95Ms = Math.Round(times.Order().ElementAt(18), 2), maxReaderCommands = reads.Max() });
    if (!explain) return;
    var connection = db.Database.GetDbConnection(); await db.Database.OpenConnectionAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + counter.LastSql;
    foreach (var parameter in counter.Parameters) command.Parameters.Add(parameter.Clone());
    await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
    plans.Add(new { operation, sql = counter.LastSql, explain = JsonSerializer.Deserialize<JsonElement>(reader.GetString(0)) });
}
await Measure("publicProfileHotShop", async () => { await service.PublicAsync("benchmark-shop-1", default); });
await Measure("ownerPreviewHotShop", async () => { await service.PreviewAsync(owner, default); });
await Measure("publicNewestHotShop", async () => { await service.ProductsAsync("benchmark-shop-1", new() { PageSize = 12 }, default); }, true);
await Measure("publicPriceFilteredHotShop", async () => { await service.ProductsAsync("benchmark-shop-1", new() {
    PageSize = 12, Sort = "priceAsc", Filling = "Lotus", MinPrice = 20000, MaxPrice = 50000 }, default); }, true);
await Measure("adminPage10", async () => { await service.ListAdminAsync(admin, new() { PageSize = 10 }, default); });
await Measure("adminPage100", async () => { await service.ListAdminAsync(admin, new() { PageSize = 100 }, default); }, true);
var report = new { generatedAtUtc = DateTime.UtcNow, environment = "Windows host / Docker PostgreSQL 17 pgvector / loopback / warm cache / sequential Application service calls; not HTTP load testing",
    data = new { shops = 10000, products = 100000, variants = 300000, images = 100000, hotShopProducts = 50000 },
    measurements, plans };
await File.WriteAllTextAsync(".agents/feature/shop-management/08_QUERY_BENCHMARK.json", JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(measurements));

sealed class CaptureQueries : DbCommandInterceptor {
    public int Reads; public string LastSql = ""; public NpgsqlParameter[] Parameters = [];
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
        Reads++; LastSql = command.CommandText; Parameters = command.Parameters.Cast<NpgsqlParameter>().Select(x => x.Clone()).ToArray();
        return ValueTask.FromResult(result);
    }
}
sealed class FixtureLifetime : IAsyncDisposable {
    public PostgresVerificationFixture Fixture { get; } = new();
    public ValueTask DisposeAsync() => new(Fixture.DisposeAsync());
}
