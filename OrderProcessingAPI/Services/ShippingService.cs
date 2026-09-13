using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Domain.Enums;
using OrderProcessingAPI.Repositories;

namespace OrderProcessingAPI.Services;

public sealed class ShippingService(IShipmentRepository shipments) : IShippingService
{
    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => shipments.GetByOrderIdAsync(orderId, cancellationToken);

    public async Task<Shipment> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        var existing = await shipments.GetByOrderIdAsync(order.Id, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        if (order.FailShipment)
        {
            throw new InvalidOperationException("Эмуляция сбоя службы доставки (FailShipment=true).");
        }

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = ShipmentStatus.Created,
            TrackingNumber = $"TRK-{order.Id.ToString("N")[..8].ToUpperInvariant()}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await shipments.AddAsync(shipment, cancellationToken);
        await shipments.SaveChangesAsync(cancellationToken);
        return shipment;
    }

    public async Task CancelAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetByOrderIdAsync(orderId, cancellationToken);
        if (shipment is null || shipment.Status == ShipmentStatus.Cancelled)
        {
            return;
        }

        shipment.Status = ShipmentStatus.Cancelled;
        shipment.UpdatedAtUtc = DateTime.UtcNow;
        await shipments.SaveChangesAsync(cancellationToken);
    }
}
