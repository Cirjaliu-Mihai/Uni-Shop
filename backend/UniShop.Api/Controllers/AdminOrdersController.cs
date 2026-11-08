using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(IAdminOrderService orderService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderListDto>>> GetOrders(
        [FromQuery] AdminOrderQuery query,
        CancellationToken cancellationToken) =>
        Ok(await orderService.GetOrdersAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetOrderAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        int id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await orderService.UpdateStatusAsync(id, request.Status, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }
}