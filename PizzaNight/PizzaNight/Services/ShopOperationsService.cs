using Microsoft.EntityFrameworkCore;
using PizzaNight.Data;
using PizzaNight.Models;

namespace PizzaNight.Services;

public sealed class ShopOperationsService(
    PizzaNightDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<ShopOperationsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.ShopSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? new ShopSettings();
        var hours = await dbContext.ShopOpeningHours
            .AsNoTracking()
            .OrderBy(item => item.DayOfWeek)
            .ToListAsync(cancellationToken);
        var zones = await dbContext.DeliveryZones
            .AsNoTracking()
            .Where(zone => zone.IsActive)
            .OrderBy(zone => zone.DisplayOrder)
            .ThenBy(zone => zone.Name)
            .ToListAsync(cancellationToken);

        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), GetShopTimeZone());
        var isWithinHours = !settings.UseOpeningHours || IsOpenAt(hours, localNow);
        var isAcceptingOrders = settings.AcceptingOnlineOrders && isWithinHours;
        var statusMessage = isAcceptingOrders
            ? "Open for online orders"
            : !settings.AcceptingOnlineOrders
                ? settings.TemporaryClosureMessage ?? "Online ordering is temporarily unavailable."
                : "We are currently closed. Please check today's opening hours.";

        return new ShopOperationsSnapshot(
            settings,
            hours,
            zones,
            localNow,
            isAcceptingOrders,
            statusMessage);
    }

    public static string NormalizePostcode(string? postcode) =>
        string.Concat((postcode ?? string.Empty).Where(character => !char.IsWhiteSpace(character)))
            .ToUpperInvariant();

    private static bool IsOpenAt(IReadOnlyCollection<ShopOpeningHour> hours, DateTimeOffset localNow)
    {
        var minute = localNow.Hour * 60 + localNow.Minute;
        var today = hours.SingleOrDefault(item => item.DayOfWeek == localNow.DayOfWeek);
        if (MatchesToday(today, minute))
        {
            return true;
        }

        var previousDay = localNow.DayOfWeek == DayOfWeek.Sunday
            ? DayOfWeek.Saturday
            : localNow.DayOfWeek - 1;
        var previous = hours.SingleOrDefault(item => item.DayOfWeek == previousDay);
        return previous is { IsClosed: false }
            && previous.CloseMinutes < previous.OpenMinutes
            && minute < previous.CloseMinutes;
    }

    private static bool MatchesToday(ShopOpeningHour? hours, int minute)
    {
        if (hours is null || hours.IsClosed || hours.OpenMinutes == hours.CloseMinutes)
        {
            return false;
        }

        return hours.CloseMinutes > hours.OpenMinutes
            ? minute >= hours.OpenMinutes && minute < hours.CloseMinutes
            : minute >= hours.OpenMinutes;
    }

    private static TimeZoneInfo GetShopTimeZone()
    {
        var id = OperatingSystem.IsWindows() ? "GMT Standard Time" : "Europe/London";
        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }
}

public sealed record ShopOperationsSnapshot(
    ShopSettings Settings,
    IReadOnlyList<ShopOpeningHour> OpeningHours,
    IReadOnlyList<DeliveryZone> DeliveryZones,
    DateTimeOffset LocalNow,
    bool IsAcceptingOrders,
    string StatusMessage)
{
    public bool CoversPostcode(string? postcode)
    {
        var normalized = ShopOperationsService.NormalizePostcode(postcode);
        return normalized.Length > 0 && DeliveryZones.Any(zone =>
            normalized.StartsWith(ShopOperationsService.NormalizePostcode(zone.PostcodePrefix), StringComparison.Ordinal));
    }

    public string EstimatedTime(OrderType orderType) => orderType == OrderType.Delivery
        ? $"{Settings.DeliveryEtaMinMinutes}–{Settings.DeliveryEtaMaxMinutes} mins"
        : $"{Settings.CollectionEtaMinMinutes}–{Settings.CollectionEtaMaxMinutes} mins";
}
