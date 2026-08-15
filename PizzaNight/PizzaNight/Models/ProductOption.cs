namespace PizzaNight.Models;

public sealed class ProductOption
{
    public int Id { get; set; }
    public int ProductOptionGroupId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public int PriceAdjustmentPence { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ProductOptionGroup ProductOptionGroup { get; set; } = null!;
}
