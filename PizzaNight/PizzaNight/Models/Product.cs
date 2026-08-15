namespace PizzaNight.Models;

public sealed class Product
{
    public int Id { get; set; }
    public int MenuCategoryId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public int BasePricePence { get; set; }
    public required string ImagePath { get; set; }
    public string? Badge { get; set; }
    public bool IsCustomisable { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int DisplayOrder { get; set; }

    public MenuCategory MenuCategory { get; set; } = null!;
    public ICollection<ProductOptionGroup> OptionGroups { get; set; } = new List<ProductOptionGroup>();
}
