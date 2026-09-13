using Microsoft.AspNetCore.Mvc;
using OrderProcessingAPI.Contracts.Dtos;
using OrderProcessingAPI.Services;
using OrderProcessingAPI.Services.Mapping;

namespace OrderProcessingAPI.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly IInventoryService _inventoryService;

    public OrdersController(OrderService orderService, IInventoryService inventoryService)
    {
        _orderService = orderService;
        _inventoryService = inventoryService;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _orderService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetRecent([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await _orderService.GetRecentAsync(Math.Clamp(take, 1, 100), cancellationToken));
    }

    [HttpGet("/api/inventory")]
    public async Task<ActionResult<IReadOnlyList<StockResponse>>> GetInventory(CancellationToken cancellationToken)
    {
        var stock = await _inventoryService.GetAllStockAsync(cancellationToken);
        return Ok(stock.Select(OrderMapper.ToStock).ToList());
    }

    [HttpGet("/api/outbox")]
    public async Task<ActionResult<IReadOnlyList<OutboxMessageResponse>>> GetOutbox(CancellationToken cancellationToken)
    {
        return Ok(await _orderService.GetOutboxAsync(cancellationToken));
    }
}
