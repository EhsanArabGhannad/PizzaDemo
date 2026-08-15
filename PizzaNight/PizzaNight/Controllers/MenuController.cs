using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Data;

namespace PizzaNight.Controllers;

[ApiController]
[Route("api/menu")]
public sealed class MenuController(PizzaNightDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var categories = await dbContext.MenuCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new
            {
                id = category.Slug,
                label = category.Name
            })
            .ToListAsync(cancellationToken);

        var products = await dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Where(product => product.IsAvailable && product.MenuCategory.IsActive)
            .OrderBy(product => product.MenuCategory.DisplayOrder)
            .ThenBy(product => product.DisplayOrder)
            .Select(product => new
            {
                id = product.Slug,
                productId = product.Id,
                name = product.Name,
                description = product.Description,
                category = product.MenuCategory.Slug,
                price = product.BasePricePence / 100m,
                image = product.ImagePath,
                badge = product.Badge,
                customisable = product.IsCustomisable,
                optionGroups = product.OptionGroups
                    .OrderBy(group => group.DisplayOrder)
                    .Select(group => new
                    {
                        id = group.Slug,
                        name = group.Name,
                        required = group.IsRequired,
                        minimumSelections = group.MinimumSelections,
                        maximumSelections = group.MaximumSelections,
                        options = group.Options
                            .Where(option => option.IsAvailable)
                            .OrderBy(option => option.DisplayOrder)
                            .Select(option => new
                            {
                                id = option.Slug,
                                name = option.Name,
                                description = option.Description,
                                price = option.PriceAdjustmentPence / 100m
                            })
                    })
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            categories = new[] { new { id = "all", label = "Popular" } }.Concat(categories),
            products
        });
    }
}
