namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Admin;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class SystemOperationsTests
{
  private readonly Mock<INotificationRepository> notifRepoMock = new();
  private readonly Mock<IRealtimeNotificationPublisher> publisherMock = new();
  private readonly Mock<IContractRepository> contractRepoMock = new();
  private readonly Mock<IAuthService> authMock = new();
  private readonly Mock<ISupplierRepository> supplierRepoMock = new();
  private readonly Mock<IDateTimeProvider> clockMock = new();
  private readonly Mock<IAdminPlatformRepository> adminRepoMock = new();
  private readonly Mock<IUserRepository> userRepoMock = new();
  private readonly Guid userId = Guid.NewGuid();

  [Fact]
  public async Task NotificationService_DispatchNotification_SavesEntityAndBroadcastsRealtimeEvent()
  {
    // Arrange
    notifRepoMock.Setup(r => r.CreateNotificationAsync(It.IsAny<Notification>(), default))
        .ReturnsAsync((Notification n, CancellationToken ct) => n);
    notifRepoMock.Setup(r => r.GetUnreadCountAsync(userId, default))
        .ReturnsAsync(1);

    var service = new NotificationService(notifRepoMock.Object, publisherMock.Object);

    // Act
    var result = await service.DispatchNotificationAsync(
        userId,
        "payment_vnpay_success",
        "Thanh toán VNPay Sandbox thành công",
        "Đã nhận 120.000.000 ₫ tiền cọc mốc M1 cho hợp đồng CTR-2026-00012.",
        "contract",
        Guid.NewGuid(),
        "/#/hop-dong-tien-do",
        "high");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("payment_vnpay_success", result.Type);
    Assert.Equal("payment", result.Category);
    Assert.Equal("high", result.Priority);
    Assert.False(result.IsRead);

    notifRepoMock.Verify(r => r.CreateNotificationAsync(It.IsAny<Notification>(), default), Times.Once);
    publisherMock.Verify(p => p.PublishToUserAsync(userId, "ReceiveNotification", It.IsAny<object>(), default), Times.Once);
    publisherMock.Verify(p => p.PublishToUserAsync(userId, "UnreadCountUpdated", It.IsAny<object>(), default), Times.Once);
  }

  [Fact]
  public async Task NotificationService_MarkAsRead_UpdatesIsReadAndSyncsCount()
  {
    // Arrange
    var notificationId = Guid.NewGuid();
    notifRepoMock.Setup(r => r.MarkAsReadAsync(userId, notificationId, default))
        .ReturnsAsync(true);
    notifRepoMock.Setup(r => r.GetUnreadCountAsync(userId, default))
        .ReturnsAsync(0);

    var service = new NotificationService(notifRepoMock.Object, publisherMock.Object);

    // Act
    var marked = await service.MarkAsReadAsync(userId, notificationId);

    // Assert
    Assert.True(marked);
    notifRepoMock.Verify(r => r.MarkAsReadAsync(userId, notificationId, default), Times.Once);
    publisherMock.Verify(p => p.PublishToUserAsync(userId, "NotificationsMarkedAsRead", It.IsAny<object>(), default), Times.Once);
  }

  [Fact]
  public async Task NotificationService_MarkAllAsRead_ResetsAllToRead()
  {
    // Arrange
    notifRepoMock.Setup(r => r.MarkAllAsReadAsync(userId, "all", It.IsAny<DateTime?>(), default))
        .ReturnsAsync(3);

    var service = new NotificationService(notifRepoMock.Object, publisherMock.Object);

    // Act
    var updated = await service.MarkAllAsReadAsync(userId, "all", DateTime.UtcNow);

    // Assert
    Assert.Equal(3, updated);
    publisherMock.Verify(p => p.PublishToUserAsync(userId, "AllNotificationsRead", It.IsAny<object>(), default), Times.Once);
  }

  [Fact]
  public async Task NotificationService_GetInboxAsync_ReturnsPagedResponse()
  {
    // Arrange
    var items = new List<NotificationItemDto>
        {
            new(Guid.NewGuid(), "payment_vnpay_success", "payment", "Thanh toán thành công", "Chi tiết", null, null, null, "high", false, null, DateTime.UtcNow)
        };
    var expectedResponse = new NotificationInboxResponse(items, 1, 20, 1, 1, 1);

    notifRepoMock.Setup(r => r.GetInboxAsync(userId, 1, 20, "all", null, default))
        .ReturnsAsync(expectedResponse);

    var service = new NotificationService(notifRepoMock.Object, publisherMock.Object);

    // Act
    var result = await service.GetInboxAsync(userId, 1, 20, "all", null);

    // Assert
    Assert.NotNull(result);
    Assert.Single(result.Items);
    Assert.Equal(1, result.UnreadCount);
  }

  [Fact]
  public async Task NotificationService_Preferences_SaveAndRetrieveSuccessfully()
  {
    // Arrange
    var expected = new NotificationPreferencesDto(EnableInApp: true, EnableWebPush: false, EnableEmailDigest: true);
    notifRepoMock.Setup(r => r.GetPreferencesAsync(userId, default))
        .ReturnsAsync(expected);
    notifRepoMock.Setup(r => r.UpdatePreferencesAsync(userId, expected, default))
        .ReturnsAsync(expected);

    var service = new NotificationService(notifRepoMock.Object, publisherMock.Object);

    // Act
    var fetched = await service.GetPreferencesAsync(userId);
    var updated = await service.UpdatePreferencesAsync(userId, expected);

    // Assert
    Assert.Equal(expected, fetched);
    Assert.Equal(expected, updated);
  }

  [Fact]
  public async Task ContractService_ListDisputes_ReturnsDisputeListWithDetails()
  {
    // Arrange
    var contractId = Guid.NewGuid();
    var disputeId = Guid.NewGuid();
    var disputeItems = new List<ContractDisputeListItemResponse>
        {
            new(
                disputeId,
                contractId,
                "CTR-2026-00012",
                "Công Ty B2B Hoa Mai",
                "Xưởng Bánh Tràng An Luxury",
                "Xưởng giao bánh chậm 7 ngày so với tiến độ mốc M2",
                new[] { "https://evidence.jpg" },
                "Pending",
                null,
                null,
                0,
                120000000,
                DateTime.UtcNow,
                null)
        };

    contractRepoMock.Setup(r => r.ListDisputesAsync(null, default))
        .ReturnsAsync(disputeItems);

    var service = new ContractService(contractRepoMock.Object, authMock.Object, supplierRepoMock.Object, clockMock.Object);

    // Act
    var result = await service.ListDisputesAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Single(result);
    var item = result[0];
    Assert.Equal("CTR-2026-00012", item.ContractNumber);
    Assert.Equal("Pending", item.Status);
    Assert.Equal(120000000, item.HeldEscrowAmount);
  }

  [Fact]
  public async Task ContractService_ResolveDisputeByIdAsync_CallsRepositoryAndReturnsResolvedState()
  {
    // Arrange
    var contractId = Guid.NewGuid();
    var disputeId = Guid.NewGuid();
    var adminUserId = Guid.NewGuid();

    var disputeEntity = new ContractDispute
    {
      Id = disputeId,
      ContractId = contractId,
      Status = "Pending"
    };

    var contractEntity = new Contract
    {
      Id = contractId,
      Status = Mooncake.EcommercePlatform.Domain.Enums.ContractStatus.Disputed
    };

    var resolvedDispute = new ContractDispute
    {
      Id = disputeId,
      ContractId = contractId,
      Status = "Resolved",
      ResolutionType = "SupplierAtFault",
      RefundAmount = 120000000,
      AdminVerdict = "Xưởng chậm giao hàng.",
      ResolvedAt = DateTime.UtcNow
    };

    contractRepoMock.Setup(r => r.GetDisputeByIdAsync(disputeId, default))
        .ReturnsAsync(disputeEntity);
    contractRepoMock.Setup(r => r.GetContractAsync(contractId, default))
        .ReturnsAsync(contractEntity);
    contractRepoMock.Setup(r => r.ResolveDisputeAsync(contractId, "SupplierAtFault", 120000000, "Xưởng chậm giao hàng.", adminUserId, default))
        .ReturnsAsync(resolvedDispute);

    var service = new ContractService(contractRepoMock.Object, authMock.Object, supplierRepoMock.Object, clockMock.Object);

    var request = new AdminResolveDisputeRequest
    {
      ResolutionType = "SupplierAtFault",
      RefundAmount = 120000000,
      AdminVerdict = "Xưởng chậm giao hàng."
    };

    // Act
    var response = await service.ResolveDisputeByIdAsync(adminUserId, disputeId, request);

    // Assert
    Assert.NotNull(response);
    Assert.Equal("Resolved", response.Status);
    Assert.Equal("SupplierAtFault", response.ResolutionType);
    Assert.Equal(120000000, response.RefundAmount);
  }

  [Fact]
  public async Task AdminPlatformService_GetDashboardAsync_ReturnsAggregatedMetrics()
  {
    // Arrange
    var metrics = new AdminPlatformMetrics(
        TotalUsers: 120,
        ActiveUsers: 110,
        PendingSuppliers: 5,
        ActiveShops: 40,
        TotalOrders: 350,
        PaidGmv: 4500000000m,
        PendingPayments: 8,
        DisputedContracts: 2,
        TopSuppliers: [new AdminTopSupplierMetric(Guid.NewGuid(), "Xưởng Bánh Kinh Đô", 1500000000m)],
        TopProducts: [new AdminTopProductMetric(Guid.NewGuid(), "Bánh Trung Thu Thập Cẩm Bát Bửu", 2000, 500000000m)]);

    adminRepoMock.Setup(r => r.GetMetricsAsync(default)).ReturnsAsync(metrics);

    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act
    var result = await service.GetDashboardAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(120, result.TotalUsers);
    Assert.Equal(4500000000m, result.PaidGmv);
    Assert.Equal(2, result.DisputedContracts);
    Assert.Single(result.TopSuppliers);
    Assert.Equal("Xưởng Bánh Kinh Đô", result.TopSuppliers[0].CompanyName);
  }

  [Fact]
  public async Task AdminPlatformService_SetSystemConfig_ValidJson_SavesAndAudits()
  {
    // Arrange
    var actorId = Guid.NewGuid();
    var now = DateTime.UtcNow;
    clockMock.Setup(c => c.UtcNow).Returns(now);
    adminRepoMock.Setup(r => r.GetSystemConfigAsync("escrow_hold_percentage", default))
        .ReturnsAsync((SystemConfig?)null);

    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act
    var result = await service.SetSystemConfigAsync("escrow_hold_percentage", "{\"percent\": 30}", "Tỷ lệ ký quỹ chuẩn", actorId, "127.0.0.1");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("escrow_hold_percentage", result.Key);
    Assert.Equal("{\"percent\": 30}", result.ValueJson);
    adminRepoMock.Verify(r => r.SaveSystemConfigAsync(It.Is<SystemConfig>(c => c.Key == "escrow_hold_percentage"), default), Times.Once);
    adminRepoMock.Verify(r => r.SaveAuditAsync(It.Is<AuditLog>(a => a.ActorUserId == actorId && a.Action == "SystemConfigUpdated"), default), Times.Once);
  }

  [Fact]
  public async Task AdminPlatformService_SetSystemConfig_InvalidJson_ThrowsBadRequest()
  {
    // Arrange
    var actorId = Guid.NewGuid();
    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<HttpException>(() =>
        service.SetSystemConfigAsync("invalid_key", "{invalid_json", "Description", actorId, null));
    Assert.Equal(400, ex.StatusCode);
  }

  [Fact]
  public async Task AdminPlatformService_SetSystemConfig_WhitespaceKey_ThrowsBadRequest()
  {
    // Arrange
    var actorId = Guid.NewGuid();
    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<HttpException>(() =>
        service.SetSystemConfigAsync("   ", "{}", "Description", actorId, null));
    Assert.Equal(400, ex.StatusCode);
  }

  [Fact]
  public async Task AdminPlatformService_GetAuditLogs_InvalidPage_ThrowsBadRequest()
  {
    // Arrange
    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<HttpException>(() => service.GetAuditLogsAsync(0, 50));
    Assert.Equal(400, ex.StatusCode);
  }

  [Fact]
  public async Task AdminPlatformService_UpdateUser_DeactivatingUser_RevokesSessionsAndLogsAudit()
  {
    // Arrange
    var adminId = Guid.NewGuid();
    var targetUserId = Guid.NewGuid();
    var now = DateTime.UtcNow;
    clockMock.Setup(c => c.UtcNow).Returns(now);

    supplierRepoMock.Setup(s => s.InTransactionAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
        .Returns((IReadOnlyList<Guid> ids, Func<CancellationToken, Task<bool>> action, CancellationToken ct) => action(ct));
    supplierRepoMock.Setup(s => s.GetUserAsync(adminId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new User { Id = adminId, Role = UserRole.Admin, IsActive = true });

    var targetUser = new User { Id = targetUserId, Role = UserRole.Customer, IsActive = true };
    adminRepoMock.Setup(r => r.GetUserForUpdateAsync(targetUserId, default)).ReturnsAsync(targetUser);

    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act
    await service.UpdateUserAsync(targetUserId, new AdminUserUpdateRequest(IsActive: false, Role: null), adminId, "127.0.0.1");

    // Assert
    Assert.False(targetUser.IsActive);
    userRepoMock.Verify(u => u.RevokeAllRefreshSessionsAsync(targetUserId, now, default), Times.Once);
    supplierRepoMock.Verify(s => s.AddAudit(It.Is<AuditLog>(a => a.ActorUserId == adminId && a.Action == "UserUpdatedByAdmin")), Times.Once);
  }

  [Fact]
  public async Task AdminPlatformService_UpdateUser_CannotAssignAdminRole_ThrowsBadRequest()
  {
    // Arrange
    var adminId = Guid.NewGuid();
    var targetUserId = Guid.NewGuid();

    supplierRepoMock.Setup(s => s.InTransactionAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
        .Returns((IReadOnlyList<Guid> ids, Func<CancellationToken, Task<bool>> action, CancellationToken ct) => action(ct));
    supplierRepoMock.Setup(s => s.GetUserAsync(adminId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new User { Id = adminId, Role = UserRole.Admin, IsActive = true });

    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<HttpException>(() =>
        service.UpdateUserAsync(targetUserId, new AdminUserUpdateRequest(IsActive: true, Role: UserRole.Admin), adminId, null));
    Assert.Equal(400, ex.StatusCode);
  }

  [Fact]
  public async Task AdminPlatformService_UpdateUser_UnverifiedSupplierRole_ThrowsConflict()
  {
    // Arrange
    var adminId = Guid.NewGuid();
    var targetUserId = Guid.NewGuid();

    supplierRepoMock.Setup(s => s.InTransactionAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
        .Returns((IReadOnlyList<Guid> ids, Func<CancellationToken, Task<bool>> action, CancellationToken ct) => action(ct));
    supplierRepoMock.Setup(s => s.GetUserAsync(adminId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new User { Id = adminId, Role = UserRole.Admin, IsActive = true });

    var targetUser = new User { Id = targetUserId, Role = UserRole.Customer, IsActive = true };
    adminRepoMock.Setup(r => r.GetUserForUpdateAsync(targetUserId, default)).ReturnsAsync(targetUser);
    adminRepoMock.Setup(r => r.IsSupplierVerifiedAsync(targetUserId, default)).ReturnsAsync(false);

    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<HttpException>(() =>
        service.UpdateUserAsync(targetUserId, new AdminUserUpdateRequest(IsActive: true, Role: UserRole.Supplier), adminId, null));
    Assert.Equal(409, ex.StatusCode);
  }

  [Fact]
  public async Task AdminPlatformService_SaveContractDepositRule_CreatesNewRule()
  {
    // Arrange
    var adminId = Guid.NewGuid();
    var now = DateTime.UtcNow;
    clockMock.Setup(c => c.UtcNow).Returns(now);

    var service = new AdminPlatformService(adminRepoMock.Object, userRepoMock.Object, clockMock.Object, supplierRepoMock.Object);

    var request = new SaveContractDepositRuleRequest
    {
      Name = "Quy tắc cọc B2B VIP 30%",
      DepositPercentage = 30m,
      IsMandatory = true,
      MinContractValue = 50000000m,
      PaymentDeadlineHours = 48
    };

    // Act
    var response = await service.SaveContractDepositRuleAsync(null, request, adminId, "127.0.0.1");

    // Assert
    Assert.NotNull(response);
    Assert.Equal("Quy tắc cọc B2B VIP 30%", response.Name);
    Assert.Equal(30m, response.DepositPercentage);
    Assert.True(response.IsMandatory);
    Assert.Equal(50000000m, response.MinContractValue);
    adminRepoMock.Verify(r => r.SaveContractDepositRuleAndAuditAsync(
        It.Is<ContractDepositRule>(rule => rule.Name == "Quy tắc cọc B2B VIP 30%"),
        It.Is<AuditLog>(a => a.ActorUserId == adminId && a.Action == "ContractDepositRuleUpdated"),
        default), Times.Once);
  }
}
