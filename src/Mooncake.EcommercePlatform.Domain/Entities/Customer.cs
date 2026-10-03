using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Customer : BaseEntity
{
    public long UserId { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Individual;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? DefaultAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User? User { get; set; }
    public ICollection<CustomPackaging> CustomPackagings { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<RequestForQuotation> RequestForQuotations { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
}
