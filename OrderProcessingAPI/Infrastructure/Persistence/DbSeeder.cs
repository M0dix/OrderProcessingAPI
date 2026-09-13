using Microsoft.EntityFrameworkCore;
using OrderProcessingAPI.Domain.Entities;

namespace OrderProcessingAPI.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.ProductStocks.AnyAsync(cancellationToken))
        {
            return;
        }

        db.ProductStocks.AddRange(
            new ProductStock
            {
                Sku = "SKU-BOOK",
                Name = "Distributed Systems in Practice",
                Available = 20,
                Price = 49.90m
            },
            new ProductStock
            {
                Sku = "SKU-MUG",
                Name = "Outbox Pattern Mug",
                Available = 8,
                Price = 15.00m
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
