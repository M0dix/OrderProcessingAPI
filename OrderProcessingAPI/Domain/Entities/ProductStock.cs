namespace OrderProcessingAPI.Domain.Entities;

public class ProductStock
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Available { get; set; }
    public decimal Price { get; set; }
}
