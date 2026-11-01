namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Models;

using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record ContractRecord(ContractResponse Response, Guid CustomerId, Guid SupplierId, ContractStatus Status);
