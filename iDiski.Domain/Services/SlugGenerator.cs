using System.Text;
using System.Text.RegularExpressions;

namespace iDiski.Domain.Services;

/// <summary>
/// Pure static domain service that generates URL-safe, SEO-friendly slugs from free text.
///
/// Rules applied in order:
///   1. Normalise Unicode (NFD decomposition) to strip accents: "Björn" → "Bjorn"
///   2. Lowercase everything
///   3. Replace any run of non-alphanumeric characters with a single hyphen
///   4. Trim leading/trailing hyphens
///   5. Hard-cap at <see cref="MaxLength"/> characters, cutting at a word boundary
///
/// Examples:
///   "Player of the Month – March 2025!" → "player-of-the-month-march-2025"
///   "Bafana Bafana ★ Awards"             → "bafana-bafana-awards"
///   "iDiski FC: Best XI"                 → "idiski-fc-best-xi"
/// </summary>
public static class SlugGenerator
{
    public const int MaxLength = 250;

    private static readonly Regex NonAlphanumeric =
        new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static string Generate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be empty.", nameof(title));

        // Step 1 — strip accents via Unicode normalisation
        var normalised = title.Normalize(NormalizationForm.FormD);
        var ascii = new StringBuilder(normalised.Length);
        foreach (var c in normalised)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                ascii.Append(c);
        }

        // Step 2-4 — lowercase, collapse non-alphanumeric runs, trim hyphens
        var slug = NonAlphanumeric
            .Replace(ascii.ToString().ToLowerInvariant(), "-")
            .Trim('-');

        // Step 5 — cap length at a word boundary
        if (slug.Length <= MaxLength)
            return slug;

        var cut = slug[..MaxLength].LastIndexOf('-');
        return cut > 0 ? slug[..cut] : slug[..MaxLength];
    }

    /// <summary>
    /// Appends a numeric suffix to resolve collisions.
    /// Usage: <c>SlugGenerator.WithSuffix("player-of-the-month", 2)</c>
    /// → <c>"player-of-the-month-2"</c>
    /// </summary>
    public static string WithSuffix(string baseSlug, int attempt) =>
        $"{baseSlug}-{attempt}";
}
