namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed class DeliveryService(
    IDeliveryRepository repository,
    IDateTimeProvider clock) : IDeliveryService
{
    private static readonly Regex PhoneRegex = new(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<DeliveryResponse>> GetMineAsync(Guid userId, bool isSupplier, CancellationToken cancellationToken = default)
    {
        var deliveries = await repository.GetDeliveriesForUserAsync(userId, isSupplier, cancellationToken);
        var responses = new List<DeliveryResponse>(deliveries.Count);
        foreach (var delivery in deliveries)
        {
            var proofs = await repository.GetProofsAsync(delivery.Id, cancellationToken);
            responses.Add(ToResponse(delivery, proofs.Count));
        }
        return responses;
    }

    public async Task<DeliveryDetailResponse> GetAsync(Guid userId, Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy thông tin chuyến giao hàng.");

        var canAccess = await repository.CanAccessDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canAccess)
            throw new HttpException(403, "Bạn không có quyền truy cập thông tin chuyến giao hàng này.");

        var proofs = await repository.GetProofsAsync(deliveryId, cancellationToken);
        return ToDetailResponse(delivery, proofs.Select(ToProofResponse).ToArray());
    }

    public async Task<IReadOnlyList<DeliveryResponse>> GetByContractAsync(Guid userId, Guid contractId, CancellationToken cancellationToken = default)
    {
        var deliveries = await repository.GetByContractIdAsync(contractId, cancellationToken);
        var responses = new List<DeliveryResponse>(deliveries.Count);
        foreach (var delivery in deliveries)
        {
            var proofs = await repository.GetProofsAsync(delivery.Id, cancellationToken);
            responses.Add(ToResponse(delivery, proofs.Count));
        }
        return responses;
    }

    public async Task<IReadOnlyList<DeliveryResponse>> GetByOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var deliveries = await repository.GetByOrderIdAsync(orderId, cancellationToken);
        var responses = new List<DeliveryResponse>(deliveries.Count);
        foreach (var delivery in deliveries)
        {
            var proofs = await repository.GetProofsAsync(delivery.Id, cancellationToken);
            responses.Add(ToResponse(delivery, proofs.Count));
        }
        return responses;
    }

    public async Task<DeliveryResponse> CreateAsync(Guid userId, CreateDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        if ((!request.ContractId.HasValue && !request.OrderId.HasValue) ||
            (request.ContractId.HasValue && request.OrderId.HasValue))
        {
            throw new HttpException(400, "Chuyến giao hàng bắt buộc phải gắn với duy nhất một Hợp đồng hoặc một Đơn hàng.");
        }

        if (string.IsNullOrWhiteSpace(request.FromAddress))
            throw new HttpException(400, "Địa chỉ xuất hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(request.ToAddress))
            throw new HttpException(400, "Địa chỉ nhận hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(request.RecipientName))
            throw new HttpException(400, "Tên người nhận không được để trống.");

        if (string.IsNullOrWhiteSpace(request.RecipientPhone) || !PhoneRegex.IsMatch(request.RecipientPhone.Trim()))
            throw new HttpException(400, "Số điện thoại người nhận không hợp lệ (định dạng 10 số di động Việt Nam).");

        if (request.ScheduledDate.HasValue && request.ScheduledDate.Value < DateOnly.FromDateTime(clock.UtcNow.AddDays(-1)))
            throw new HttpException(400, "Ngày dự kiến giao hàng không được ở trong quá khứ.");

        var deliveryNumber = $"DEL-{clock.UtcNow:yyyyMM}-{Random.Shared.Next(1000, 9999)}";

        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            ContractId = request.ContractId,
            OrderId = request.OrderId,
            DeliveryNumber = deliveryNumber,
            DeliveryType = request.DeliveryType,
            Status = DeliveryStatus.Pending,
            ScheduledDate = request.ScheduledDate,
            FromAddress = request.FromAddress.Trim(),
            ToAddress = request.ToAddress.Trim(),
            RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(),
            TrackingCode = string.IsNullOrWhiteSpace(request.TrackingCode) ? null : request.TrackingCode.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedBy = userId,
            CreatedAt = clock.UtcNow
        };

        var created = await repository.CreateAsync(delivery, cancellationToken);
        return ToResponse(created, 0);
    }

    public async Task<DeliveryResponse> UpdateStatusAsync(Guid userId, Guid deliveryId, UpdateDeliveryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy thông tin chuyến giao hàng.");

        var canManage = await repository.CanManageDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canManage)
            throw new HttpException(403, "Bạn không có quyền cập nhật trạng thái chuyến giao hàng này.");

        ValidateStateTransition(delivery.Status, request.Status);

        if (request.Status == DeliveryStatus.Delivered)
        {
            var proofs = await repository.GetProofsAsync(deliveryId, cancellationToken);
            if (!proofs.Any(p => p.ProofType == ProofType.Delivery))
            {
                throw new HttpException(400, "Bắt buộc phải có bằng chứng giao hàng (Delivery PoD) trước khi chuyển sang trạng thái Đã giao hàng.");
            }
            delivery.ActualDeliveryDate = request.ActualDeliveryDate ?? DateOnly.FromDateTime(clock.UtcNow);
        }

        delivery.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            delivery.Notes = string.IsNullOrWhiteSpace(delivery.Notes)
                ? request.Notes.Trim()
                : $"{delivery.Notes}\n[{clock.UtcNow:yyyy-MM-dd HH:mm}] {request.Notes.Trim()}";
        }
        delivery.UpdatedBy = userId;
        delivery.UpdatedAt = clock.UtcNow;

        var updated = await repository.UpdateAsync(delivery, cancellationToken);
        var proofCount = (await repository.GetProofsAsync(deliveryId, cancellationToken)).Count;
        return ToResponse(updated, proofCount);
    }

    public async Task<DeliveryResponse> UpdateCarrierInfoAsync(Guid userId, Guid deliveryId, UpdateCarrierInfoRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy thông tin chuyến giao hàng.");

        var canManage = await repository.CanManageDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canManage)
            throw new HttpException(403, "Bạn không có quyền cập nhật thông tin vận chuyển này.");

        if (!string.IsNullOrWhiteSpace(request.TrackingCode))
            delivery.TrackingCode = request.TrackingCode.Trim();

        if (!string.IsNullOrWhiteSpace(request.RecipientName))
            delivery.RecipientName = request.RecipientName.Trim();

        if (!string.IsNullOrWhiteSpace(request.RecipientPhone))
        {
            if (!PhoneRegex.IsMatch(request.RecipientPhone.Trim()))
                throw new HttpException(400, "Số điện thoại người nhận không hợp lệ.");
            delivery.RecipientPhone = request.RecipientPhone.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.FromAddress))
            delivery.FromAddress = request.FromAddress.Trim();

        if (!string.IsNullOrWhiteSpace(request.ToAddress))
            delivery.ToAddress = request.ToAddress.Trim();

        if (!string.IsNullOrWhiteSpace(request.Notes))
            delivery.Notes = request.Notes.Trim();

        delivery.UpdatedBy = userId;
        delivery.UpdatedAt = clock.UtcNow;

        var updated = await repository.UpdateAsync(delivery, cancellationToken);
        var proofCount = (await repository.GetProofsAsync(deliveryId, cancellationToken)).Count;
        return ToResponse(updated, proofCount);
    }

    public async Task<DeliveryProofResponse> SubmitProofAsync(Guid userId, Guid deliveryId, CreateDeliveryProofRequest request, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetByIdAsync(deliveryId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy thông tin chuyến giao hàng.");

        var canManage = await repository.CanManageDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canManage)
            throw new HttpException(403, "Bạn không có quyền nộp bằng chứng cho chuyến giao hàng này.");

        if (delivery.Status == DeliveryStatus.Delivered)
            throw new HttpException(409, "Chuyến giao hàng này đã được xác nhận hoàn tất trước đó.");

        if (request.LocationLat.HasValue && (request.LocationLat.Value < -90m || request.LocationLat.Value > 90m))
            throw new HttpException(400, "Tọa độ vĩ độ (Latitude) không hợp lệ [-90, 90].");

        if (request.LocationLng.HasValue && (request.LocationLng.Value < -180m || request.LocationLng.Value > 180m))
            throw new HttpException(400, "Tọa độ kinh độ (Longitude) không hợp lệ [-180, 180].");

        var photoUrl = request.PhotoUrl ?? request.PhotoBase64;
        var signatureUrl = request.SignatureUrl ?? request.SignatureBase64;

        if (string.IsNullOrWhiteSpace(photoUrl))
            throw new HttpException(400, "Ảnh chụp bằng chứng hiện trường là bắt buộc.");

        if (request.ProofType == ProofType.Delivery && string.IsNullOrWhiteSpace(signatureUrl))
            throw new HttpException(400, "Chữ ký người nhận là bắt buộc khi xác nhận bàn giao thành công.");

        var rawPayload = $"{deliveryId}|{request.ProofType}|{request.LocationLat}|{request.LocationLng}|{clock.UtcNow:O}|{photoUrl}|{signatureUrl}";
        var tamperProofHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPayload))).ToLowerInvariant();

        var proof = new DeliveryProof
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            ProofType = request.ProofType,
            PhotoUrl = photoUrl,
            SignatureUrl = signatureUrl,
            TakenBy = string.IsNullOrWhiteSpace(request.TakenBy) ? "Tài xế giao hàng" : request.TakenBy.Trim(),
            TakenAt = clock.UtcNow,
            LocationLat = request.LocationLat,
            LocationLng = request.LocationLng,
            Notes = request.Notes?.Trim(),
            TamperProofHash = tamperProofHash,
            CreatedBy = userId,
            CreatedAt = clock.UtcNow
        };

        var createdProof = await repository.CreateProofAsync(proof, cancellationToken);

        if (request.ProofType == ProofType.Delivery)
        {
            delivery.Status = DeliveryStatus.Delivered;
            delivery.ActualDeliveryDate = DateOnly.FromDateTime(clock.UtcNow);
            delivery.UpdatedBy = userId;
            delivery.UpdatedAt = clock.UtcNow;
            await repository.UpdateAsync(delivery, cancellationToken);
        }
        else if (request.ProofType == ProofType.Pickup && (delivery.Status == DeliveryStatus.Pending || delivery.Status == DeliveryStatus.Confirmed))
        {
            delivery.Status = DeliveryStatus.PickedUp;
            delivery.UpdatedBy = userId;
            delivery.UpdatedAt = clock.UtcNow;
            await repository.UpdateAsync(delivery, cancellationToken);
        }
        else if (request.ProofType == ProofType.FailedAttempt)
        {
            delivery.Status = DeliveryStatus.Failed;
            delivery.UpdatedBy = userId;
            delivery.UpdatedAt = clock.UtcNow;
            await repository.UpdateAsync(delivery, cancellationToken);
        }

        return ToProofResponse(createdProof);
    }

    public async Task<IReadOnlyList<DeliveryProofResponse>> GetProofsAsync(Guid userId, Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var canAccess = await repository.CanAccessDeliveryAsync(deliveryId, userId, cancellationToken);
        if (!canAccess)
            throw new HttpException(403, "Bạn không có quyền truy cập bằng chứng của chuyến giao hàng này.");

        var proofs = await repository.GetProofsAsync(deliveryId, cancellationToken);
        return proofs.Select(ToProofResponse).ToArray();
    }

    public async Task<DeliveryProofResponse> GetProofDetailAsync(Guid userId, Guid proofId, CancellationToken cancellationToken = default)
    {
        var proof = await repository.GetProofByIdAsync(proofId, cancellationToken)
            ?? throw new HttpException(404, "Không tìm thấy thông tin bằng chứng PoD.");

        var canAccess = await repository.CanAccessDeliveryAsync(proof.DeliveryId, userId, cancellationToken);
        if (!canAccess)
            throw new HttpException(403, "Bạn không có quyền truy cập bằng chứng này.");

        return ToProofResponse(proof);
    }

    public async Task<bool> VerifyProofIntegrityAsync(Guid proofId, CancellationToken cancellationToken = default)
    {
        var proof = await repository.GetProofByIdAsync(proofId, cancellationToken);
        if (proof == null || string.IsNullOrWhiteSpace(proof.TamperProofHash))
            return false;

        return proof.TamperProofHash.Length == 64;
    }

    private static void ValidateStateTransition(DeliveryStatus current, DeliveryStatus next)
    {
        if (current == next) return;

        var isValid = current switch
        {
            DeliveryStatus.Pending => next is DeliveryStatus.Confirmed or DeliveryStatus.PickedUp or DeliveryStatus.Failed,
            DeliveryStatus.Confirmed => next is DeliveryStatus.PickedUp or DeliveryStatus.InTransit or DeliveryStatus.Failed,
            DeliveryStatus.PickedUp => next is DeliveryStatus.InTransit or DeliveryStatus.Failed,
            DeliveryStatus.InTransit => next is DeliveryStatus.Delivered or DeliveryStatus.Failed or DeliveryStatus.Returned,
            DeliveryStatus.Failed => next is DeliveryStatus.InTransit or DeliveryStatus.Returned,
            DeliveryStatus.Delivered => false,
            DeliveryStatus.Returned => false,
            _ => false
        };

        if (!isValid)
        {
            throw new HttpException(400, $"Không thể chuyển đổi trạng thái chuyến giao từ '{current}' sang '{next}'.");
        }
    }

    private static DeliveryResponse ToResponse(Delivery d, int proofCount) => new()
    {
        Id = d.Id,
        ContractId = d.ContractId,
        OrderId = d.OrderId,
        DeliveryNumber = d.DeliveryNumber,
        DeliveryType = d.DeliveryType,
        Status = d.Status,
        ScheduledDate = d.ScheduledDate,
        ActualDeliveryDate = d.ActualDeliveryDate,
        FromAddress = d.FromAddress,
        ToAddress = d.ToAddress,
        RecipientName = d.RecipientName,
        RecipientPhone = d.RecipientPhone,
        TrackingCode = d.TrackingCode,
        Notes = d.Notes,
        ProofCount = proofCount,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };

    private static DeliveryDetailResponse ToDetailResponse(Delivery d, IReadOnlyList<DeliveryProofResponse> proofs) => new()
    {
        Id = d.Id,
        ContractId = d.ContractId,
        OrderId = d.OrderId,
        DeliveryNumber = d.DeliveryNumber,
        DeliveryType = d.DeliveryType,
        Status = d.Status,
        ScheduledDate = d.ScheduledDate,
        ActualDeliveryDate = d.ActualDeliveryDate,
        FromAddress = d.FromAddress,
        ToAddress = d.ToAddress,
        RecipientName = d.RecipientName,
        RecipientPhone = d.RecipientPhone,
        TrackingCode = d.TrackingCode,
        Notes = d.Notes,
        ProofCount = proofs.Count,
        Proofs = proofs,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };

    private static DeliveryProofResponse ToProofResponse(DeliveryProof p) => new()
    {
        Id = p.Id,
        DeliveryId = p.DeliveryId,
        ProofType = p.ProofType,
        PhotoUrl = p.PhotoUrl,
        SignatureUrl = p.SignatureUrl,
        TakenBy = p.TakenBy,
        TakenAt = p.TakenAt,
        LocationLat = p.LocationLat,
        LocationLng = p.LocationLng,
        Notes = p.Notes,
        TamperProofHash = p.TamperProofHash,
        IsVerified = !string.IsNullOrWhiteSpace(p.TamperProofHash),
        CreatedAt = p.CreatedAt
    };
}
