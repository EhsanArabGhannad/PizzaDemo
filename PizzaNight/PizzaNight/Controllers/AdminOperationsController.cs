using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Data;
using PizzaNight.Models;
using PizzaNight.Services;

namespace PizzaNight.Controllers;

[Authorize(Roles = "Administrator")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route("admin/settings")]
public sealed class AdminOperationsController(PizzaNightDbContext dbContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await BuildViewModelAsync(cancellationToken));

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AdminOperationsViewModel model, CancellationToken cancellationToken)
    {
        ValidateHours(model.Hours);
        if (!ModelState.IsValid)
        {
            model.DeliveryZones = await GetZonesAsync(cancellationToken);
            return View(model);
        }

        var settings = await dbContext.ShopSettings.SingleAsync(cancellationToken);
        settings.AcceptingOnlineOrders = model.Settings.AcceptingOnlineOrders;
        settings.UseOpeningHours = model.Settings.UseOpeningHours;
        settings.TemporaryClosureMessage = NullIfWhiteSpace(model.Settings.TemporaryClosureMessage);
        settings.DeliveryMinimumPence = ToPence(model.Settings.DeliveryMinimum);
        settings.DeliveryFeePence = ToPence(model.Settings.DeliveryFee);
        settings.ServiceFeePence = ToPence(model.Settings.ServiceFee);
        settings.DeliveryEtaMinMinutes = model.Settings.DeliveryEtaMinMinutes;
        settings.DeliveryEtaMaxMinutes = model.Settings.DeliveryEtaMaxMinutes;
        settings.CollectionEtaMinMinutes = model.Settings.CollectionEtaMinMinutes;
        settings.CollectionEtaMaxMinutes = model.Settings.CollectionEtaMaxMinutes;

        var existingHours = await dbContext.ShopOpeningHours.ToDictionaryAsync(hours => hours.DayOfWeek, cancellationToken);
        foreach (var input in model.Hours)
        {
            if (!existingHours.TryGetValue(input.DayOfWeek, out var hours))
            {
                hours = new ShopOpeningHour { DayOfWeek = input.DayOfWeek };
                dbContext.ShopOpeningHours.Add(hours);
            }

            hours.IsClosed = input.IsClosed;
            hours.OpenMinutes = ParseMinutes(input.OpensAt);
            hours.CloseMinutes = ParseMinutes(input.ClosesAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Shop settings and opening hours were updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("delivery-zones/new")]
    public async Task<IActionResult> CreateZone(CancellationToken cancellationToken)
    {
        var nextOrder = (await dbContext.DeliveryZones.MaxAsync(zone => (int?)zone.DisplayOrder, cancellationToken) ?? 0) + 1;
        return View("ZoneForm", new DeliveryZoneFormViewModel { DisplayOrder = nextOrder });
    }

    [HttpPost("delivery-zones/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateZone(DeliveryZoneFormViewModel form, CancellationToken cancellationToken)
    {
        form.PostcodePrefix = ShopOperationsService.NormalizePostcode(form.PostcodePrefix);
        await ValidateZonePrefixAsync(form, null, cancellationToken);
        if (!ModelState.IsValid) return View("ZoneForm", form);

        dbContext.DeliveryZones.Add(new DeliveryZone
        {
            Name = form.Name.Trim(),
            PostcodePrefix = form.PostcodePrefix,
            IsActive = form.IsActive,
            DisplayOrder = form.DisplayOrder
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Delivery area {form.Name.Trim()} was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("delivery-zones/{id:int}/edit")]
    public async Task<IActionResult> EditZone(int id, CancellationToken cancellationToken)
    {
        var zone = await dbContext.DeliveryZones.AsNoTracking().SingleOrDefaultAsync(zone => zone.Id == id, cancellationToken);
        if (zone is null) return NotFound();
        return View("ZoneForm", new DeliveryZoneFormViewModel
        {
            Id = zone.Id,
            Name = zone.Name,
            PostcodePrefix = zone.PostcodePrefix,
            IsActive = zone.IsActive,
            DisplayOrder = zone.DisplayOrder
        });
    }

    [HttpPost("delivery-zones/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditZone(int id, DeliveryZoneFormViewModel form, CancellationToken cancellationToken)
    {
        var zone = await dbContext.DeliveryZones.SingleOrDefaultAsync(zone => zone.Id == id, cancellationToken);
        if (zone is null) return NotFound();

        form.Id = id;
        form.PostcodePrefix = ShopOperationsService.NormalizePostcode(form.PostcodePrefix);
        await ValidateZonePrefixAsync(form, id, cancellationToken);
        if (!ModelState.IsValid) return View("ZoneForm", form);

        zone.Name = form.Name.Trim();
        zone.PostcodePrefix = form.PostcodePrefix;
        zone.IsActive = form.IsActive;
        zone.DisplayOrder = form.DisplayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Delivery area {zone.Name} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delivery-zones/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleZone(int id, CancellationToken cancellationToken)
    {
        var zone = await dbContext.DeliveryZones.SingleOrDefaultAsync(zone => zone.Id == id, cancellationToken);
        if (zone is null) return NotFound();
        zone.IsActive = !zone.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"{zone.Name} is now {(zone.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delivery-zones/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteZone(int id, CancellationToken cancellationToken)
    {
        var zone = await dbContext.DeliveryZones.SingleOrDefaultAsync(zone => zone.Id == id, cancellationToken);
        if (zone is null) return NotFound();
        dbContext.DeliveryZones.Remove(zone);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Delivery area {zone.Name} was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminOperationsViewModel> BuildViewModelAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ShopSettings.AsNoTracking().SingleAsync(cancellationToken);
        var existingHours = await dbContext.ShopOpeningHours.AsNoTracking().ToDictionaryAsync(hours => hours.DayOfWeek, cancellationToken);
        var orderedDays = Enumerable.Range(0, 7).Select(offset => (DayOfWeek)(((int)DayOfWeek.Monday + offset) % 7));

        return new AdminOperationsViewModel
        {
            Settings = new OperationsSettingsFormViewModel
            {
                AcceptingOnlineOrders = settings.AcceptingOnlineOrders,
                UseOpeningHours = settings.UseOpeningHours,
                TemporaryClosureMessage = settings.TemporaryClosureMessage,
                DeliveryMinimum = settings.DeliveryMinimumPence / 100m,
                DeliveryFee = settings.DeliveryFeePence / 100m,
                ServiceFee = settings.ServiceFeePence / 100m,
                DeliveryEtaMinMinutes = settings.DeliveryEtaMinMinutes,
                DeliveryEtaMaxMinutes = settings.DeliveryEtaMaxMinutes,
                CollectionEtaMinMinutes = settings.CollectionEtaMinMinutes,
                CollectionEtaMaxMinutes = settings.CollectionEtaMaxMinutes
            },
            Hours = orderedDays.Select(day => ToHoursForm(existingHours.GetValueOrDefault(day), day)).ToList(),
            DeliveryZones = await GetZonesAsync(cancellationToken)
        };
    }

    private async Task ValidateZonePrefixAsync(DeliveryZoneFormViewModel form, int? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.PostcodePrefix)) return;
        if (await dbContext.DeliveryZones.AnyAsync(
            zone => zone.PostcodePrefix == form.PostcodePrefix && zone.Id != currentId,
            cancellationToken))
        {
            ModelState.AddModelError(nameof(form.PostcodePrefix), "That postcode prefix already exists.");
        }
    }

    private void ValidateHours(IReadOnlyCollection<OpeningHoursFormViewModel> hours)
    {
        if (hours.Count != 7 || hours.Select(item => item.DayOfWeek).Distinct().Count() != 7)
        {
            ModelState.AddModelError(nameof(AdminOperationsViewModel.Hours), "Opening hours must include all seven days.");
        }
    }

    private Task<List<DeliveryZone>> GetZonesAsync(CancellationToken cancellationToken) => dbContext.DeliveryZones
        .AsNoTracking()
        .OrderBy(zone => zone.DisplayOrder)
        .ThenBy(zone => zone.Name)
        .ToListAsync(cancellationToken);

    private static OpeningHoursFormViewModel ToHoursForm(ShopOpeningHour? hours, DayOfWeek day) => new()
    {
        DayOfWeek = day,
        IsClosed = hours?.IsClosed ?? true,
        OpensAt = FormatMinutes(hours?.OpenMinutes ?? 17 * 60),
        ClosesAt = FormatMinutes(hours?.CloseMinutes ?? 23 * 60)
    };

    private static int ParseMinutes(string value)
    {
        var time = TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);
        return time.Hour * 60 + time.Minute;
    }

    private static string FormatMinutes(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";
    private static int ToPence(decimal pounds) => checked((int)Math.Round(pounds * 100m, MidpointRounding.AwayFromZero));
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
