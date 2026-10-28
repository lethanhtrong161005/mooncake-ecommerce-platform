namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Private, bounded document access; never accepts a client-supplied URL.</summary>
public interface IVerificationDocumentGateway
{
    long MaxFileBytes { get; }
    Task<byte[]> ReadAsync(SupplierVerificationDocument document, CancellationToken cancellationToken);
}

/// <summary>Minimal user-addressed notifications, implemented by the HTTP host.</summary>
public interface IVerificationRealtimePublisher
{
    Task PublishAsync(IReadOnlyList<Guid> userIds, string eventName, object payload, CancellationToken cancellationToken);
}
