using Microsoft.EntityFrameworkCore;
using PizzaNight.Models;

namespace PizzaNight.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(PizzaNightDbContext dbContext)
    {
        if (!await dbContext.MenuCategories.AnyAsync())
        {
            SeedMenu(dbContext);
        }

        if (!await dbContext.ShopSettings.AnyAsync())
        {
            dbContext.ShopSettings.Add(new ShopSettings());
        }

        if (!await dbContext.ShopOpeningHours.AnyAsync())
        {
            dbContext.ShopOpeningHours.AddRange(
                CreateHours(DayOfWeek.Sunday, false, 17, 0, 23, 0),
                CreateHours(DayOfWeek.Monday, false, 17, 0, 23, 0),
                CreateHours(DayOfWeek.Tuesday, true, 17, 0, 23, 0),
                CreateHours(DayOfWeek.Wednesday, false, 17, 0, 23, 0),
                CreateHours(DayOfWeek.Thursday, false, 17, 0, 23, 0),
                CreateHours(DayOfWeek.Friday, false, 17, 0, 23, 30),
                CreateHours(DayOfWeek.Saturday, false, 17, 0, 23, 30));
        }

        if (!await dbContext.DeliveryZones.AnyAsync())
        {
            dbContext.DeliveryZones.Add(new DeliveryZone
            {
                Name = "Consett and nearby DH8 addresses",
                PostcodePrefix = "DH8",
                DisplayOrder = 1
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static void SeedMenu(PizzaNightDbContext dbContext)
    {

        var pizzas = new MenuCategory { Name = "Pizzas", Slug = "pizza", DisplayOrder = 1 };
        var burgers = new MenuCategory { Name = "Burgers", Slug = "burger", DisplayOrder = 2 };
        var wraps = new MenuCategory { Name = "Wraps", Slug = "wrap", DisplayOrder = 3 };
        var sides = new MenuCategory { Name = "Sides", Slug = "side", DisplayOrder = 4 };
        var deals = new MenuCategory { Name = "Meal deals", Slug = "deal", DisplayOrder = 5 };

        pizzas.Products.Add(CreatePizza(
            "Knight’s Special",
            "knights-special",
            "Pepperoni, chicken, peppers, red onion, mushrooms and mozzarella.",
            1099,
            "/assets/images/menu-supreme-pizza.webp",
            "Bestseller",
            1));

        pizzas.Products.Add(CreatePizza(
            "Pepperoni Feast",
            "pepperoni-feast",
            "Double pepperoni layered with bubbling mozzarella and house tomato sauce.",
            999,
            "/assets/images/pizza-hero.webp",
            null,
            2));

        pizzas.Products.Add(CreatePizza(
            "Classic Margherita",
            "margherita",
            "House tomato sauce, mozzarella and a touch of oregano.",
            799,
            "/assets/images/menu-supreme-pizza.webp",
            null,
            3));

        burgers.Products.Add(CreateProduct(
            "Double Smash Burger",
            "double-smash",
            "Two beef patties, double cheddar, fresh salad and signature burger sauce.",
            749,
            "/assets/images/menu-double-burger.webp",
            "New",
            1));

        wraps.Products.Add(CreateProduct(
            "Chicken & Doner Wrap",
            "chicken-doner-wrap",
            "Grilled chicken, doner meat, crisp salad and garlic sauce in a toasted wrap.",
            795,
            "/assets/images/menu-kebab-wrap.webp",
            null,
            1));

        sides.Products.Add(CreateProduct(
            "Knight’s Loaded Fries",
            "loaded-fries",
            "Seasoned fries, chicken, melted cheese, jalapeños and burger sauce.",
            649,
            "/assets/images/menu-loaded-fries.webp",
            "Popular",
            1));

        deals.Products.Add(CreateProduct(
            "Family Feast",
            "family-feast",
            "Two large pizzas, two sides and a 1.5L drink — built for sharing.",
            2799,
            "/assets/images/pizza-hero.webp",
            "Save £8",
            1));

        deals.Products.Add(CreateProduct(
            "Burger Box Deal",
            "burger-box",
            "Double smash burger, seasoned fries and a chilled can.",
            1099,
            "/assets/images/menu-double-burger.webp",
            null,
            2));

        dbContext.MenuCategories.AddRange(pizzas, burgers, wraps, sides, deals);
    }

    private static ShopOpeningHour CreateHours(
        DayOfWeek day,
        bool isClosed,
        int openHour,
        int openMinute,
        int closeHour,
        int closeMinute) => new()
        {
            DayOfWeek = day,
            IsClosed = isClosed,
            OpenMinutes = openHour * 60 + openMinute,
            CloseMinutes = closeHour * 60 + closeMinute
        };

    private static Product CreatePizza(
        string name,
        string slug,
        string description,
        int pricePence,
        string imagePath,
        string? badge,
        int displayOrder)
    {
        var product = CreateProduct(name, slug, description, pricePence, imagePath, badge, displayOrder);
        product.IsCustomisable = true;

        product.OptionGroups.Add(new ProductOptionGroup
        {
            Name = "Choose your size",
            Slug = "size",
            IsRequired = true,
            MinimumSelections = 1,
            MaximumSelections = 1,
            DisplayOrder = 1,
            Options =
            {
                new ProductOption { Name = "10 inch", Slug = "small", Description = "Small", DisplayOrder = 1 },
                new ProductOption { Name = "12 inch", Slug = "medium", Description = "Medium", PriceAdjustmentPence = 200, DisplayOrder = 2 },
                new ProductOption { Name = "14 inch", Slug = "large", Description = "Large", PriceAdjustmentPence = 400, DisplayOrder = 3 }
            }
        });

        product.OptionGroups.Add(new ProductOptionGroup
        {
            Name = "Choose your crust",
            Slug = "crust",
            IsRequired = true,
            MinimumSelections = 1,
            MaximumSelections = 1,
            DisplayOrder = 2,
            Options =
            {
                new ProductOption { Name = "Classic", Slug = "classic", Description = "Soft & golden", DisplayOrder = 1 },
                new ProductOption { Name = "Thin", Slug = "thin", Description = "Light & crispy", DisplayOrder = 2 },
                new ProductOption { Name = "Stuffed", Slug = "stuffed", Description = "Cheese-filled edge", PriceAdjustmentPence = 250, DisplayOrder = 3 }
            }
        });

        product.OptionGroups.Add(new ProductOptionGroup
        {
            Name = "Add extras",
            Slug = "extras",
            MinimumSelections = 0,
            MaximumSelections = 4,
            DisplayOrder = 3,
            Options =
            {
                new ProductOption { Name = "Extra cheese", Slug = "extra-cheese", Description = "More mozzarella", PriceAdjustmentPence = 120, DisplayOrder = 1 },
                new ProductOption { Name = "Pepperoni", Slug = "pepperoni", Description = "Extra portion", PriceAdjustmentPence = 150, DisplayOrder = 2 },
                new ProductOption { Name = "Mushrooms", Slug = "mushrooms", Description = "Freshly sliced", PriceAdjustmentPence = 80, DisplayOrder = 3 },
                new ProductOption { Name = "Jalapeños", Slug = "jalapenos", Description = "Add some heat", PriceAdjustmentPence = 70, DisplayOrder = 4 }
            }
        });

        return product;
    }

    private static Product CreateProduct(
        string name,
        string slug,
        string description,
        int pricePence,
        string imagePath,
        string? badge,
        int displayOrder) => new()
        {
            Name = name,
            Slug = slug,
            Description = description,
            BasePricePence = pricePence,
            ImagePath = imagePath,
            Badge = badge,
            DisplayOrder = displayOrder
        };
}
