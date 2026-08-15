using System.ComponentModel.DataAnnotations;

namespace PizzaNight.Models;

public sealed class AdminOperationsViewModel
{
    public OperationsSettingsFormViewModel Settings { get; set; } = new();
    public List<OpeningHoursFormViewModel> Hours { get; set; } = [];
    public IReadOnlyList<DeliveryZone> DeliveryZones { get; set; } = [];
}

public sealed class OperationsSettingsFormViewModel : IValidatableObject
{
    public bool AcceptingOnlineOrders { get; set; } = true;
    public bool UseOpeningHours { get; set; }

    [StringLength(240)]
    public string? TemporaryClosureMessage { get; set; }

    [Range(typeof(decimal), "0", "999.99")]
    public decimal DeliveryMinimum { get; set; }

    [Range(typeof(decimal), "0", "99.99")]
    public decimal DeliveryFee { get; set; }

    [Range(typeof(decimal), "0", "99.99")]
    public decimal ServiceFee { get; set; }

    [Range(5, 240)]
    public int DeliveryEtaMinMinutes { get; set; }

    [Range(5, 240)]
    public int DeliveryEtaMaxMinutes { get; set; }

    [Range(5, 240)]
    public int CollectionEtaMinMinutes { get; set; }

    [Range(5, 240)]
    public int CollectionEtaMaxMinutes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeliveryEtaMinMinutes > DeliveryEtaMaxMinutes)
        {
            yield return new ValidationResult(
                "Delivery minimum time cannot be greater than its maximum time.",
                [nameof(DeliveryEtaMinMinutes), nameof(DeliveryEtaMaxMinutes)]);
        }

        if (CollectionEtaMinMinutes > CollectionEtaMaxMinutes)
        {
            yield return new ValidationResult(
                "Collection minimum time cannot be greater than its maximum time.",
                [nameof(CollectionEtaMinMinutes), nameof(CollectionEtaMaxMinutes)]);
        }
    }
}

public sealed class OpeningHoursFormViewModel : IValidatableObject
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }

    [Required]
    [RegularExpression("^(?:[01]\\d|2[0-3]):[0-5]\\d$", ErrorMessage = "Enter a valid opening time.")]
    public string OpensAt { get; set; } = "17:00";

    [Required]
    [RegularExpression("^(?:[01]\\d|2[0-3]):[0-5]\\d$", ErrorMessage = "Enter a valid closing time.")]
    public string ClosesAt { get; set; } = "23:00";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsClosed && OpensAt == ClosesAt)
        {
            yield return new ValidationResult(
                "Opening and closing times must be different.",
                [nameof(OpensAt), nameof(ClosesAt)]);
        }
    }
}

public sealed class DeliveryZoneFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(12, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9 ]+$", ErrorMessage = "Use letters, numbers and spaces only.")]
    public string PostcodePrefix { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(0, 10000)]
    public int DisplayOrder { get; set; }
}
