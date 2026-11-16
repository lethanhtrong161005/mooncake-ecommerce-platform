using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverRoleAndProfiles : Migration
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
                .Annotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned,AwaitingCustomerConfirmation,Disputed")
                .Annotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled,Expired,AwaitingCustomerDeliveryApproval,CancellationRequested,Disputed")
                .Annotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .Annotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .Annotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .Annotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .Annotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .Annotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .Annotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .Annotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .Annotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin,Driver")
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
                .OldAnnotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned,AwaitingCustomerConfirmation,Disputed")
                .OldAnnotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled,Expired,AwaitingCustomerDeliveryApproval,CancellationRequested,Disputed")
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

            migrationBuilder.CreateTable(
                name: "driver_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    default_vehicle_plate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("driver_profiles_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_driver_profiles_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_driver_profiles_phone",
                table: "driver_profiles",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "idx_driver_profiles_user_id",
                table: "driver_profiles",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "driver_profiles");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:ai_conversation_type", "ProductInfo,MooncakeHistory,SalesAnalytics,General")
                .Annotation("Npgsql:Enum:ai_message_role", "User,Assistant,System")
                .Annotation("Npgsql:Enum:ai_report_status", "Pending,Processing,Completed,Failed")
                .Annotation("Npgsql:Enum:ai_report_type", "SalesTrend,SupplierPerformance,DemandForecast,ProductAnalysis,RevenueSummary")
                .Annotation("Npgsql:Enum:bid_status", "Submitted,Accepted,Cancelled")
                .Annotation("Npgsql:Enum:contract_status", "Draft,PendingSignature,Signed,PendingDeposit,Active,CompletedOnTime,CompletedLate,Breached,Disputed,Cancelled")
                .Annotation("Npgsql:Enum:custom_packaging_status", "Pending,DesignReview,RevisionRequested,Approved,InProduction,Completed,Rejected")
                .Annotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned,AwaitingCustomerConfirmation,Disputed")
                .Annotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .Annotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .Annotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .Annotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled,Expired,AwaitingCustomerDeliveryApproval,CancellationRequested,Disputed")
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
                .OldAnnotation("Npgsql:Enum:delivery_status", "Pending,Confirmed,PickedUp,InTransit,Delivered,Failed,Returned,AwaitingCustomerConfirmation,Disputed")
                .OldAnnotation("Npgsql:Enum:delivery_type", "PlatformManaged,SelfArranged")
                .OldAnnotation("Npgsql:Enum:discount_type", "Percentage,FixedAmount,FixedPrice")
                .OldAnnotation("Npgsql:Enum:milestone_status", "Pending,AwaitingPayment,Paid,InProgress,PendingReview,CompletedOnTime,CompletedLate,Overdue,Failed")
                .OldAnnotation("Npgsql:Enum:order_status", "Pending,Confirmed,Processing,Shipped,Delivered,Cancelled,Expired,AwaitingCustomerDeliveryApproval,CancellationRequested,Disputed")
                .OldAnnotation("Npgsql:Enum:payment_method", "BankTransfer,Momo,VNPay,ZaloPay,Cash")
                .OldAnnotation("Npgsql:Enum:payment_status", "Pending,Processing,Completed,Failed,Refunded")
                .OldAnnotation("Npgsql:Enum:product_status", "Active,Inactive,OutOfStock")
                .OldAnnotation("Npgsql:Enum:proof_type", "Pickup,InTransit,Delivery,FailedAttempt")
                .OldAnnotation("Npgsql:Enum:reputation_event", "CompletedOnTime,CompletedLate,Breached,PositiveReview,NegativeReview,AdminPenalty,AdminBonus")
                .OldAnnotation("Npgsql:Enum:rfq_invitation_status", "Invited,Viewed,BidSubmitted,Declined")
                .OldAnnotation("Npgsql:Enum:rfq_status", "Draft,Open,InNegotiation,Awarded,Closed,Cancelled")
                .OldAnnotation("Npgsql:Enum:shop_status", "Active,Inactive,Suspended")
                .OldAnnotation("Npgsql:Enum:user_role", "Customer,Supplier,Admin,Driver")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
