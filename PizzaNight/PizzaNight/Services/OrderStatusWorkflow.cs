using PizzaNight.Models;

namespace PizzaNight.Services;

public static class OrderStatusWorkflow
{
    public static IReadOnlyList<OrderStatus> GetAvailableTransitions(Order order) => order.Status switch
    {
        OrderStatus.Pending => [OrderStatus.Accepted, OrderStatus.Cancelled],
        OrderStatus.Accepted => [OrderStatus.Preparing, OrderStatus.Cancelled],
        OrderStatus.Preparing => [OrderStatus.Ready, OrderStatus.Cancelled],
        OrderStatus.Ready when order.Type == OrderType.Delivery =>
            [OrderStatus.OutForDelivery, OrderStatus.Completed, OrderStatus.Cancelled],
        OrderStatus.Ready => [OrderStatus.Completed, OrderStatus.Cancelled],
        OrderStatus.OutForDelivery => [OrderStatus.Completed, OrderStatus.Cancelled],
        _ => []
    };

    public static bool CanTransition(Order order, OrderStatus nextStatus) =>
        GetAvailableTransitions(order).Contains(nextStatus);
}
