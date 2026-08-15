using System.ComponentModel.DataAnnotations;

namespace PizzaNight.Models;

public sealed class AdminLoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed record AdminOrdersIndexViewModel(
    IReadOnlyList<Order> Orders,
    OrderStatus? ActiveStatus,
    int PendingCount,
    int ActiveCount,
    int CompletedTodayCount,
    int CancelledTodayCount);

public sealed record AdminOrderDetailsViewModel(
    Order Order,
    IReadOnlyList<OrderStatus> AvailableStatuses);

public static class OrderDisplayExtensions
{
    public static string ToDisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pending",
        OrderStatus.Accepted => "Accepted",
        OrderStatus.Preparing => "Preparing",
        OrderStatus.Ready => "Ready",
        OrderStatus.OutForDelivery => "Out for delivery",
        OrderStatus.Completed => "Completed",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string ToCssClass(this OrderStatus status) =>
        $"status-{status.ToString().ToLowerInvariant()}";
}
