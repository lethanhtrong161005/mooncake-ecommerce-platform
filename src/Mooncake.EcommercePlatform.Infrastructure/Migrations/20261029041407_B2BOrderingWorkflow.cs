using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class B2BOrderingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .Annotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .Annotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .Annotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .Annotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .Annotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .Annotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .Annotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .Annotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .Annotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .Annotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .Annotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .Annotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .Annotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .Annotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .Annotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .Annotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .OldAnnotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .OldAnnotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .OldAnnotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .OldAnnotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .OldAnnotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .OldAnnotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,Approved,InProduction,Rejected")
                .OldAnnotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .OldAnnotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .OldAnnotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .OldAnnotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .OldAnnotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .OldAnnotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .OldAnnotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .OldAnnotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .OldAnnotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .OldAnnotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .OldAnnotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<decimal>(
                name: "packaging_fee",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "packaging_fee",
                table: "orders");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .Annotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .Annotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .Annotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .Annotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .Annotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,Approved,InProduction,Rejected")
                .Annotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .Annotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .Annotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .Annotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .Annotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .Annotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .Annotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .Annotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .Annotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .Annotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .Annotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .OldAnnotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .OldAnnotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .OldAnnotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .OldAnnotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .OldAnnotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .OldAnnotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .OldAnnotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned")
                .OldAnnotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled")
                .OldAnnotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .OldAnnotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .OldAnnotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .OldAnnotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .OldAnnotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .OldAnnotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .OldAnnotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .OldAnnotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .OldAnnotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
