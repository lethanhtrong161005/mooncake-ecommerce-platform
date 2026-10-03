using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Supplier : BaseEntity
{
    public long UserId { get; set; }
    public string BusinessName { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public bool IsVerified { get; set; }
    public int ReputationScore { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User? User { get; set; }
    public ICollection<Shop> Shops { get; set; } = [];
    public ICollection<RfqInvitation> RfqInvitations { get; set; } = [];
    public ICollection<Quotation> Quotations { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<ReputationLog> ReputationLogs { get; set; } = [];
    public ICollection<AiAnalyticsReport> AiAnalyticsReports { get; set; } = [];
}
