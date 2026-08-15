using System.Text;

namespace PizzaNight.Services;

public static class SlugGenerator
{
    public static string Generate(string? requestedSlug, string fallbackName)
    {
        var source = string.IsNullOrWhiteSpace(requestedSlug) ? fallbackName : requestedSlug;
        var builder = new StringBuilder(source.Length);
        var previousWasSeparator = false;

        foreach (var character in source.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
