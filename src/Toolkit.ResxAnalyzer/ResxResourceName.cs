using System.Globalization;

namespace Toolkit.ResxAnalyzer;

/// <summary>
/// Parses a .resx file path into its resource group's base name and, when the file name carries
/// a culture segment (for example <c>Strings.ru.resx</c>), the parsed <see cref="CultureInfo"/>.
/// A file without a (valid) culture segment - <c>Strings.resx</c> - is the neutral file of its
/// group.
/// </summary>
internal static class ResxResourceName
{
    /// <summary>
    /// <see cref="CultureInfo.GetCultureInfo(string)"/> does not reliably throw for an unregistered-but-
    /// syntactically-plausible tag (behavior depends on the underlying ICU/NLS globalization
    /// data), so an arbitrary dotted segment such as "Strings.v2.resx" or "Strings.Designer.resx"
    /// can silently be accepted as a "culture" if we only try/catch it. Matching against the set
    /// of actually registered cultures instead avoids that false positive.
    /// </summary>
    private static readonly Lazy<HashSet<string>> KnownCultureNames = new(() =>
        new HashSet<string>(
            CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Select(culture => culture.Name)
                .Where(name => !string.IsNullOrEmpty(name)),
            StringComparer.OrdinalIgnoreCase));

    public static bool TryParse(string filePath, out string groupKey, out CultureInfo? culture)
    {
        groupKey = string.Empty;
        culture = null;

        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        if (string.IsNullOrEmpty(fileName))
            return false;

        int lastDot = fileName.LastIndexOf('.');
        if (lastDot > 0 && TryParseCulture(fileName.Substring(lastDot + 1), out CultureInfo parsedCulture))
        {
            culture = parsedCulture;
            fileName = fileName.Substring(0, lastDot);
        }

        groupKey = Path.Combine(directory, fileName).Replace('\\', '/').ToLowerInvariant();
        return true;
    }

    private static bool TryParseCulture(string candidate, out CultureInfo culture)
    {
        culture = CultureInfo.InvariantCulture;

        if (candidate.Length == 0 || !KnownCultureNames.Value.Contains(candidate))
            return false;

        culture = CultureInfo.GetCultureInfo(candidate);
        return !culture.Equals(CultureInfo.InvariantCulture);
    }
}
