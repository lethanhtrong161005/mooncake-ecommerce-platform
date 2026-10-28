namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Admin;

public interface IAdminPlatformService
{
    Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemConfigResponse>> GetSystemConfigsAsync(CancellationToken cancellationToken = default);
    Task<SystemConfigResponse> SetSystemConfigAsync(string key, string valueJson, string? description, Guid actorId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogResponse>> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(Guid userId, AdminUserUpdateRequest request, Guid actorId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContractDepositRuleResponse>> GetContractDepositRulesAsync(CancellationToken cancellationToken = default);
    Task<ContractDepositRuleResponse> SaveContractDepositRuleAsync(Guid? ruleId, SaveContractDepositRuleRequest request, Guid actorId, string? ipAddress, CancellationToken cancellationToken = default);
}
