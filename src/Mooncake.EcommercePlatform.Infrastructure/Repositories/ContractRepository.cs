namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                var milestone1 = new ContractMilestone
                {
                    Id = Guid.NewGuid(), ContractId = contract.Id, SequenceNumber = 1, Name = "Đặt cọc kích hoạt hợp đồng (M1)",
                    Description = "Mandatory deposit required to activate the contract.", IsDeposit = true,
                    Amount = depositAmount, Percentage = depositPercentage,
                    DueDate = DateOnly.FromDateTime(now.AddHours(deadlineHours)), Status = MilestoneStatus.AwaitingPayment,
                    CreatedBy = actorUserId
                };
                var m2Amount = decimal.Round(totalValue * 0.50m, 2, MidpointRounding.AwayFromZero);
                var milestone2 = new ContractMilestone
                {
                    Id = Guid.NewGuid(), ContractId = contract.Id, SequenceNumber = 2, Name = "Sản xuất & Giao hàng đợt 1 (M2)",
                    Description = "Báo cáo tiến độ sản xuất và giao đợt 1.", IsDeposit = false,
                    Amount = m2Amount, Percentage = 50m,
                    DueDate = contract.DeliveryDeadline ?? DateOnly.FromDateTime(now.AddDays(14)), Status = MilestoneStatus.Pending,
                    CreatedBy = actorUserId
                };
                var m3Percentage = 100m - depositPercentage - 50m;
                var m3Amount = totalValue - depositAmount - m2Amount;
                var milestone3 = new ContractMilestone
                {
                    Id = Guid.NewGuid(), ContractId = contract.Id, SequenceNumber = 3, Name = "Nghiệm thu toàn bộ & Quyết toán (M3)",
                    Description = "Nghiệm thu dứt điểm 100% lô hàng và tất toán hợp đồng.", IsDeposit = false,
                    Amount = m3Amount, Percentage = m3Percentage,
                    DueDate = contract.DeliveryDeadline ?? DateOnly.FromDateTime(now.AddDays(21)), Status = MilestoneStatus.Pending,
                    CreatedBy = actorUserId
                };
                context.ContractMilestones.AddRange(milestone1, milestone2, milestone3);

                var payment = new Payment
                {
                    Id = Guid.NewGuid(), ContractId = contract.Id, MilestoneId = milestone1.Id, CustomerId = contract.CustomerId,
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

    public async Task<IReadOnlyList<ContractMilestone>> GetMilestonesAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var milestones = await context.ContractMilestones
            .Where(m => m.ContractId == contractId && !m.IsDeleted)
            .OrderBy(m => m.SequenceNumber)
            .ToListAsync(cancellationToken);

        // Auto-backfill M2 and M3 if an existing active contract only had M1 created
        if (milestones.Count == 1 && milestones[0].IsDeposit)
        {
            var contract = await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
            if (contract != null && (contract.TotalValue ?? 0) > 0)
            {
                var totalValue = contract.TotalValue!.Value;
                var m1 = milestones[0];
                var m2Amount = decimal.Round(totalValue * 0.50m, 2, MidpointRounding.AwayFromZero);
                var now = clock.UtcNow;
                var milestone2 = new ContractMilestone
                {
                    Id = Guid.NewGuid(), ContractId = contractId, SequenceNumber = 2, Name = "Sản xuất & Giao hàng đợt 1 (M2)",
                    Description = "Báo cáo tiến độ sản xuất và giao đợt 1.", IsDeposit = false,
                    Amount = m2Amount, Percentage = 50m,
                    DueDate = contract.DeliveryDeadline ?? DateOnly.FromDateTime(now.AddDays(14)),
                    Status = contract.Status == ContractStatus.Active ? MilestoneStatus.InProgress : MilestoneStatus.Pending,
                    CreatedBy = contract.CustomerId
                };
                var m3Percentage = 100m - (m1.Percentage ?? 30m) - 50m;
                var m3Amount = totalValue - (m1.Amount ?? 0m) - m2Amount;
                var milestone3 = new ContractMilestone
                {
                    Id = Guid.NewGuid(), ContractId = contractId, SequenceNumber = 3, Name = "Nghiệm thu toàn bộ & Quyết toán (M3)",
                    Description = "Nghiệm thu dứt điểm 100% lô hàng và tất toán hợp đồng.", IsDeposit = false,
                    Amount = m3Amount, Percentage = m3Percentage,
                    DueDate = contract.DeliveryDeadline ?? DateOnly.FromDateTime(now.AddDays(21)), Status = MilestoneStatus.Pending,
                    CreatedBy = contract.CustomerId
                };
                context.ContractMilestones.AddRange(milestone2, milestone3);
                await context.SaveChangesAsync(cancellationToken);
                milestones.Add(milestone2);
                milestones.Add(milestone3);
            }
        }

        return milestones;
    }

    public Task<ContractMilestone?> GetMilestoneAsync(Guid contractId, Guid milestoneId, CancellationToken cancellationToken = default) =>
        context.ContractMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId && m.ContractId == contractId && !m.IsDeleted, cancellationToken);

    public Task<ContractExtension?> GetPendingExtensionAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        context.ContractExtensions.AsNoTracking()
            .Where(e => e.ContractId == contractId && e.Status == "Pending" && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ContractDispute?> GetActiveDisputeAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        context.ContractDisputes.AsNoTracking()
            .Where(d => d.ContractId == contractId && d.Status == "Pending" && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ContractMilestone?> SubmitMilestoneProgressAsync(Guid contractId, Guid milestoneId, string? notes, IReadOnlyList<string>? evidenceUrls, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var milestone = await context.ContractMilestones.FromSqlInterpolated($"SELECT * FROM contract_milestones WHERE id = {milestoneId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (milestone == null || milestone.ContractId != contractId || milestone.IsDeleted) return null;

        milestone.ProgressNotes = notes;
        milestone.EvidenceUrls = evidenceUrls != null && evidenceUrls.Count > 0 ? JsonSerializer.Serialize(evidenceUrls) : null;
        milestone.Status = MilestoneStatus.PendingReview;
        milestone.UpdatedAt = clock.UtcNow;
        milestone.UpdatedBy = actorUserId;

        var contract = await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract != null)
        {
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "ContractMilestone", AggregateId = milestone.Id, EventType = "MilestoneProgressSubmitted",
                FromStatus = MilestoneStatus.InProgress.ToString(), ToStatus = MilestoneStatus.PendingReview.ToString(), ActorUserId = actorUserId,
                Metadata = $"notes:{notes}"
            });
            context.Notifications.Add(new Notification
            {
                UserId = contract.CustomerId, Type = "contract.milestone_progress", Title = "Milestone progress submitted",
                Content = $"Supplier submitted progress evidence for milestone: {milestone.Name}. Please review and verify.",
                ReferenceType = "contract", ReferenceId = contract.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return milestone;
    }

    public async Task<ContractMilestone?> VerifyMilestoneAsync(Guid contractId, Guid milestoneId, bool isApproved, string? rejectionReason, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var milestone = await context.ContractMilestones.FromSqlInterpolated($"SELECT * FROM contract_milestones WHERE id = {milestoneId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (milestone == null || milestone.ContractId != contractId || milestone.IsDeleted) return null;

        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var fromStatus = milestone.Status;

        if (isApproved)
        {
            var isOnTime = !milestone.DueDate.HasValue || today <= milestone.DueDate.Value;
            milestone.Status = isOnTime ? MilestoneStatus.CompletedOnTime : MilestoneStatus.CompletedLate;
            milestone.CompletionDate = today;
        }
        else
        {
            milestone.Status = MilestoneStatus.Failed;
            if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                milestone.ProgressNotes = string.IsNullOrWhiteSpace(milestone.ProgressNotes)
                    ? $"[Customer Rejection]: {rejectionReason}"
                    : $"{milestone.ProgressNotes}\n[Customer Rejection]: {rejectionReason}";
            }
        }

        milestone.UpdatedAt = now;
        milestone.UpdatedBy = actorUserId;

        var contract = await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract != null)
        {
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "ContractMilestone", AggregateId = milestone.Id, EventType = "MilestoneVerified",
                FromStatus = fromStatus.ToString(), ToStatus = milestone.Status.ToString(), ActorUserId = actorUserId,
                Metadata = $"approved:{isApproved}; reason:{rejectionReason}"
            });
            var supplierUserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken);
            context.Notifications.Add(new Notification
            {
                UserId = supplierUserId, Type = "contract.milestone_verified", Title = isApproved ? "Milestone verified" : "Milestone rejected",
                Content = isApproved ? $"Milestone '{milestone.Name}' was verified and approved by customer." : $"Milestone '{milestone.Name}' was rejected. Reason: {rejectionReason}",
                ReferenceType = "contract", ReferenceId = contract.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return milestone;
    }

    public async Task<ContractExtension> RequestExtensionAsync(Guid contractId, DateOnly proposedDueDate, string reason, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated($"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        var now = clock.UtcNow;
        var extension = new ContractExtension
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            ProposedDueDate = proposedDueDate,
            Reason = reason,
            Status = "Pending",
            RequestedByUserId = actorUserId,
            RequestedAt = now,
            CreatedAt = now,
            CreatedBy = actorUserId
        };
        context.ContractExtensions.Add(extension);

        if (contract != null)
        {
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractExtensionRequested",
                FromStatus = contract.Status.ToString(), ToStatus = contract.Status.ToString(), ActorUserId = actorUserId,
                Metadata = $"proposed_date:{proposedDueDate:yyyy-MM-dd}; reason:{reason}"
            });
            context.Notifications.Add(new Notification
            {
                UserId = contract.CustomerId, Type = "contract.extension_requested", Title = "Contract extension requested",
                Content = $"Supplier has requested a deadline extension to {proposedDueDate:dd/MM/yyyy}. Reason: {reason}",
                ReferenceType = "contract", ReferenceId = contract.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return extension;
    }

    public async Task<ContractExtension?> RespondExtensionAsync(Guid contractId, bool isApproved, string? responseNote, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var extension = await context.ContractExtensions
            .FromSqlInterpolated($"SELECT * FROM contract_extensions WHERE contract_id = {contractId} AND status = 'Pending' AND is_deleted = false FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (extension == null) return null;

        var contract = await context.Contracts.FromSqlInterpolated($"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        var now = clock.UtcNow;

        extension.Status = isApproved ? "Approved" : "Rejected";
        extension.RespondedAt = now;
        extension.RespondedByUserId = actorUserId;
        extension.ResponseNote = responseNote;
        extension.UpdatedAt = now;
        extension.UpdatedBy = actorUserId;

        if (contract != null)
        {
            if (isApproved)
            {
                contract.DeliveryDeadline = extension.ProposedDueDate;
                contract.UpdatedAt = now;
                contract.UpdatedBy = actorUserId;

                // Update the last milestone due date to match
                var lastMilestone = await context.ContractMilestones
                    .Where(m => m.ContractId == contractId && !m.IsDeleted && !m.IsDeposit)
                    .OrderByDescending(m => m.SequenceNumber)
                    .FirstOrDefaultAsync(cancellationToken);
                if (lastMilestone != null)
                {
                    lastMilestone.DueDate = extension.ProposedDueDate;
                    lastMilestone.UpdatedAt = now;
                }
            }

            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractExtensionResponded",
                FromStatus = contract.Status.ToString(), ToStatus = contract.Status.ToString(), ActorUserId = actorUserId,
                Metadata = $"approved:{isApproved}; note:{responseNote}"
            });

            var supplierUserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken);
            context.Notifications.Add(new Notification
            {
                UserId = supplierUserId, Type = "contract.extension_responded", Title = isApproved ? "Extension approved" : "Extension rejected",
                Content = isApproved ? $"Your deadline extension request to {extension.ProposedDueDate:dd/MM/yyyy} was approved." : $"Your deadline extension request was rejected. Note: {responseNote}",
                ReferenceType = "contract", ReferenceId = contract.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return extension;
    }

    public async Task<ContractDispute> RaiseDisputeAsync(Guid contractId, string reason, IReadOnlyList<string>? evidenceUrls, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated($"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        var now = clock.UtcNow;

        var fromStatus = contract?.Status ?? ContractStatus.Active;
        if (contract != null)
        {
            contract.Status = ContractStatus.Disputed;
            contract.UpdatedAt = now;
            contract.UpdatedBy = actorUserId;
        }

        var dispute = new ContractDispute
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            RaisedByUserId = actorUserId,
            DisputeReason = reason,
            EvidenceUrls = evidenceUrls != null && evidenceUrls.Count > 0 ? JsonSerializer.Serialize(evidenceUrls) : null,
            Status = "Pending",
            RaisedAt = now,
            CreatedAt = now,
            CreatedBy = actorUserId
        };
        context.ContractDisputes.Add(dispute);

        if (contract != null)
        {
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractDisputeRaised",
                FromStatus = fromStatus.ToString(), ToStatus = ContractStatus.Disputed.ToString(), ActorUserId = actorUserId,
                Metadata = $"reason:{reason}"
            });

            var supplierUserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken);
            context.Notifications.AddRange(
                new Notification { UserId = contract.CustomerId, Type = "contract.disputed", Title = "Contract dispute opened", Content = $"A dispute was opened for contract {contract.ContractNumber}. Escrow funds are frozen pending review.", ReferenceType = "contract", ReferenceId = contract.Id },
                new Notification { UserId = supplierUserId, Type = "contract.disputed", Title = "Contract dispute opened", Content = $"A dispute was opened for contract {contract.ContractNumber}. Escrow funds are frozen pending review.", ReferenceType = "contract", ReferenceId = contract.Id });
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return dispute;
    }

    public async Task<bool> RequestFinalAcceptanceAsync(Guid contractId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var contract = await context.Contracts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        if (contract == null || contract.Status != ContractStatus.Active) return false;

        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Contract", AggregateId = contract.Id, EventType = "FinalAcceptanceRequested",
            FromStatus = ContractStatus.Active.ToString(), ToStatus = ContractStatus.Active.ToString(), ActorUserId = actorUserId
        });
        context.Notifications.Add(new Notification
        {
            UserId = contract.CustomerId, Type = "contract.final_acceptance_requested", Title = "Final acceptance requested",
            Content = $"Supplier has completed all deliveries for contract {contract.ContractNumber} and requested final acceptance.",
            ReferenceType = "contract", ReferenceId = contract.Id
        });
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Contract?> ConfirmFinalAcceptanceAsync(Guid contractId, bool confirmedQualityAndQuantity, string? acceptanceNotes, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated($"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (contract == null || contract.Status != ContractStatus.Active) return null;

        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var isOnTime = !contract.DeliveryDeadline.HasValue || today <= contract.DeliveryDeadline.Value;

        contract.Status = isOnTime ? ContractStatus.CompletedOnTime : ContractStatus.CompletedLate;
        contract.CompletedAt = now;
        contract.UpdatedAt = now;
        contract.UpdatedBy = actorUserId;

        // Mark remaining milestones as completed
        var milestones = await context.ContractMilestones
            .Where(m => m.ContractId == contractId && !m.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var m in milestones.Where(m => m.Status != MilestoneStatus.Paid && m.Status != MilestoneStatus.CompletedOnTime))
        {
            m.Status = isOnTime ? MilestoneStatus.CompletedOnTime : MilestoneStatus.CompletedLate;
            m.CompletionDate = today;
            m.UpdatedAt = now;
        }

        // Award reputation to supplier (+10 on time, +3 late)
        var delta = isOnTime ? 10m : 3m;
        var repEvent = isOnTime ? ReputationEvent.CompletedOnTime : ReputationEvent.CompletedLate;
        context.ReputationLogs.Add(new ReputationLog
        {
            Id = Guid.NewGuid(),
            SupplierId = contract.SupplierId,
            EventType = repEvent,
            ScoreDelta = delta,
            Reason = $"Contract {contract.ContractNumber} final acceptance confirmed ({(isOnTime ? "on time" : "late")}). {acceptanceNotes}".Trim(),
            ReferenceId = contract.Id,
            ReferenceType = "Contract",
            CreatedAt = now,
            CreatedBy = actorUserId
        });

        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractCompleted",
            FromStatus = ContractStatus.Active.ToString(), ToStatus = contract.Status.ToString(), ActorUserId = actorUserId,
            Metadata = $"confirmed:{confirmedQualityAndQuantity}; on_time:{isOnTime}; score_delta:{delta}"
        });

        var supplierUserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken);
        context.Notifications.AddRange(
            new Notification { UserId = contract.CustomerId, Type = "contract.completed", Title = "Contract completed", Content = $"Contract {contract.ContractNumber} has been finalized successfully.", ReferenceType = "contract", ReferenceId = contract.Id },
            new Notification { UserId = supplierUserId, Type = "contract.completed", Title = "Contract completed", Content = $"Contract {contract.ContractNumber} final acceptance confirmed! Earned +{delta:0} reputation points.", ReferenceType = "contract", ReferenceId = contract.Id });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return contract;
    }

    public async Task<ContractDispute?> ResolveDisputeAsync(Guid contractId, string resolutionType, decimal refundAmount, string adminVerdict, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var contract = await context.Contracts.FromSqlInterpolated($"SELECT * FROM contracts WHERE id = {contractId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        var dispute = await context.ContractDisputes
            .FromSqlInterpolated($"SELECT * FROM contract_disputes WHERE contract_id = {contractId} AND status = 'Pending' AND is_deleted = false FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (dispute == null || contract == null) return null;

        var now = clock.UtcNow;
        dispute.Status = "Resolved";
        dispute.ResolutionType = resolutionType;
        dispute.RefundAmount = refundAmount;
        dispute.AdminVerdict = adminVerdict;
        dispute.ResolvedAt = now;
        dispute.ResolvedByUserId = adminUserId;
        dispute.UpdatedAt = now;
        dispute.UpdatedBy = adminUserId;

        var isBreach = resolutionType.Equals("FullRefund", StringComparison.OrdinalIgnoreCase)
            || resolutionType.Equals("SupplierAtFault", StringComparison.OrdinalIgnoreCase);

        if (isBreach)
        {
            contract.Status = ContractStatus.Breached;
            context.ReputationLogs.Add(new ReputationLog
            {
                Id = Guid.NewGuid(),
                SupplierId = contract.SupplierId,
                EventType = ReputationEvent.Breached,
                ScoreDelta = -20m,
                Reason = $"Dispute resolved against supplier for contract {contract.ContractNumber}: {adminVerdict}",
                ReferenceId = contract.Id,
                ReferenceType = "Contract",
                CreatedAt = now,
                CreatedBy = adminUserId
            });
        }
        else
        {
            contract.Status = ContractStatus.Cancelled;
        }

        contract.UpdatedAt = now;
        contract.UpdatedBy = adminUserId;

        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Contract", AggregateId = contract.Id, EventType = "ContractDisputeResolved",
            FromStatus = ContractStatus.Disputed.ToString(), ToStatus = contract.Status.ToString(), ActorUserId = adminUserId,
            Metadata = $"resolution:{resolutionType}; refund:{refundAmount:0.00}; verdict:{adminVerdict}"
        });

        var supplierUserId = await GetSupplierUserIdAsync(contract.SupplierId, cancellationToken);
        context.Notifications.AddRange(
            new Notification { UserId = contract.CustomerId, Type = "contract.dispute_resolved", Title = "Dispute resolved", Content = $"Dispute for contract {contract.ContractNumber} has been resolved by platform admin. Verdict: {adminVerdict}", ReferenceType = "contract", ReferenceId = contract.Id },
            new Notification { UserId = supplierUserId, Type = "contract.dispute_resolved", Title = "Dispute resolved", Content = $"Dispute for contract {contract.ContractNumber} has been resolved by platform admin. Verdict: {adminVerdict}", ReferenceType = "contract", ReferenceId = contract.Id });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return dispute;
    }

    private Task<Guid> GetSupplierUserIdAsync(Guid supplierProfileId, CancellationToken cancellationToken) =>
        context.SupplierProfiles.Where(profile => profile.Id == supplierProfileId && !profile.IsDeleted)
            .Select(profile => profile.UserId).FirstAsync(cancellationToken);

    public async Task<IReadOnlyList<Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses.ContractDisputeListItemResponse>> ListDisputesAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = from dispute in context.ContractDisputes.AsNoTracking()
                    join contract in context.Contracts.AsNoTracking() on dispute.ContractId equals contract.Id
                    join supplier in context.SupplierProfiles.AsNoTracking() on contract.SupplierId equals supplier.Id into supps
                    from supplier in supps.DefaultIfEmpty()
                    join shop in context.Shops.AsNoTracking() on supplier.Id equals shop.SupplierId into shops
                    from shop in shops.DefaultIfEmpty()
                    join user in context.Users.AsNoTracking() on contract.CustomerId equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    where !dispute.IsDeleted && !contract.IsDeleted
                    select new { dispute, contract, supplier, shop, user };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.dispute.Status == status);
        }

        var list = await query.OrderByDescending(x => x.dispute.RaisedAt).Take(100).ToListAsync(cancellationToken);

        return list.Select(x =>
        {
            IReadOnlyList<string>? evidenceList = null;
            if (!string.IsNullOrWhiteSpace(x.dispute.EvidenceUrls))
            {
                try
                {
                    evidenceList = JsonSerializer.Deserialize<IReadOnlyList<string>>(x.dispute.EvidenceUrls);
                }
                catch { }
            }

            var customerName = x.user?.FullName ?? "Doanh Nghiệp B2B";
            var supplierName = x.shop?.Name ?? x.supplier?.CompanyName ?? "Xưởng Bánh Trung Thu";
            var heldEscrow = x.contract.DepositAmount ?? (x.contract.TotalValue * (DefaultDepositPercentage / 100m)) ?? 0m;

            return new Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses.ContractDisputeListItemResponse(
                x.dispute.Id,
                x.contract.Id,
                x.contract.ContractNumber ?? "HĐ-B2B",
                customerName,
                supplierName,
                x.dispute.DisputeReason,
                evidenceList,
                x.dispute.Status,
                x.dispute.ResolutionType,
                x.dispute.AdminVerdict,
                x.dispute.RefundAmount,
                heldEscrow,
                x.dispute.RaisedAt,
                x.dispute.ResolvedAt
            );
        }).ToList();
    }

    public Task<ContractDispute?> GetDisputeByIdAsync(Guid disputeId, CancellationToken cancellationToken = default) =>
        context.ContractDisputes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == disputeId && !d.IsDeleted, cancellationToken);
}
