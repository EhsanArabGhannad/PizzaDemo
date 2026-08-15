using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Contracts;
using PizzaNight.Data;
using PizzaNight.Services;
using Xunit;

namespace PizzaNight.Tests;

public sealed class OrderSubmissionServiceTests
{
    [Fact]
    public async Task Delivery_order_uses_database_prices_and_applies_each_fee_once()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var request = CreateRequest("delivery",
            new CreateOrderItemRequest
            {
                ProductId = "knights-special",
                Quantity = 2,
                Selections = new()
                {
                    ["size"] = ["medium"],
                    ["crust"] = ["stuffed"],
                    ["extras"] = ["extra-cheese"]
                }
            },
            new CreateOrderItemRequest
            {
                ProductId = "double-smash",
                Quantity = 1
            });

        var result = await fixture.Service.SubmitAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4087, result.Order!.SubtotalPence);
        Assert.Equal(250, result.Order.DeliveryFeePence);
        Assert.Equal(50, result.Order.ServiceFeePence);
        Assert.Equal(4387, result.Order.TotalPence);
        Assert.Equal(2, await fixture.Context.Orders.SelectMany(order => order.Items).CountAsync());
        Assert.Equal(3, await fixture.Context.OrderItemOptions.CountAsync());
    }

    [Fact]
    public async Task Collection_order_has_no_delivery_fee()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var request = CreateRequest("collection",
            new CreateOrderItemRequest
            {
                ProductId = "loaded-fries",
                Quantity = 1
            });

        var result = await fixture.Service.SubmitAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Order!.DeliveryFeePence);
        Assert.Equal(50, result.Order.ServiceFeePence);
        Assert.Equal(699, result.Order.TotalPence);
    }

    [Fact]
    public async Task Unknown_product_option_is_rejected_without_saving_an_order()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var request = CreateRequest("delivery",
            new CreateOrderItemRequest
            {
                ProductId = "knights-special",
                Quantity = 1,
                Selections = new()
                {
                    ["size"] = ["gigantic"],
                    ["crust"] = ["classic"],
                    ["extras"] = []
                }
            });

        var result = await fixture.Service.SubmitAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Contains("gigantic", StringComparison.Ordinal));
        Assert.Equal(0, await fixture.Context.Orders.CountAsync());
    }

    private static CreateOrderRequest CreateRequest(string orderType, params CreateOrderItemRequest[] items) => new()
    {
        CustomerName = "Automated Test",
        CustomerEmail = "test@example.com",
        CustomerPhone = "07123456789",
        OrderType = orderType,
        Postcode = orderType == "delivery" ? "DH8 5AA" : null,
        AddressLine = orderType == "delivery" ? "1 Test Street" : null,
        Items = [.. items]
    };

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, PizzaNightDbContext context)
        {
            this.connection = connection;
            Context = context;
            Service = new OrderSubmissionService(context);
        }

        public PizzaNightDbContext Context { get; }
        public OrderSubmissionService Service { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<PizzaNightDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new PizzaNightDbContext(options);
            await context.Database.EnsureCreatedAsync();
            await DbInitializer.SeedAsync(context);

            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
