using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Toolkit.ResxAnalyzer;

/// <summary>
/// The set of key-naming conventions RESX008 knows how to validate.
/// </summary>
internal enum NamingConventionKind
{
    PascalCase = 0,
    CamelCase = 1,
    SnakeCase = 2,
    KebabCase = 3,
    Hierarchical = 4,
    Custom = 5,
}

/// <summary>
/// RESX008 configuration, read from <c>.editorconfig</c> via <c>AnalyzerConfigOptions</c>. The
/// rule is opt-in: it only runs when a project sets <c>resx_analyzer.key_naming_convention</c> to
/// a recognized value (typically under a <c>[*.resx]</c> section), for example:
/// <code>
/// [*.resx]
/// resx_analyzer.key_naming_convention = pascal_case
/// </code>
/// A missing or unrecognized value disables the rule entirely - no diagnostic is produced.
/// </summary>
internal sealed class KeyNamingOptions
{
    private const string ConventionOption = "resx_analyzer.key_naming_convention";
    private const string RegexOption = "resx_analyzer.key_naming_regex";
    private const string SeparatorOption = "resx_analyzer.key_naming_hierarchical_separator";
    private const string SegmentConventionOption = "resx_analyzer.key_naming_hierarchical_segment_convention";

    private static readonly Regex PascalCasePattern = new("^[A-Z][A-Za-z0-9]*$", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static readonly Regex CamelCasePattern = new("^[a-z][A-Za-z0-9]*$", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static readonly Regex SnakeCasePattern = new("^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static readonly Regex KebabCasePattern = new("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.None, TimeSpan.FromSeconds(1));

    private KeyNamingOptions(NamingConventionKind convention, string displayName, Regex? customPattern, string separator, NamingConventionKind segmentConvention)
    {
        Convention = convention;
        DisplayName = displayName;
        CustomPattern = customPattern;
        Separator = separator;
        SegmentConvention = segmentConvention;
    }

    public NamingConventionKind Convention { get; }

    /// <summary>Human-readable convention name, used in the diagnostic message.</summary>
    public string DisplayName { get; }

    private Regex? CustomPattern { get; }

    private string Separator { get; }

    private NamingConventionKind SegmentConvention { get; }

    /// <summary>
    /// Reads RESX008 configuration that applies to <paramref name="file"/>. Returns null when the
    /// rule is not configured (or is misconfigured) for that file, meaning it should not run.
    /// </summary>
    /// <param name="provider">The analyzer config options provider.</param>
    /// <param name="file">The file whose options are read.</param>
    public static KeyNamingOptions? Read(AnalyzerConfigOptionsProvider provider, Microsoft.CodeAnalysis.AdditionalText file)
    {
        AnalyzerConfigOptions options = provider.GetOptions(file);

        if (!options.TryGetValue(ConventionOption, out string? rawConvention) || string.IsNullOrWhiteSpace(rawConvention))
            return null;

        if (!TryParseConvention(rawConvention, out NamingConventionKind convention))
            return null;

        if (convention == NamingConventionKind.Custom)
        {
            if (!options.TryGetValue(RegexOption, out string? pattern) || string.IsNullOrWhiteSpace(pattern))
                return null;

            Regex? customPattern = TryCompile(pattern);
            if (customPattern is null)
                return null;

            return new KeyNamingOptions(convention, "custom", customPattern, ".", NamingConventionKind.PascalCase);
        }

        string separator = ".";
        NamingConventionKind segmentConvention = NamingConventionKind.PascalCase;

        if (convention == NamingConventionKind.Hierarchical)
        {
            if (options.TryGetValue(SeparatorOption, out string? configuredSeparator) && !string.IsNullOrEmpty(configuredSeparator))
                separator = configuredSeparator;

            if (options.TryGetValue(SegmentConventionOption, out string? configuredSegmentConvention)
                && TryParseConvention(configuredSegmentConvention, out NamingConventionKind parsedSegmentConvention)
                && parsedSegmentConvention != NamingConventionKind.Hierarchical
                && parsedSegmentConvention != NamingConventionKind.Custom)
            {
                segmentConvention = parsedSegmentConvention;
            }
        }

        return new KeyNamingOptions(convention, rawConvention.Trim(), null, separator, segmentConvention);
    }

    /// <summary>Checks whether <paramref name="key"/> matches this configuration's convention.</summary>
    /// <param name="key">The resource key to validate.</param>
    public bool IsValid(string key)
    {
        if (string.IsNullOrEmpty(key))
            return true;

        if (Convention == NamingConventionKind.Custom)
            return CustomPattern!.IsMatch(key);

        if (Convention != NamingConventionKind.Hierarchical)
            return GetPattern(Convention).IsMatch(key);

        string[] segments = key.Split([Separator], StringSplitOptions.None);
        Regex segmentPattern = GetPattern(SegmentConvention);

        return segments.Length > 0 && segments.All(segment => segment.Length > 0 && segmentPattern.IsMatch(segment));
    }

    private static Regex GetPattern(NamingConventionKind convention)
    {
        return convention switch
        {
            NamingConventionKind.PascalCase => PascalCasePattern,
            NamingConventionKind.CamelCase => CamelCasePattern,
            NamingConventionKind.SnakeCase => SnakeCasePattern,
            NamingConventionKind.KebabCase => KebabCasePattern,
            NamingConventionKind.Hierarchical => PascalCasePattern,
            NamingConventionKind.Custom => PascalCasePattern,
            _ => PascalCasePattern,
        };
    }

    private static bool TryParseConvention(string value, out NamingConventionKind convention)
    {
        switch (value.Trim().Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant())
        {
            case "pascalcase":
                convention = NamingConventionKind.PascalCase;
                return true;
            case "camelcase":
                convention = NamingConventionKind.CamelCase;
                return true;
            case "snakecase":
                convention = NamingConventionKind.SnakeCase;
                return true;
            case "kebabcase":
                convention = NamingConventionKind.KebabCase;
                return true;
            case "hierarchical":
                convention = NamingConventionKind.Hierarchical;
                return true;
            case "custom":
            case "regex":
                convention = NamingConventionKind.Custom;
                return true;
            default:
                convention = default;
                return false;
        }
    }

    private static Regex? TryCompile(string pattern)
    {
        try
        {
            return new Regex("^(?:" + pattern + ")$", RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
