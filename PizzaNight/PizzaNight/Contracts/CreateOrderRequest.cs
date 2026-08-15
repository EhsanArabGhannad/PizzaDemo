using System.ComponentModel.DataAnnotations;

namespace PizzaNight.Contracts;

public sealed class CreateOrderRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string CustomerName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 7)]
    public string CustomerPhone { get; init; } = string.Empty;

    [Required, StringLength(24)]
    public string OrderType { get; init; } = string.Empty;

    [StringLength(16)]
    public string? Postcode { get; init; }

    [StringLength(300)]
    public string? AddressLine { get; init; }

    [StringLength(1000)]
    public string? OrderNotes { get; init; }

    [Required, MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; init; } = [];
}

public sealed class CreateOrderItemRequest
{
    [Required, StringLength(120)]
    public string ProductId { get; init; } = string.Empty;

    [Range(1, 10)]
    public int Quantity { get; init; }

    public Dictionary<string, List<string>> Selections { get; init; } = [];
}

public sealed record CreateOrderResponse(
    string OrderNumber,
    string Status,
    string Type,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal ServiceFee,
    decimal Total,
    string EstimatedTime);
