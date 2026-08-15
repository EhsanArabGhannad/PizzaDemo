using Microsoft.AspNetCore.Mvc;
using PizzaNight.Services;

namespace PizzaNight.Controllers;

[ApiController]
[Route("api/shop")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ShopController(ShopOperationsService shopOperationsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var snapshot = await shopOperationsService.GetSnapshotAsync(cancellationToken);
        var settings = snapshot.Settings;

        return Ok(new
        {
            acceptingOnlineOrders = snapshot.IsAcceptingOrders,
            statusMessage = snapshot.StatusMessage,
            currentDay = snapshot.LocalNow.DayOfWeek.ToString(),
            useOpeningHours = settings.UseOpeningHours,
            deliveryMinimum = ToPounds(settings.DeliveryMinimumPence),
            deliveryFee = ToPounds(settings.DeliveryFeePence),
            serviceFee = ToPounds(settings.ServiceFeePence),
            deliveryEta = snapshot.EstimatedTime(Models.OrderType.Delivery),
            collectionEta = snapshot.EstimatedTime(Models.OrderType.Collection),
            deliveryZones = snapshot.DeliveryZones.Select(zone => new
            {
                zone.Name,
                prefix = ShopOperationsService.NormalizePostcode(zone.PostcodePrefix)
            }),
            openingHours = snapshot.OpeningHours
                .OrderBy(hours => ((int)hours.DayOfWeek + 6) % 7)
                .Select(hours => new
                {
                    day = hours.DayOfWeek.ToString(),
                    hours.IsClosed,
                    opensAt = FormatMinutes(hours.OpenMinutes),
                    closesAt = FormatMinutes(hours.CloseMinutes)
                })
        });
    }

    private static decimal ToPounds(int pence) => pence / 100m;
    private static string FormatMinutes(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";
}
