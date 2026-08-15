namespace PizzaNight.Models;

public enum OrderType
{
    Delivery,
    Collection
}

public enum OrderStatus
{
    Pending,
    Accepted,
    Preparing,
    Ready,
    OutForDelivery,
    Completed,
    Cancelled
}

public enum PaymentMethod
{
    NotSelected,
    Card,
    Cash
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public sealed class Order
{
    public int Id { get; set; }
    public required string OrderNumber { get; set; }
    public OrderType Type { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public required string CustomerPhone { get; set; }
    public string? Postcode { get; set; }
    public string? AddressLine { get; set; }
    public string? DeliveryInstructions { get; set; }
    public string? OrderNotes { get; set; }

    public int SubtotalPence { get; set; }
    public int DeliveryFeePence { get; set; }
    public int ServiceFeePence { get; set; }
    public int TotalPence { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RequestedForUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    public required string ProductName { get; set; }
    public int UnitPricePence { get; set; }
    public int Quantity { get; set; }
    public int LineTotalPence { get; set; }

    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
    public ICollection<OrderItemOption> Options { get; set; } = new List<OrderItemOption>();
}

public sealed class OrderItemOption
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public required string Name { get; set; }
    public int PriceAdjustmentPence { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
}
