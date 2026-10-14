namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class ContractRepository(ApplicationDbContext context, IDateTimeProvider clock) : IContractRepository
{
    private const decimal DefaultDepositPercentage = 30m;
    private const int DefaultDepositDeadlineHours = 48;
    public Task<Contract?> GetContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        context.Contracts.AsNoTracking().FirstOrDefaultAsync(contract => contract.Id == contractId && !contract.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Contract>> GetContractsForUserAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default) =>
        supplier
            ? await (from contract in context.Contracts.AsNoTracking()
                     join profile in context.SupplierProfiles.AsNoTracking() on contract.SupplierId equals profile.Id
                     where profile.UserId == userId && !profile.IsDeleted && !contract.IsDeleted
                     orderby contract.CreatedAt descending
                     select contract).Take(100).ToListAsync(cancellationToken)
            : await context.Contracts.AsNoTracking().Where(contract => contract.CustomerId == userId && !contract.IsDeleted)
                .OrderByDescending(contract => contract.CreatedAt).Take(100).ToListAsync(cancellationToken);

    public async Task<bool> IsPartyAsync(Contract contract, Guid userId, CancellationToken cancellationToken = default) =>
        contract.CustomerId == userId || await context.SupplierProfiles.AsNoTracking()
            .AnyAsync(profile => profile.Id == contract.SupplierId && profile.UserId == userId && !profile.IsDeleted, cancellationToken);

    public async Task<bool> HasSignedAsync(Contract contract, Guid userId, CancellationToken cancellationToken = default)
    {
        if (contract.CustomerId == userId) return contract.SignedByCustomerAt is not null;
        return await context.SupplierProfiles.AsNoTracking().AnyAsync(profile => profile.Id == contract.SupplierId
            && profile.UserId == userId && !profile.IsDeleted, cancellationToken) && contract.SignedBySupplierAt is not null;
    }

    public async Task<bool> BeginSignatureAsync(Guid contractId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated($"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (contract is null || contract.IsDeleted || !await IsPartyAsync(contract, actorUserId, cancellationToken)) return false;
        if (contract.Status == ContractStatus.PendingSignature)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        if (contract.Status != ContractStatus.Draft) return false;
        contract.Status = ContractStatus.PendingSignature;
        contract.UpdatedBy = actorUserId;
        contract.UpdatedAt = clock.UtcNow;
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractStatusChanged",
            FromStatus = ContractStatus.Draft.ToString(), ToStatus = ContractStatus.PendingSignature.ToString(), ActorUserId = actorUserId
        });
        context.Notifications.AddRange(
            new Notification { UserId = contract.CustomerId, Type = "contract.pending_signature", Title = "Contract ready for signature", Content = "The draft contract is ready for both parties to sign.", ReferenceType = "contract", ReferenceId = contract.Id },
            new Notification { UserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken), Type = "contract.pending_signature", Title = "Contract ready for signature", Content = "The draft contract is ready for both parties to sign.", ReferenceType = "contract", ReferenceId = contract.Id });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<Contract?> SignAsync(Guid contractId, Guid actorUserId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated($"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (contract is null || contract.IsDeleted || contract.Status != ContractStatus.PendingSignature) return null;
        var isCustomer = contract.CustomerId == actorUserId;
        var isSupplier = !isCustomer && await context.SupplierProfiles.AsNoTracking().AnyAsync(profile => profile.Id == contract.SupplierId
            && profile.UserId == actorUserId && !profile.IsDeleted, cancellationToken);
        if (!isCustomer && !isSupplier) return null;
        var now = clock.UtcNow;
        if (isCustomer)
        {
            if (contract.SignedByCustomerAt.HasValue) return contract;
            contract.SignedByCustomerAt = now;
            contract.CustomerSignatureIp = ipAddress;
            contract.CustomerSignatureUserAgent = userAgent;
        }
        else
        {
            if (contract.SignedBySupplierAt.HasValue) return contract;
            contract.SignedBySupplierAt = now;
            contract.SupplierSignatureIp = ipAddress;
            contract.SupplierSignatureUserAgent = userAgent;
        }
        contract.UpdatedAt = now;
        contract.UpdatedBy = actorUserId;
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractPartySigned",
            FromStatus = ContractStatus.PendingSignature.ToString(), ToStatus = ContractStatus.PendingSignature.ToString(),
            ActorUserId = actorUserId, Metadata = $"ip:{ipAddress ?? "unknown"}; user_agent:{userAgent ?? "unknown"}"
        });
        if (contract.SignedByCustomerAt.HasValue && contract.SignedBySupplierAt.HasValue)
        {
            var snapshot = $"{contract.ContractNumber}\n{contract.Title}\n{contract.TotalValue:0.00}\n{contract.DeliveryDeadline:yyyy-MM-dd}\n{contract.TermsAndConditions}";
            contract.SignedDocumentSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
            var totalValue = contract.TotalValue ?? 0m;
            var depositRule = await context.ContractDepositRules.AsNoTracking()
                .Where(rule => !rule.IsDeleted && rule.AppliesToAll && rule.IsMandatory
                    && rule.DepositPercentage > 0 && rule.DepositPercentage <= 100
                    && (!rule.MinContractValue.HasValue || rule.MinContractValue.Value <= totalValue))
                .OrderByDescending(rule => rule.MinContractValue)
                .ThenByDescending(rule => rule.DepositPercentage)
                .FirstOrDefaultAsync(cancellationToken);
            var fromStatus = ContractStatus.Signed;
            if (totalValue > 0)
            {
                var depositPercentage = depositRule?.DepositPercentage ?? DefaultDepositPercentage;
                var depositAmount = decimal.Round(totalValue * depositPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                var deadlineHours = depositRule?.PaymentDeadlineHours is > 0 and <= 720
                    ? depositRule.PaymentDeadlineHours : DefaultDepositDeadlineHours;
                var milestone = new ContractMilestone
                {
                    Id = Guid.NewGuid(), ContractId = contract.Id, SequenceNumber = 1, Name = "Contract deposit",
                    Description = "Mandatory deposit required to activate the contract.", IsDeposit = true,
                    Amount = depositAmount, Percentage = depositPercentage,
                    DueDate = DateOnly.FromDateTime(now.AddHours(deadlineHours)), Status = MilestoneStatus.AwaitingPayment,
                    CreatedBy = actorUserId
                };
                context.ContractMilestones.Add(milestone);
                var payment = new Payment
                {
                    Id = Guid.NewGuid(), ContractId = contract.Id, MilestoneId = milestone.Id, CustomerId = contract.CustomerId,
                    PaymentNumber = $"PAY-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(), Amount = depositAmount,
                    Currency = "VND", Method = PaymentMethod.BankTransfer, Status = PaymentStatus.Pending,
                    ExpiresAt = now.AddHours(deadlineHours), CreatedBy = actorUserId
                };
                context.Payments.Add(payment);
                contract.DepositRuleId = depositRule?.Id;
                contract.DepositAmount = depositAmount;
                contract.Status = ContractStatus.PendingDeposit;
                context.Notifications.Add(new Notification
                {
                    UserId = contract.CustomerId, Type = "contract.deposit_required", Title = "Contract deposit required",
                    Content = $"Pay {depositAmount:0.00} VND by {payment.ExpiresAt:O} to activate the contract. Use {payment.PaymentNumber} as the transfer reference.",
                    ReferenceType = "contract", ReferenceId = contract.Id
                });
                var supplierUserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken);
                context.Notifications.Add(new Notification
                {
                    UserId = supplierUserId, Type = "contract.deposit_required", Title = "Waiting for contract deposit",
                    Content = "The customer must pay the mandatory deposit before the contract can become active.",
                    ReferenceType = "contract", ReferenceId = contract.Id
                });
            }
            else
            {
                contract.Status = ContractStatus.Active;
            }
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractStatusChanged",
                FromStatus = ContractStatus.PendingSignature.ToString(), ToStatus = ContractStatus.Signed.ToString(), ActorUserId = actorUserId,
                Metadata = $"signed_document_sha256:{contract.SignedDocumentSha256}"
            });
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractStatusChanged",
                FromStatus = fromStatus.ToString(), ToStatus = contract.Status.ToString(), ActorUserId = actorUserId,
                Metadata = $"signed_document_sha256:{contract.SignedDocumentSha256}"
            });
        }
        context.Notifications.AddRange(
            new Notification { UserId = contract.CustomerId, Type = "contract.signature_received", Title = "Contract signature recorded", Content = contract.Status == ContractStatus.Signed ? "Both parties signed the contract." : "The other party still needs to sign the contract.", ReferenceType = "contract", ReferenceId = contract.Id },
            new Notification { UserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken), Type = "contract.signature_received", Title = "Contract signature recorded", Content = contract.Status == ContractStatus.Signed ? "Both parties signed the contract." : "The other party still needs to sign the contract.", ReferenceType = "contract", ReferenceId = contract.Id });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return contract;
    }

    private Task<Guid> GetSupplierUserIdAsync(Guid supplierProfileId, CancellationToken cancellationToken) =>
        context.SupplierProfiles.Where(profile => profile.Id == supplierProfileId && !profile.IsDeleted)
            .Select(profile => profile.UserId).FirstAsync(cancellationToken);
}
