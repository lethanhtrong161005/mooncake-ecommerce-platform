namespace Mooncake.EcommercePlatform.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Mooncake.EcommercePlatform.Domain.Enums;
using Npgsql;
using Npgsql.NameTranslation;
using Pgvector.EntityFrameworkCore;

/// <summary>Creates the context for EF migrations without starting the Web API host.</summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        string? settingsPath = null;
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Mooncake.EcommercePlatform.WebApi", "appsettings.json");
            if (File.Exists(candidate))
            {
                settingsPath = candidate;
                break;
            }

            directory = directory.Parent;
        }

        var configurationBuilder = new ConfigurationBuilder();
        if (settingsPath is not null)
            configurationBuilder.AddJsonFile(settingsPath);

        var configuration = configurationBuilder.AddEnvironmentVariables().Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DB_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is missing. Configure ConnectionStrings:DefaultConnection in appsettings.json or the DB_CONNECTION_STRING environment variable.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var translator = new NpgsqlNullNameTranslator();
        dataSourceBuilder.MapEnum<UserRole>("user_role", translator);
        dataSourceBuilder.MapEnum<ShopStatus>("shop_status", translator);
        dataSourceBuilder.MapEnum<ProductStatus>("product_status", translator);
        dataSourceBuilder.MapEnum<OrderStatus>("order_status", translator);
        dataSourceBuilder.MapEnum<PaymentMethod>("payment_method", translator);
        dataSourceBuilder.MapEnum<PaymentStatus>("payment_status", translator);
        dataSourceBuilder.MapEnum<DeliveryType>("delivery_type", translator);
        dataSourceBuilder.MapEnum<DeliveryStatus>("delivery_status", translator);
        dataSourceBuilder.MapEnum<ProofType>("proof_type", translator);
        dataSourceBuilder.MapEnum<CustomPackagingStatus>("custom_packaging_status", translator);
        dataSourceBuilder.MapEnum<RfqStatus>("rfq_status", translator);
        dataSourceBuilder.MapEnum<RfqInvitationStatus>("rfq_invitation_status", translator);
        dataSourceBuilder.MapEnum<BidStatus>("bid_status", translator);
        dataSourceBuilder.MapEnum<ContractStatus>("contract_status", translator);
        dataSourceBuilder.MapEnum<MilestoneStatus>("milestone_status", translator);
        dataSourceBuilder.MapEnum<DiscountType>("discount_type", translator);
        dataSourceBuilder.MapEnum<AiConversationType>("ai_conversation_type", translator);
        dataSourceBuilder.MapEnum<AiMessageRole>("ai_message_role", translator);
        dataSourceBuilder.MapEnum<AiReportType>("ai_report_type", translator);
        dataSourceBuilder.MapEnum<AiReportStatus>("ai_report_status", translator);
        dataSourceBuilder.MapEnum<ReputationEvent>("reputation_event", translator);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dataSourceBuilder.Build(), npgsql => npgsql.UseVector())
            .Options;
        return new ApplicationDbContext(options);
    }
}
