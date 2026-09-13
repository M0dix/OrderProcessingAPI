using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderProcessingAPI.Infrastructure.Messaging;
using OrderProcessingAPI.Infrastructure.Persistence;
using OrderProcessingAPI.Repositories;
using OrderProcessingAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Processing API",
        Version = "v1",
        Description = "Конвейер заказа: Outbox + идемпотентный consumer + saga с компенсациями."
    });
});

var connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? "Data Source=orders.db;Cache=Shared";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IIdempotencyStore, IdempotencyStore>();

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<OrderSagaOrchestrator>();
builder.Services.AddScoped<DemoService>();

builder.Services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
builder.Services.AddSingleton(new OutboxPublisherState
{
    Enabled = builder.Configuration.GetValue("Outbox:PublisherEnabled", true)
});
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<OrderCreatedConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await DbSeeder.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program;
