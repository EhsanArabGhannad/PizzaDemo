using System.ComponentModel.DataAnnotations;

namespace PizzaNight.Models;

public sealed record AdminMenuIndexViewModel(IReadOnlyList<MenuCategory> Categories);

public sealed class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers and hyphens only.")]
    public string? Slug { get; set; }

    [Range(0, 10000)]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class ProductFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int MenuCategoryId { get; set; }

    [Required, StringLength(140, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers and hyphens only.")]
    public string? Slug { get; set; }

    [Required, StringLength(600, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999.99")]
    public decimal BasePrice { get; set; }

    [Required, StringLength(260)]
    public string ImagePath { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Badge { get; set; }

    public bool IsCustomisable { get; set; }
    public bool IsAvailable { get; set; } = true;

    [Range(0, 10000)]
    public int DisplayOrder { get; set; }
}

public sealed class ProductEditorViewModel
{
    public ProductEditorViewModel()
    {
    }

    public ProductEditorViewModel(
        ProductFormViewModel product,
        IReadOnlyList<MenuCategory> categories,
        IReadOnlyList<string> availableImages)
    {
        Product = product;
        Categories = categories;
        AvailableImages = availableImages;
    }

    public ProductFormViewModel Product { get; set; } = new();
    public IReadOnlyList<MenuCategory> Categories { get; set; } = [];
    public IReadOnlyList<string> AvailableImages { get; set; } = [];
}

public sealed record ProductOptionsViewModel(Product Product);

public sealed class OptionGroupFormViewModel : IValidatableObject
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers and hyphens only.")]
    public string? Slug { get; set; }

    public bool IsRequired { get; set; }

    [Range(0, 20)]
    public int MinimumSelections { get; set; }

    [Range(1, 20)]
    public int MaximumSelections { get; set; } = 1;

    [Range(0, 10000)]
    public int DisplayOrder { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinimumSelections > MaximumSelections)
        {
            yield return new ValidationResult(
                "Minimum selections cannot be greater than maximum selections.",
                [nameof(MinimumSelections), nameof(MaximumSelections)]);
        }

        if (IsRequired && MinimumSelections < 1)
        {
            yield return new ValidationResult(
                "A required group must require at least one selection.",
                [nameof(MinimumSelections)]);
        }
    }
}

public sealed class ProductOptionFormViewModel
{
    public int Id { get; set; }
    public int ProductOptionGroupId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers and hyphens only.")]
    public string? Slug { get; set; }

    [StringLength(240)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "999.99")]
    public decimal PriceAdjustment { get; set; }

    public bool IsAvailable { get; set; } = true;

    [Range(0, 10000)]
    public int DisplayOrder { get; set; }
}

public sealed class OptionEditorViewModel
{
    public OptionEditorViewModel()
    {
    }

    public OptionEditorViewModel(
        ProductOptionFormViewModel option,
        ProductOptionGroup group,
        Product product)
    {
        Option = option;
        Group = group;
        Product = product;
    }

    public ProductOptionFormViewModel Option { get; set; } = new();
    public ProductOptionGroup Group { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public static class MenuImageCatalog
{
    public static readonly IReadOnlyList<string> Paths =
    [
        "/assets/images/menu-supreme-pizza.webp",
        "/assets/images/pizza-hero.webp",
        "/assets/images/menu-double-burger.webp",
        "/assets/images/menu-kebab-wrap.webp",
        "/assets/images/menu-loaded-fries.webp"
    ];
}
