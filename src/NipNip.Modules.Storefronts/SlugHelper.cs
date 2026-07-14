using System.Text;
using System.Text.RegularExpressions;

namespace NipNip.Modules.Storefronts;

public static partial class SlugHelper
{
    // Georgian National System (2002) romanization, used on road signs — ejective marks dropped for URL-friendliness.
    private static readonly Dictionary<char, string> GeorgianTransliteration = new()
    {
        ['ა'] = "a", ['ბ'] = "b", ['გ'] = "g", ['დ'] = "d", ['ე'] = "e",
        ['ვ'] = "v", ['ზ'] = "z", ['თ'] = "t", ['ი'] = "i", ['კ'] = "k",
        ['ლ'] = "l", ['მ'] = "m", ['ნ'] = "n", ['ო'] = "o", ['პ'] = "p",
        ['ჟ'] = "zh", ['რ'] = "r", ['ს'] = "s", ['ტ'] = "t", ['უ'] = "u",
        ['ფ'] = "p", ['ქ'] = "k", ['ღ'] = "gh", ['ყ'] = "q", ['შ'] = "sh",
        ['ჩ'] = "ch", ['ც'] = "ts", ['ძ'] = "dz", ['წ'] = "ts", ['ჭ'] = "ch",
        ['ხ'] = "kh", ['ჯ'] = "j", ['ჰ'] = "h",
    };

    public static string Slugify(string input, string fallback = "item")
    {
        var slug = Transliterate(input.Trim().ToLowerInvariant());
        slug = WhitespaceRegex().Replace(slug, "-");
        slug = InvalidCharsRegex().Replace(slug, "");
        slug = MultiHyphenRegex().Replace(slug, "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? fallback : slug;
    }

    public static string WithRandomSuffix(string baseSlug) => $"{baseSlug}-{Random.Shared.Next(1000, 99999)}";

    private static string Transliterate(string input)
    {
        var sb = new StringBuilder(input.Length * 2);
        foreach (var ch in input)
            sb.Append(GeorgianTransliteration.TryGetValue(ch, out var latin) ? latin : ch.ToString());
        return sb.ToString();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultiHyphenRegex();
}
