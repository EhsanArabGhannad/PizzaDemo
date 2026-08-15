using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PizzaNight.Contracts;
using PizzaNight.Services;

namespace PizzaNight.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(OrderSubmissionService orderSubmissionService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("orders")]
    [RequestSizeLimit(64 * 1024)]
    [ProducesResponseType<CreateOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderSubmissionService.SubmitAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["order"] = result.Errors.ToArray()
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The order could not be submitted."
            });
        }

        var order = result.Order!;
        return StatusCode(StatusCodes.Status201Created, new CreateOrderResponse(
            order.OrderNumber,
            order.Status.ToString().ToLowerInvariant(),
            order.Type.ToString().ToLowerInvariant(),
            ToPounds(order.SubtotalPence),
            ToPounds(order.DeliveryFeePence),
            ToPounds(order.ServiceFeePence),
            ToPounds(order.TotalPence),
            result.EstimatedTime!));
    }

    private static decimal ToPounds(int pence) => pence / 100m;
}
