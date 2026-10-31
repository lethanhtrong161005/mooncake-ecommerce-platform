namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Users.Models;

/// <summary>DTO-only persistence contract for administrator user management.</summary>
public interface IUserManagementRepository
{
    Task<IReadOnlyList<ManagedUserData>> GetAllManagedUsersAsync(CancellationToken cancellationToken = default);
    Task<ManagedUserData?> GetManagedUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ManagedEmailExistsAsync(string email, Guid? exceptUserId = null, CancellationToken cancellationToken = default);
    Task<bool> ManagedPhoneExistsAsync(string phone, Guid? exceptUserId = null, CancellationToken cancellationToken = default);
    Task<ManagedUserData> CreateManagedUserAsync(CreateManagedUserData user, CancellationToken cancellationToken = default);
    Task<ManagedUserData> UpdateManagedUserAsync(UpdateManagedUserData user, CancellationToken cancellationToken = default);
    Task DeleteManagedUserAsync(Guid id, CancellationToken cancellationToken = default);
}
