using PizzaNight.Models;
using PizzaNight.Services;
using Xunit;

namespace PizzaNight.Tests;

public sealed class OrderStatusWorkflowTests
{
    [Fact]
    public void Pending_order_can_only_be_accepted_or_cancelled()
    {
        var order = CreateOrder(OrderStatus.Pending, OrderType.Delivery);

        var transitions = OrderStatusWorkflow.GetAvailableTransitions(order);

        Assert.Equal([OrderStatus.Accepted, OrderStatus.Cancelled], transitions);
    }

    [Fact]
    public void Delivery_ready_order_can_be_sent_out_but_collection_cannot()
    {
        var delivery = CreateOrder(OrderStatus.Ready, OrderType.Delivery);
        var collection = CreateOrder(OrderStatus.Ready, OrderType.Collection);

        Assert.Contains(OrderStatus.OutForDelivery, OrderStatusWorkflow.GetAvailableTransitions(delivery));
        Assert.DoesNotContain(OrderStatus.OutForDelivery, OrderStatusWorkflow.GetAvailableTransitions(collection));
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void Closed_order_has_no_next_status(OrderStatus status)
    {
        var order = CreateOrder(status, OrderType.Delivery);

        Assert.Empty(OrderStatusWorkflow.GetAvailableTransitions(order));
    }

    private static Order CreateOrder(OrderStatus status, OrderType type) => new()
    {
        OrderNumber = "PK-TEST",
        CustomerName = "Test Customer",
        CustomerEmail = "test@example.com",
        CustomerPhone = "07123456789",
        Status = status,
        Type = type
    };
}
