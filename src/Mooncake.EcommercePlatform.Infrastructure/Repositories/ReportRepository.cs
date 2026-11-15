namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Reports.Models;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core repository providing projection queries for executive reporting and analytics.</summary>
public sealed class ReportRepository(ApplicationDbContext dbContext) : IReportRepository
{
    public async Task<AdminReportSnapshot> GetAdminReportSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.Orders.AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Select(o => new ReportOrderRow(o.Id, o.ShopId, o.Status, o.TotalAmount, o.CreatedAt))
            .ToListAsync(cancellationToken);

        var orderLedgerEntries = await dbContext.OrderFinancialLedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted && (e.EntryType == "Captured" || e.EntryType == "RefundedSimulated"))
            .Select(e => new ReportOrderLedgerRow(e.OrderId, e.EntryType, e.Amount))
            .ToListAsync(cancellationToken);

        var contracts = await dbContext.Contracts.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => new ReportContractRow(c.Id, c.SupplierId, c.Status, c.TotalValue ?? 0m, c.CreatedAt))
            .ToListAsync(cancellationToken);

        var resolvedContractRefunds = await dbContext.ContractDisputes.AsNoTracking()
            .Where(d => !d.IsDeleted && d.Status == "Resolved" && d.RefundAmount > 0m)
            .Select(d => new ReportContractRefundRow(d.ContractId, d.RefundAmount))
            .ToListAsync(cancellationToken);

        var pendingSuppliersCount = await dbContext.SupplierProfiles.AsNoTracking()
            .CountAsync(p => !p.IsDeleted && p.VerificationStatus == SupplierVerificationStatus.Pending, cancellationToken);

        var pendingContractDisputesCount = await dbContext.ContractDisputes.AsNoTracking()
            .CountAsync(d => !d.IsDeleted && d.Status == "Pending", cancellationToken);

        var openDeliveryDisputesCount = await dbContext.OrderDeliveryDisputes.AsNoTracking()
            .CountAsync(d => !d.IsDeleted && d.Status == "Open", cancellationToken);

        var totalPaidMilestoneEscrowBalance = await dbContext.ContractMilestones.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Status == MilestoneStatus.Paid)
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0m;

        return new AdminReportSnapshot(
            orders,
            orderLedgerEntries,
            contracts,
            resolvedContractRefunds,
            pendingSuppliersCount,
            pendingContractDisputesCount,
            openDeliveryDisputesCount,
            totalPaidMilestoneEscrowBalance);
    }

    public async Task<ConversionFunnelRawCounts> GetConversionFunnelCountsAsync(CancellationToken cancellationToken = default)
    {
        var rfqsCount = await dbContext.RequestForQuotations.AsNoTracking()
            .CountAsync(r => !r.IsDeleted, cancellationToken);
        var bidsCount = await dbContext.Bids.AsNoTracking()
            .CountAsync(b => !b.IsDeleted, cancellationToken);
        var bidsAwardedCount = await dbContext.Bids.AsNoTracking()
            .CountAsync(b => !b.IsDeleted && b.Status == BidStatus.Accepted, cancellationToken);
        var contractsSignedCount = await dbContext.Contracts.AsNoTracking()
            .CountAsync(c => !c.IsDeleted && c.SignedByCustomerAt != null && c.SignedBySupplierAt != null, cancellationToken);
        var contractsCompletedCount = await dbContext.Contracts.AsNoTracking()
            .CountAsync(c => !c.IsDeleted && (c.Status == ContractStatus.CompletedOnTime || c.Status == ContractStatus.CompletedLate), cancellationToken);

        return new ConversionFunnelRawCounts(
            rfqsCount,
            bidsCount,
            bidsAwardedCount,
            contractsSignedCount,
            contractsCompletedCount);
    }

    public async Task<TopSuppliersReportSnapshot> GetTopSuppliersSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var verifiedSuppliers = await dbContext.SupplierProfiles.AsNoTracking()
            .Where(p => !p.IsDeleted && p.VerificationStatus == SupplierVerificationStatus.Verified)
            .Select(p => new ReportSupplierProfileRow(
                p.Id,
                p.CompanyName,
                p.TaxCode,
                p.ReputationScore,
                p.TotalContracts,
                p.ContractsOnTime))
            .ToListAsync(cancellationToken);

        var shops = await dbContext.Shops.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => new ReportShopRow(s.Id, s.SupplierId, s.Name))
            .ToListAsync(cancellationToken);

        var recognizedOrders = await dbContext.Orders.AsNoTracking()
            .Where(o => !o.IsDeleted && (o.Status == OrderStatus.Confirmed
                || o.Status == OrderStatus.AwaitingCustomerDeliveryApproval
                || o.Status == OrderStatus.Processing
                || o.Status == OrderStatus.Shipped
                || o.Status == OrderStatus.Delivered))
            .Select(o => new ReportOrderRow(o.Id, o.ShopId, o.Status, o.TotalAmount, o.CreatedAt))
            .ToListAsync(cancellationToken);

        var orderRefundEntries = await dbContext.OrderFinancialLedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted && e.EntryType == "RefundedSimulated")
            .Select(e => new ReportOrderLedgerRow(e.OrderId, e.EntryType, e.Amount))
            .ToListAsync(cancellationToken);

        var recognizedContracts = await dbContext.Contracts.AsNoTracking()
            .Where(c => !c.IsDeleted && (c.Status == ContractStatus.Active
                || c.Status == ContractStatus.CompletedOnTime
                || c.Status == ContractStatus.CompletedLate))
            .Select(c => new ReportContractRow(c.Id, c.SupplierId, c.Status, c.TotalValue ?? 0m, c.CreatedAt))
            .ToListAsync(cancellationToken);

        var resolvedContractRefunds = await dbContext.ContractDisputes.AsNoTracking()
            .Where(d => !d.IsDeleted && d.Status == "Resolved" && d.RefundAmount > 0m)
            .Select(d => new ReportContractRefundRow(d.ContractId, d.RefundAmount))
            .ToListAsync(cancellationToken);

        return new TopSuppliersReportSnapshot(
            verifiedSuppliers,
            shops,
            recognizedOrders,
            orderRefundEntries,
            recognizedContracts,
            resolvedContractRefunds);
    }

    public async Task<IReadOnlyList<ContractExportRow>> GetRecentContractsForExportAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
        await dbContext.Contracts.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new ContractExportRow(
                c.ContractNumber,
                c.Title,
                c.TotalValue,
                c.Status,
                c.SignedByCustomerAt,
                c.SignedBySupplierAt))
            .ToListAsync(cancellationToken);
}
