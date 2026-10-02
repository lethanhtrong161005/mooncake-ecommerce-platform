namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IRfqRepository"/>.</summary>
public class RfqRepository(ApplicationDbContext context) : IRfqRepository
{
    public async Task<IEnumerable<RequestForQuotation>> GetRfqsAsync(long? customerId = null, RfqStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = context.RequestForQuotations
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.RfqItems)
            .Include(r => r.Quotations)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<RequestForQuotation?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.RequestForQuotations
            .Include(r => r.Customer).ThenInclude(c => c.User)
            .Include(r => r.RfqItems).ThenInclude(i => i.CustomPackaging)
            .Include(r => r.RfqItems).ThenInclude(i => i.Product)
            .Include(r => r.Quotations).ThenInclude(q => q.Supplier).ThenInclude(s => s.User)
            .Include(r => r.Quotations).ThenInclude(q => q.QuotationItems)
            .Include(r => r.Quotations).ThenInclude(q => q.PriceNegotiations)
            .Include(r => r.RfqInvitations).ThenInclude(inv => inv.Supplier)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<RequestForQuotation> CreateRfqAsync(RequestForQuotation rfq, CancellationToken cancellationToken = default)
    {
        context.RequestForQuotations.Add(rfq);
        await context.SaveChangesAsync(cancellationToken);
        return rfq;
    }

    public async Task<RequestForQuotation> UpdateRfqAsync(RequestForQuotation rfq, CancellationToken cancellationToken = default)
    {
        context.RequestForQuotations.Update(rfq);
        await context.SaveChangesAsync(cancellationToken);
        return rfq;
    }

    public async Task<RfqInvitation> AddInvitationAsync(RfqInvitation invitation, CancellationToken cancellationToken = default)
    {
        context.RfqInvitations.Add(invitation);
        await context.SaveChangesAsync(cancellationToken);
        return invitation;
    }

    public async Task<IEnumerable<RfqInvitation>> GetInvitationsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default) =>
        await context.RfqInvitations
            .Include(i => i.RequestForQuotation).ThenInclude(r => r.Customer)
            .Include(i => i.RequestForQuotation).ThenInclude(r => r.RfqItems)
            .AsNoTracking()
            .Where(i => i.SupplierId == supplierId)
            .OrderByDescending(i => i.InvitedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Quotation>> GetQuotationsByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default) =>
        await context.Quotations
            .Include(q => q.Supplier).ThenInclude(s => s.User)
            .Include(q => q.QuotationItems).ThenInclude(qi => qi.RfqItem)
            .Include(q => q.PriceNegotiations.OrderBy(n => n.RoundNo))
            .AsNoTracking()
            .Where(q => q.RfqId == rfqId)
            .OrderBy(q => q.TotalAmount)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Quotation>> GetQuotationsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default) =>
        await context.Quotations
            .Include(q => q.RequestForQuotation).ThenInclude(r => r.Customer)
            .Include(q => q.QuotationItems)
            .Include(q => q.PriceNegotiations.OrderBy(n => n.RoundNo))
            .AsNoTracking()
            .Where(q => q.SupplierId == supplierId)
            .OrderByDescending(q => q.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<Quotation?> GetQuotationByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Quotations
            .Include(q => q.Supplier).ThenInclude(s => s.User)
            .Include(q => q.RequestForQuotation).ThenInclude(r => r.Customer).ThenInclude(c => c.User)
            .Include(q => q.QuotationItems).ThenInclude(qi => qi.RfqItem)
            .Include(q => q.PriceNegotiations.OrderBy(n => n.RoundNo))
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<Quotation> CreateQuotationAsync(Quotation quotation, CancellationToken cancellationToken = default)
    {
        context.Quotations.Add(quotation);
        await context.SaveChangesAsync(cancellationToken);
        return quotation;
    }

    public async Task<Quotation> UpdateQuotationAsync(Quotation quotation, CancellationToken cancellationToken = default)
    {
        context.Quotations.Update(quotation);
        await context.SaveChangesAsync(cancellationToken);
        return quotation;
    }

    public async Task<PriceNegotiation> AddNegotiationAsync(PriceNegotiation negotiation, CancellationToken cancellationToken = default)
    {
        context.PriceNegotiations.Add(negotiation);
        await context.SaveChangesAsync(cancellationToken);
        return negotiation;
    }

    public async Task<PriceNegotiation?> GetNegotiationByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.PriceNegotiations
            .Include(n => n.Quotation)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<PriceNegotiation> UpdateNegotiationAsync(PriceNegotiation negotiation, CancellationToken cancellationToken = default)
    {
        context.PriceNegotiations.Update(negotiation);
        await context.SaveChangesAsync(cancellationToken);
        return negotiation;
    }

    public async Task CheckAndExpireRfqsAndQuotationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // 1. Expire RFQs whose quote deadline has passed
        var expiredRfqs = await context.RequestForQuotations
            .Where(r => r.Status == RfqStatus.Open && r.QuoteDeadline.HasValue && r.QuoteDeadline.Value < now)
            .ToListAsync(cancellationToken);

        foreach (var rfq in expiredRfqs)
        {
            rfq.Status = RfqStatus.Expired;
            rfq.UpdatedAtUtc = now;
        }

        // 2. Expire Quotations whose validity date has passed
        var expiredQuotations = await context.Quotations
            .Where(q => (q.Status == QuotationStatus.Submitted || q.Status == QuotationStatus.Negotiating)
                     && q.ValidUntilUtc.HasValue && q.ValidUntilUtc.Value < now)
            .ToListAsync(cancellationToken);

        foreach (var q in expiredQuotations)
        {
            q.Status = QuotationStatus.Expired;
            q.UpdatedAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
