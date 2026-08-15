using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PizzaNight.Contracts;
using PizzaNight.Data;
using PizzaNight.Models;

namespace PizzaNight.Services;

public sealed class OrderSubmissionService(PizzaNightDbContext dbContext)
{
    private const int DeliveryFeePence = 250;
    private const int ServiceFeePence = 50;
    private const int MaximumOrderLines = 50;
    private const int MaximumOrderQuantity = 50;

    public async Task<OrderSubmissionResult> SubmitAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRequest(request, out var orderType);
        if (errors.Count > 0)
        {
            return OrderSubmissionResult.Invalid(errors);
        }

        var requestedProductSlugs = request.Items
            .Select(item => item.ProductId.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var products = await dbContext.Products
            .AsSplitQuery()
            .Where(product => requestedProductSlugs.Contains(product.Slug))
            .Include(product => product.MenuCategory)
            .Include(product => product.OptionGroups)
                .ThenInclude(group => group.Options)
            .ToListAsync(cancellationToken);

        var productsBySlug = products.ToDictionary(product => product.Slug, StringComparer.OrdinalIgnoreCase);
        var orderItems = new List<OrderItem>();
        var subtotalPence = 0;

        for (var index = 0; index < request.Items.Count; index++)
        {
            var requestedItem = request.Items[index];
            var itemPath = $"items[{index}]";

            if (!productsBySlug.TryGetValue(requestedItem.ProductId.Trim(), out var product)
                || !product.IsAvailable
                || !product.MenuCategory.IsActive)
            {
                errors.Add($"{itemPath}: This product is no longer available.");
                continue;
            }

            var selectedOptions = ValidateAndResolveOptions(product, requestedItem, itemPath, errors);
            if (selectedOptions is null)
            {
                continue;
            }

            var unitPricePence = checked(product.BasePricePence + selectedOptions.Sum(selection => selection.Option.PriceAdjustmentPence));
            var lineTotalPence = checked(unitPricePence * requestedItem.Quantity);
            subtotalPence = checked(subtotalPence + lineTotalPence);

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPricePence = unitPricePence,
                Quantity = requestedItem.Quantity,
                LineTotalPence = lineTotalPence,
                Options = selectedOptions
                    .Select(selection => new OrderItemOption
                    {
                        Name = $"{selection.Group.Name}: {selection.Option.Name}",
                        PriceAdjustmentPence = selection.Option.PriceAdjustmentPence
                    })
                    .ToList()
            });
        }

        if (errors.Count > 0)
        {
            return OrderSubmissionResult.Invalid(errors);
        }

        var deliveryFeePence = orderType == OrderType.Delivery ? DeliveryFeePence : 0;
        var totalPence = checked(subtotalPence + deliveryFeePence + ServiceFeePence);
        var order = new Order
        {
            OrderNumber = await CreateOrderNumberAsync(cancellationToken),
            Type = orderType,
            Status = OrderStatus.Pending,
            PaymentMethod = PaymentMethod.NotSelected,
            PaymentStatus = PaymentStatus.Pending,
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            Postcode = NullIfWhiteSpace(request.Postcode),
            AddressLine = NullIfWhiteSpace(request.AddressLine),
            OrderNotes = NullIfWhiteSpace(request.OrderNotes),
            SubtotalPence = subtotalPence,
            DeliveryFeePence = deliveryFeePence,
            ServiceFeePence = ServiceFeePence,
            TotalPence = totalPence,
            Items = orderItems
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return OrderSubmissionResult.Accepted(order);
    }

    private static List<string> ValidateRequest(CreateOrderRequest request, out OrderType orderType)
    {
        var errors = new List<string>();
        if (!Enum.TryParse(request.OrderType, true, out orderType))
        {
            errors.Add("Order type must be delivery or collection.");
        }

        if (request.Items.Count > MaximumOrderLines)
        {
            errors.Add($"An order cannot contain more than {MaximumOrderLines} separate items.");
        }

        if (request.Items.Sum(item => item.Quantity) > MaximumOrderQuantity)
        {
            errors.Add($"An order cannot contain more than {MaximumOrderQuantity} items in total.");
        }

        if (orderType == OrderType.Delivery)
        {
            if (string.IsNullOrWhiteSpace(request.Postcode))
            {
                errors.Add("A postcode is required for delivery.");
            }

            if (string.IsNullOrWhiteSpace(request.AddressLine))
            {
                errors.Add("An address is required for delivery.");
            }
        }

        return errors;
    }

    private static List<ResolvedOption>? ValidateAndResolveOptions(
        Product product,
        CreateOrderItemRequest requestedItem,
        string itemPath,
        ICollection<string> errors)
    {
        var selections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var selection in requestedItem.Selections.Where(selection => !string.IsNullOrWhiteSpace(selection.Key)))
        {
            if (!selections.TryAdd(selection.Key.Trim(), selection.Value ?? []))
            {
                errors.Add($"{itemPath}: The option group '{selection.Key}' was supplied more than once.");
            }
        }

        var groupsBySlug = product.OptionGroups.ToDictionary(group => group.Slug, StringComparer.OrdinalIgnoreCase);
        foreach (var unknownGroup in selections.Keys.Where(groupSlug => !groupsBySlug.ContainsKey(groupSlug)))
        {
            errors.Add($"{itemPath}: Unknown option group '{unknownGroup}'.");
        }

        var resolved = new List<ResolvedOption>();
        foreach (var group in product.OptionGroups.OrderBy(group => group.DisplayOrder))
        {
            var selectedSlugs = selections.GetValueOrDefault(group.Slug, [])
                .Where(slug => !string.IsNullOrWhiteSpace(slug))
                .Select(slug => slug.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (selectedSlugs.Count < group.MinimumSelections || selectedSlugs.Count > group.MaximumSelections)
            {
                errors.Add($"{itemPath}: Choose between {group.MinimumSelections} and {group.MaximumSelections} option(s) for {group.Name}.");
                continue;
            }

            var availableOptions = group.Options
                .Where(option => option.IsAvailable)
                .ToDictionary(option => option.Slug, StringComparer.OrdinalIgnoreCase);

            foreach (var selectedSlug in selectedSlugs)
            {
                if (!availableOptions.TryGetValue(selectedSlug, out var option))
                {
                    errors.Add($"{itemPath}: Option '{selectedSlug}' is unavailable for {group.Name}.");
                    continue;
                }

                resolved.Add(new ResolvedOption(group, option));
            }
        }

        return errors.Any(error => error.StartsWith(itemPath, StringComparison.Ordinal)) ? null : resolved;
    }

    private async Task<string> CreateOrderNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = $"PK-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 10000)}";
            if (!await dbContext.Orders.AnyAsync(order => order.OrderNumber == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return $"PK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ResolvedOption(ProductOptionGroup Group, ProductOption Option);
}

public sealed record OrderSubmissionResult(Order? Order, IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Order is not null;

    public static OrderSubmissionResult Accepted(Order order) => new(order, []);

    public static OrderSubmissionResult Invalid(IReadOnlyList<string> errors) => new(null, errors);
}
