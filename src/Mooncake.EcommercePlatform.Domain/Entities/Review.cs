using Mooncake.EcommercePlatform.Domain.Common;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Review : BaseEntity
{
    public long CustomerId { get; set; }
    public long SupplierId { get; set; }
    public long? OrderId { get; set; }
    public long? ContractId { get; set; }
    public short Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Customer Customer { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public Order? Order { get; set; }
    public Contract? Contract { get; set; }
    public ICollection<ReputationLog> ReputationLogs { get; set; } = [];
}
