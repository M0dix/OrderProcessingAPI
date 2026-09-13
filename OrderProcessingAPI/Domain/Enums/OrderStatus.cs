namespace OrderProcessingAPI.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Reserved = 1,
    Paid = 2,
    Shipped = 3,
    Failed = 10,
    Compensated = 11
}
