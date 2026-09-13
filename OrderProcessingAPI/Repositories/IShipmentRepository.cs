using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
