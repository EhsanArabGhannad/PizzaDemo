namespace PizzaNight.Models;

public sealed class ProductOptionGroup
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsRequired { get; set; }
    public int MinimumSelections { get; set; }
    public int MaximumSelections { get; set; } = 1;
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();
}
