using System.ComponentModel.DataAnnotations;
using PizzaNight.Models;
using PizzaNight.Services;
using Xunit;

namespace PizzaNight.Tests;

public sealed class AdminMenuValidationTests
{
    [Theory]
    [InlineData(null, "Garlic Bread & Cheese", "garlic-bread-cheese")]
    [InlineData("  Family DEAL  ", "Ignored", "family-deal")]
    [InlineData("Loaded---Fries", "Ignored", "loaded-fries")]
    public void Slug_generator_creates_safe_menu_identifiers(string? requested, string name, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(requested, name));
    }

    [Fact]
    public void Required_option_group_must_require_at_least_one_choice()
    {
        var model = new OptionGroupFormViewModel
        {
            Name = "Choose size",
            IsRequired = true,
            MinimumSelections = 0,
            MaximumSelections = 1
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.ErrorMessage!.Contains("at least one", StringComparison.Ordinal));
    }

    [Fact]
    public void Minimum_selections_cannot_exceed_maximum()
    {
        var model = new OptionGroupFormViewModel
        {
            Name = "Add extras",
            MinimumSelections = 3,
            MaximumSelections = 2
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.ErrorMessage!.Contains("greater", StringComparison.Ordinal));
    }
}
