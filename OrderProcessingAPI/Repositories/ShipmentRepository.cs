using Microsoft.EntityFrameworkCore;
using OrderProcessingAPI.Domain.Entities;
using OrderProcessingAPI.Infrastructure.Persistence;

namespace OrderProcessingAPI.Repositories;

public sealed class ShipmentRepository(AppDbContext db) : IShipmentRepository
{
    public Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => db.Shipments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

    public Task AddAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        db.Shipments.Add(shipment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
