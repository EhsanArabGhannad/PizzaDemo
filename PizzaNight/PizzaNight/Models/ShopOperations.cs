namespace PizzaNight.Models;

public sealed class ShopSettings
{
    public int Id { get; set; }
    public bool AcceptingOnlineOrders { get; set; } = true;
    public bool UseOpeningHours { get; set; }
    public string? TemporaryClosureMessage { get; set; }
    public int DeliveryMinimumPence { get; set; } = 1000;
    public int DeliveryFeePence { get; set; } = 250;
    public int ServiceFeePence { get; set; } = 50;
    public int DeliveryEtaMinMinutes { get; set; } = 35;
    public int DeliveryEtaMaxMinutes { get; set; } = 50;
    public int CollectionEtaMinMinutes { get; set; } = 20;
    public int CollectionEtaMaxMinutes { get; set; } = 30;
}

public sealed class ShopOpeningHour
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public int OpenMinutes { get; set; } = 17 * 60;
    public int CloseMinutes { get; set; } = 23 * 60;
}

public sealed class DeliveryZone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PostcodePrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
