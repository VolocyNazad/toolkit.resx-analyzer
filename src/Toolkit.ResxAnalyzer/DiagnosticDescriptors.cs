using Microsoft.CodeAnalysis;

namespace Toolkit.ResxAnalyzer;

public static class DiagnosticDescriptors
{
    public const string MissingResourceKeyDiagnosticId = "RESX001";
    public const string ExtraResourceKeyDiagnosticId = "RESX002";
    public const string DuplicateResourceKeyDiagnosticId = "RESX003";
    public const string MismatchedPlaceholdersDiagnosticId = "RESX004";
    public const string EmptyResourceValueDiagnosticId = "RESX005";
    public const string MissingCultureFileDiagnosticId = "RESX006";
    public const string OrphanedSatelliteDiagnosticId = "RESX007";

    public static readonly DiagnosticDescriptor MissingResourceKeyRule = CreateRule(
        MissingResourceKeyDiagnosticId,
        "Resource table is incomplete",
        "Resource key '{0}' defined in '{1}' is missing in '{2}'");

    public static readonly DiagnosticDescriptor ExtraResourceKeyRule = CreateRule(
        ExtraResourceKeyDiagnosticId,
        "Resource table has an orphaned translation",
        "Resource key '{0}' in '{1}' does not exist in the neutral resource file '{2}'");

    public static readonly DiagnosticDescriptor DuplicateResourceKeyRule = CreateRule(
        DuplicateResourceKeyDiagnosticId,
        "Resource key is declared more than once",
        "Resource key '{0}' is declared {1} times in '{2}'");

    public static readonly DiagnosticDescriptor MismatchedPlaceholdersRule = CreateRule(
        MismatchedPlaceholdersDiagnosticId,
        "Resource translation placeholders do not match",
        "Resource key '{0}': placeholders {2} in '{1}' do not match placeholders {4} in '{3}'");

    public static readonly DiagnosticDescriptor EmptyResourceValueRule = CreateRule(
        EmptyResourceValueDiagnosticId,
        "Resource translation is empty",
        "Resource key '{0}' in '{1}' has an empty or whitespace-only value");

    public static readonly DiagnosticDescriptor MissingCultureFileRule = CreateRule(
        MissingCultureFileDiagnosticId,
        "Resource group is missing a required localization file",
        "Resource group '{0}' has no '{1}' resource file, but '{1}' is used by other resource groups in the project");

    public static readonly DiagnosticDescriptor OrphanedSatelliteRule = CreateRule(
        OrphanedSatelliteDiagnosticId,
        "Satellite resource file has no neutral resource file",
        "'{0}' is a culture-specific satellite resource file, but its neutral resource file '{1}' does not exist");

    private static DiagnosticDescriptor CreateRule(string id, string title, string messageFormat, DiagnosticSeverity severity = DiagnosticSeverity.Warning)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            messageFormat,
            "Localization",
            severity,
            true,
            null,
            $"https://github.com/VolocyNazad/toolkit.resx-analyzer?tab=readme-ov-file#{id.ToLowerInvariant()}",
            WellKnownDiagnosticTags.Build);
    }
}
