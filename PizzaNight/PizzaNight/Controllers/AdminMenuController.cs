using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Data;
using PizzaNight.Models;
using PizzaNight.Services;

namespace PizzaNight.Controllers;

[Authorize(Roles = "Administrator")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route("admin/menu")]
public sealed class AdminMenuController(PizzaNightDbContext dbContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await dbContext.MenuCategories
            .AsNoTracking()
            .AsSplitQuery()
            .Include(category => category.Products.OrderBy(product => product.DisplayOrder))
                .ThenInclude(product => product.OptionGroups)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return View(new AdminMenuIndexViewModel(categories));
    }

    [HttpGet("categories/new")]
    public async Task<IActionResult> CreateCategory(CancellationToken cancellationToken)
    {
        var nextOrder = (await dbContext.MenuCategories.MaxAsync(category => (int?)category.DisplayOrder, cancellationToken) ?? 0) + 1;
        return View("CategoryForm", new CategoryFormViewModel { DisplayOrder = nextOrder });
    }

    [HttpPost("categories/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryFormViewModel form, CancellationToken cancellationToken)
    {
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateCategorySlugAsync(form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("CategoryForm", form);
        }

        dbContext.MenuCategories.Add(new MenuCategory
        {
            Name = form.Name.Trim(),
            Slug = form.Slug,
            DisplayOrder = form.DisplayOrder,
            IsActive = form.IsActive
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Category {form.Name.Trim()} was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("categories/{id:int}/edit")]
    public async Task<IActionResult> EditCategory(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.MenuCategories.AsNoTracking().SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null) return NotFound();

        return View("CategoryForm", new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        });
    }

    [HttpPost("categories/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, CategoryFormViewModel form, CancellationToken cancellationToken)
    {
        var category = await dbContext.MenuCategories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null) return NotFound();

        form.Id = id;
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateCategorySlugAsync(form, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("CategoryForm", form);
        }

        category.Name = form.Name.Trim();
        category.Slug = form.Slug;
        category.DisplayOrder = form.DisplayOrder;
        category.IsActive = form.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Category {category.Name} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("categories/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategory(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.MenuCategories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null) return NotFound();
        category.IsActive = !category.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"{category.Name} is now {(category.IsActive ? "visible" : "hidden")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("products/new")]
    public async Task<IActionResult> CreateProduct(int? categoryId, CancellationToken cancellationToken)
    {
        var categories = await GetCategoriesAsync(cancellationToken);
        if (categories.Count == 0)
        {
            TempData["Error"] = "Create a category before adding a product.";
            return RedirectToAction(nameof(Index));
        }

        var selectedCategoryId = categories.Any(category => category.Id == categoryId) ? categoryId!.Value : categories[0].Id;
        var nextOrder = (await dbContext.Products
            .Where(product => product.MenuCategoryId == selectedCategoryId)
            .MaxAsync(product => (int?)product.DisplayOrder, cancellationToken) ?? 0) + 1;

        return View("ProductForm", new ProductEditorViewModel(
            new ProductFormViewModel
            {
                MenuCategoryId = selectedCategoryId,
                DisplayOrder = nextOrder,
                ImagePath = MenuImageCatalog.Paths[0]
            },
            categories,
            MenuImageCatalog.Paths));
    }

    [HttpPost("products/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(
        [Bind(Prefix = "Product")] ProductFormViewModel form,
        CancellationToken cancellationToken)
    {
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateProductAsync(form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("ProductForm", await RebuildProductEditorAsync(form, cancellationToken));
        }

        var product = new Product
        {
            MenuCategoryId = form.MenuCategoryId,
            Name = form.Name.Trim(),
            Slug = form.Slug,
            Description = form.Description.Trim(),
            BasePricePence = ToPence(form.BasePrice),
            ImagePath = form.ImagePath,
            Badge = NullIfWhiteSpace(form.Badge),
            IsCustomisable = form.IsCustomisable,
            IsAvailable = form.IsAvailable,
            DisplayOrder = form.DisplayOrder
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Product {product.Name} was created.";
        return product.IsCustomisable
            ? RedirectToAction(nameof(Options), new { id = product.Id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet("products/{id:int}/edit")]
    public async Task<IActionResult> EditProduct(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking().SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null) return NotFound();

        return View("ProductForm", await RebuildProductEditorAsync(new ProductFormViewModel
        {
            Id = product.Id,
            MenuCategoryId = product.MenuCategoryId,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePricePence / 100m,
            ImagePath = product.ImagePath,
            Badge = product.Badge,
            IsCustomisable = product.IsCustomisable,
            IsAvailable = product.IsAvailable,
            DisplayOrder = product.DisplayOrder
        }, cancellationToken));
    }

    [HttpPost("products/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(
        int id,
        [Bind(Prefix = "Product")] ProductFormViewModel form,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.Include(product => product.OptionGroups).SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null) return NotFound();

        form.Id = id;
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateProductAsync(form, id, cancellationToken);
        if (!form.IsCustomisable && product.OptionGroups.Count > 0)
        {
            ModelState.AddModelError("Product.IsCustomisable", "Remove all option groups before disabling customisation.");
        }
        if (!ModelState.IsValid)
        {
            return View("ProductForm", await RebuildProductEditorAsync(form, cancellationToken));
        }

        product.MenuCategoryId = form.MenuCategoryId;
        product.Name = form.Name.Trim();
        product.Slug = form.Slug;
        product.Description = form.Description.Trim();
        product.BasePricePence = ToPence(form.BasePrice);
        product.ImagePath = form.ImagePath;
        product.Badge = NullIfWhiteSpace(form.Badge);
        product.IsCustomisable = form.IsCustomisable;
        product.IsAvailable = form.IsAvailable;
        product.DisplayOrder = form.DisplayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Product {product.Name} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("products/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProduct(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null) return NotFound();
        product.IsAvailable = !product.IsAvailable;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"{product.Name} is now {(product.IsAvailable ? "available" : "sold out")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("products/{id:int}/options")]
    public async Task<IActionResult> Options(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.OptionGroups.OrderBy(group => group.DisplayOrder))
                .ThenInclude(group => group.Options.OrderBy(option => option.DisplayOrder))
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null) return NotFound();
        return View(new ProductOptionsViewModel(product));
    }

    [HttpGet("products/{productId:int}/groups/new")]
    public async Task<IActionResult> CreateGroup(int productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking().SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);
        if (product is null) return NotFound();
        ViewBag.Product = product;
        var nextOrder = (await dbContext.ProductOptionGroups.Where(group => group.ProductId == productId).MaxAsync(group => (int?)group.DisplayOrder, cancellationToken) ?? 0) + 1;
        return View("GroupForm", new OptionGroupFormViewModel { ProductId = productId, DisplayOrder = nextOrder, MaximumSelections = 1 });
    }

    [HttpPost("products/{productId:int}/groups/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(int productId, OptionGroupFormViewModel form, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);
        if (product is null) return NotFound();
        form.ProductId = productId;
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateGroupSlugAsync(form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            ViewBag.Product = product;
            return View("GroupForm", form);
        }

        dbContext.ProductOptionGroups.Add(new ProductOptionGroup
        {
            ProductId = productId,
            Name = form.Name.Trim(),
            Slug = form.Slug,
            IsRequired = form.IsRequired,
            MinimumSelections = form.MinimumSelections,
            MaximumSelections = form.MaximumSelections,
            DisplayOrder = form.DisplayOrder
        });
        product.IsCustomisable = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Option group {form.Name.Trim()} was created.";
        return RedirectToAction(nameof(Options), new { id = productId });
    }

    [HttpGet("groups/{id:int}/edit")]
    public async Task<IActionResult> EditGroup(int id, CancellationToken cancellationToken)
    {
        var group = await dbContext.ProductOptionGroups.Include(group => group.Product).AsNoTracking().SingleOrDefaultAsync(group => group.Id == id, cancellationToken);
        if (group is null) return NotFound();
        ViewBag.Product = group.Product;
        return View("GroupForm", new OptionGroupFormViewModel
        {
            Id = group.Id,
            ProductId = group.ProductId,
            Name = group.Name,
            Slug = group.Slug,
            IsRequired = group.IsRequired,
            MinimumSelections = group.MinimumSelections,
            MaximumSelections = group.MaximumSelections,
            DisplayOrder = group.DisplayOrder
        });
    }

    [HttpPost("groups/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(int id, OptionGroupFormViewModel form, CancellationToken cancellationToken)
    {
        var group = await dbContext.ProductOptionGroups.Include(group => group.Product).SingleOrDefaultAsync(group => group.Id == id, cancellationToken);
        if (group is null) return NotFound();
        form.Id = id;
        form.ProductId = group.ProductId;
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateGroupSlugAsync(form, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            ViewBag.Product = group.Product;
            return View("GroupForm", form);
        }

        group.Name = form.Name.Trim();
        group.Slug = form.Slug;
        group.IsRequired = form.IsRequired;
        group.MinimumSelections = form.MinimumSelections;
        group.MaximumSelections = form.MaximumSelections;
        group.DisplayOrder = form.DisplayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Option group {group.Name} was updated.";
        return RedirectToAction(nameof(Options), new { id = group.ProductId });
    }

    [HttpPost("groups/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroup(int id, CancellationToken cancellationToken)
    {
        var group = await dbContext.ProductOptionGroups.Include(group => group.Product).SingleOrDefaultAsync(group => group.Id == id, cancellationToken);
        if (group is null) return NotFound();
        var productId = group.ProductId;
        dbContext.ProductOptionGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (!await dbContext.ProductOptionGroups.AnyAsync(candidate => candidate.ProductId == productId, cancellationToken))
        {
            group.Product.IsCustomisable = false;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        TempData["Success"] = $"Option group {group.Name} was removed.";
        return RedirectToAction(nameof(Options), new { id = productId });
    }

    [HttpGet("groups/{groupId:int}/options/new")]
    public async Task<IActionResult> CreateOption(int groupId, CancellationToken cancellationToken)
    {
        var group = await GetGroupWithProductAsync(groupId, cancellationToken);
        if (group is null) return NotFound();
        var nextOrder = (await dbContext.ProductOptions.Where(option => option.ProductOptionGroupId == groupId).MaxAsync(option => (int?)option.DisplayOrder, cancellationToken) ?? 0) + 1;
        return View("OptionForm", new OptionEditorViewModel(
            new ProductOptionFormViewModel { ProductOptionGroupId = groupId, DisplayOrder = nextOrder },
            group,
            group.Product));
    }

    [HttpPost("groups/{groupId:int}/options/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOption(
        int groupId,
        [Bind(Prefix = "Option")] ProductOptionFormViewModel form,
        CancellationToken cancellationToken)
    {
        var group = await GetGroupWithProductAsync(groupId, cancellationToken);
        if (group is null) return NotFound();
        form.ProductOptionGroupId = groupId;
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateOptionSlugAsync(form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("OptionForm", new OptionEditorViewModel(form, group, group.Product));
        }

        dbContext.ProductOptions.Add(new ProductOption
        {
            ProductOptionGroupId = groupId,
            Name = form.Name.Trim(),
            Slug = form.Slug,
            Description = NullIfWhiteSpace(form.Description),
            PriceAdjustmentPence = ToPence(form.PriceAdjustment),
            IsAvailable = form.IsAvailable,
            DisplayOrder = form.DisplayOrder
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Option {form.Name.Trim()} was created.";
        return RedirectToAction(nameof(Options), new { id = group.ProductId });
    }

    [HttpGet("options/{id:int}/edit")]
    public async Task<IActionResult> EditOption(int id, CancellationToken cancellationToken)
    {
        var option = await dbContext.ProductOptions
            .AsNoTracking()
            .Include(option => option.ProductOptionGroup)
                .ThenInclude(group => group.Product)
            .SingleOrDefaultAsync(option => option.Id == id, cancellationToken);
        if (option is null) return NotFound();
        return View("OptionForm", new OptionEditorViewModel(
            new ProductOptionFormViewModel
            {
                Id = option.Id,
                ProductOptionGroupId = option.ProductOptionGroupId,
                Name = option.Name,
                Slug = option.Slug,
                Description = option.Description,
                PriceAdjustment = option.PriceAdjustmentPence / 100m,
                IsAvailable = option.IsAvailable,
                DisplayOrder = option.DisplayOrder
            },
            option.ProductOptionGroup,
            option.ProductOptionGroup.Product));
    }

    [HttpPost("options/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOption(
        int id,
        [Bind(Prefix = "Option")] ProductOptionFormViewModel form,
        CancellationToken cancellationToken)
    {
        var option = await dbContext.ProductOptions.Include(option => option.ProductOptionGroup).ThenInclude(group => group.Product).SingleOrDefaultAsync(option => option.Id == id, cancellationToken);
        if (option is null) return NotFound();
        form.Id = id;
        form.ProductOptionGroupId = option.ProductOptionGroupId;
        form.Slug = SlugGenerator.Generate(form.Slug, form.Name);
        await ValidateOptionSlugAsync(form, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("OptionForm", new OptionEditorViewModel(form, option.ProductOptionGroup, option.ProductOptionGroup.Product));
        }

        option.Name = form.Name.Trim();
        option.Slug = form.Slug;
        option.Description = NullIfWhiteSpace(form.Description);
        option.PriceAdjustmentPence = ToPence(form.PriceAdjustment);
        option.IsAvailable = form.IsAvailable;
        option.DisplayOrder = form.DisplayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Option {option.Name} was updated.";
        return RedirectToAction(nameof(Options), new { id = option.ProductOptionGroup.ProductId });
    }

    [HttpPost("options/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleOption(int id, CancellationToken cancellationToken)
    {
        var option = await dbContext.ProductOptions.Include(option => option.ProductOptionGroup).SingleOrDefaultAsync(option => option.Id == id, cancellationToken);
        if (option is null) return NotFound();
        option.IsAvailable = !option.IsAvailable;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"{option.Name} is now {(option.IsAvailable ? "available" : "unavailable")}.";
        return RedirectToAction(nameof(Options), new { id = option.ProductOptionGroup.ProductId });
    }

    [HttpPost("options/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOption(int id, CancellationToken cancellationToken)
    {
        var option = await dbContext.ProductOptions.Include(option => option.ProductOptionGroup).SingleOrDefaultAsync(option => option.Id == id, cancellationToken);
        if (option is null) return NotFound();
        var productId = option.ProductOptionGroup.ProductId;
        dbContext.ProductOptions.Remove(option);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Success"] = $"Option {option.Name} was removed.";
        return RedirectToAction(nameof(Options), new { id = productId });
    }

    private async Task ValidateCategorySlugAsync(CategoryFormViewModel form, int? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Slug)) ModelState.AddModelError(nameof(form.Slug), "A valid slug could not be generated.");
        else if (await dbContext.MenuCategories.AnyAsync(category => category.Slug == form.Slug && category.Id != currentId, cancellationToken))
            ModelState.AddModelError(nameof(form.Slug), "This slug is already in use.");
    }

    private async Task ValidateProductAsync(ProductFormViewModel form, int? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Slug)) ModelState.AddModelError("Product.Slug", "A valid slug could not be generated.");
        else if (await dbContext.Products.AnyAsync(product => product.Slug == form.Slug && product.Id != currentId, cancellationToken))
            ModelState.AddModelError("Product.Slug", "This slug is already in use.");
        if (!await dbContext.MenuCategories.AnyAsync(category => category.Id == form.MenuCategoryId, cancellationToken))
            ModelState.AddModelError("Product.MenuCategoryId", "Choose a valid category.");
        if (!MenuImageCatalog.Paths.Contains(form.ImagePath, StringComparer.Ordinal))
            ModelState.AddModelError("Product.ImagePath", "Choose an image from the available library.");
    }

    private async Task ValidateGroupSlugAsync(OptionGroupFormViewModel form, int? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Slug)) ModelState.AddModelError(nameof(form.Slug), "A valid slug could not be generated.");
        else if (await dbContext.ProductOptionGroups.AnyAsync(group => group.ProductId == form.ProductId && group.Slug == form.Slug && group.Id != currentId, cancellationToken))
            ModelState.AddModelError(nameof(form.Slug), "This slug is already used by another group on this product.");
    }

    private async Task ValidateOptionSlugAsync(ProductOptionFormViewModel form, int? currentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Slug)) ModelState.AddModelError("Option.Slug", "A valid slug could not be generated.");
        else if (await dbContext.ProductOptions.AnyAsync(option => option.ProductOptionGroupId == form.ProductOptionGroupId && option.Slug == form.Slug && option.Id != currentId, cancellationToken))
            ModelState.AddModelError("Option.Slug", "This slug is already used by another option in this group.");
    }

    private async Task<IReadOnlyList<MenuCategory>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.MenuCategories.AsNoTracking().OrderBy(category => category.DisplayOrder).ThenBy(category => category.Name).ToListAsync(cancellationToken);

    private async Task<ProductEditorViewModel> RebuildProductEditorAsync(ProductFormViewModel form, CancellationToken cancellationToken) =>
        new(form, await GetCategoriesAsync(cancellationToken), MenuImageCatalog.Paths);

    private async Task<ProductOptionGroup?> GetGroupWithProductAsync(int groupId, CancellationToken cancellationToken) =>
        await dbContext.ProductOptionGroups.AsNoTracking().Include(group => group.Product).SingleOrDefaultAsync(group => group.Id == groupId, cancellationToken);

    private static int ToPence(decimal pounds) => decimal.ToInt32(decimal.Round(pounds * 100m, 0, MidpointRounding.AwayFromZero));
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
