using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Data;
using PizzaNight.Models;
using PizzaNight.Services;

namespace PizzaNight.Controllers;

[Authorize(Roles = "Administrator")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route("admin")]
public sealed class AdminOrdersController(PizzaNightDbContext dbContext) : Controller
{
    [HttpGet("")]
    [HttpGet("orders")]
    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        OrderStatus? activeStatus = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            activeStatus = parsedStatus;
        }

        var ordersQuery = dbContext.Orders.AsNoTracking();
        if (activeStatus.HasValue)
        {
            ordersQuery = ordersQuery.Where(order => order.Status == activeStatus.Value);
        }

        var orders = await ordersQuery
            .OrderByDescending(order => order.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var todayUtc = DateTime.UtcNow.Date;
        var counts = await dbContext.Orders
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Pending = group.Count(order => order.Status == OrderStatus.Pending),
                Active = group.Count(order => order.Status == OrderStatus.Accepted
                    || order.Status == OrderStatus.Preparing
                    || order.Status == OrderStatus.Ready
                    || order.Status == OrderStatus.OutForDelivery),
                CompletedToday = group.Count(order => order.Status == OrderStatus.Completed && order.CreatedAtUtc >= todayUtc),
                CancelledToday = group.Count(order => order.Status == OrderStatus.Cancelled && order.CreatedAtUtc >= todayUtc)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return View(new AdminOrdersIndexViewModel(
            orders,
            activeStatus,
            counts?.Pending ?? 0,
            counts?.Active ?? 0,
            counts?.CompletedToday ?? 0,
            counts?.CancelledToday ?? 0));
    }

    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Items)
                .ThenInclude(item => item.Options)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return View(new AdminOrderDetailsViewModel(
            order,
            OrderStatusWorkflow.GetAvailableTransitions(order)));
    }

    [HttpPost("orders/{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        string status,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<OrderStatus>(status, true, out var nextStatus)
            || !OrderStatusWorkflow.CanTransition(order, nextStatus))
        {
            TempData["Error"] = "That status change is not allowed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Status = nextStatus;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Order {order.OrderNumber} is now {nextStatus.ToDisplayName().ToLowerInvariant()}.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
