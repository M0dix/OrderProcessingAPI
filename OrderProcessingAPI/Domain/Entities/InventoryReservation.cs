using OrderProcessingAPI.Domain.Enums;

namespace OrderProcessingAPI.Domain.Entities;

public class InventoryReservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
