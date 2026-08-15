using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Data;
using PizzaNight.Services;
using Xunit;

namespace PizzaNight.Tests;

public sealed class ShopOperationsServiceTests
{
    [Fact]
    public async Task Weekly_schedule_blocks_orders_on_the_closed_day()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PizzaNightDbContext>().UseSqlite(connection).Options;
        await using var context = new PizzaNightDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await DbInitializer.SeedAsync(context);

        var settings = await context.ShopSettings.SingleAsync();
        settings.UseOpeningHours = true;
        await context.SaveChangesAsync();

        var provider = new FixedTimeProvider(new DateTimeOffset(2026, 8, 11, 18, 0, 0, TimeSpan.Zero));
        var snapshot = await new ShopOperationsService(context, provider).GetSnapshotAsync();

        Assert.Equal(DayOfWeek.Tuesday, snapshot.LocalNow.DayOfWeek);
        Assert.False(snapshot.IsAcceptingOrders);
        Assert.Contains("closed", snapshot.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DH8 5AR")]
    [InlineData("dh85ar")]
    [InlineData(" DH8   9ZZ ")]
    public async Task Postcode_coverage_ignores_spaces_and_letter_case(string postcode)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PizzaNightDbContext>().UseSqlite(connection).Options;
        await using var context = new PizzaNightDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await DbInitializer.SeedAsync(context);

        var snapshot = await new ShopOperationsService(context, TimeProvider.System).GetSnapshotAsync();

        Assert.True(snapshot.CoversPostcode(postcode));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
