using Microsoft.CodeAnalysis;

namespace Toolkit.ResxAnalyzer;

/// <summary>
/// The diagnostic identifiers and descriptors reported by <see cref="ResxCompletenessAnalyzer"/>.
/// </summary>
public static class DiagnosticDescriptors
{
    /// <summary>The identifier of the missing resource key diagnostic.</summary>
    public const string MissingResourceKeyDiagnosticId = "RESX001";

    /// <summary>The identifier of the orphaned translation diagnostic.</summary>
    public const string ExtraResourceKeyDiagnosticId = "RESX002";

    /// <summary>The identifier of the duplicate resource key diagnostic.</summary>
    public const string DuplicateResourceKeyDiagnosticId = "RESX003";

    /// <summary>The identifier of the mismatched placeholder diagnostic.</summary>
    public const string MismatchedPlaceholdersDiagnosticId = "RESX004";

    /// <summary>The identifier of the empty translation diagnostic.</summary>
    public const string EmptyResourceValueDiagnosticId = "RESX005";

    /// <summary>The identifier of the missing culture file diagnostic.</summary>
    public const string MissingCultureFileDiagnosticId = "RESX006";

    /// <summary>The identifier of the orphaned satellite file diagnostic.</summary>
    public const string OrphanedSatelliteDiagnosticId = "RESX007";

    /// <summary>The identifier of the key naming convention diagnostic.</summary>
    public const string KeyNamingConventionDiagnosticId = "RESX008";

    /// <summary>Reports a key from the neutral file that a satellite file does not define.</summary>
    public static readonly DiagnosticDescriptor MissingResourceKeyRule = CreateRule(
        MissingResourceKeyDiagnosticId,
        "Resource table is incomplete",
        "Resource key '{0}' defined in '{1}' is missing in '{2}'");

    /// <summary>Reports a satellite key that the neutral file does not define.</summary>
    public static readonly DiagnosticDescriptor ExtraResourceKeyRule = CreateRule(
        ExtraResourceKeyDiagnosticId,
        "Resource table has an orphaned translation",
        "Resource key '{0}' in '{1}' does not exist in the neutral resource file '{2}'");

    /// <summary>Reports a key declared more than once within one resource file.</summary>
    public static readonly DiagnosticDescriptor DuplicateResourceKeyRule = CreateRule(
        DuplicateResourceKeyDiagnosticId,
        "Resource key is declared more than once",
        "Resource key '{0}' is declared {1} times in '{2}'");

    /// <summary>Reports a translation whose format placeholders differ from the neutral value.</summary>
    public static readonly DiagnosticDescriptor MismatchedPlaceholdersRule = CreateRule(
        MismatchedPlaceholdersDiagnosticId,
        "Resource translation placeholders do not match",
        "Resource key '{0}': placeholders {2} in '{1}' do not match placeholders {4} in '{3}'");

    /// <summary>Reports a satellite translation whose value is empty or whitespace-only.</summary>
    public static readonly DiagnosticDescriptor EmptyResourceValueRule = CreateRule(
        EmptyResourceValueDiagnosticId,
        "Resource translation is empty",
        "Resource key '{0}' in '{1}' has an empty or whitespace-only value");

    /// <summary>Reports a resource group that lacks a culture used by other groups in the project.</summary>
    public static readonly DiagnosticDescriptor MissingCultureFileRule = CreateRule(
        MissingCultureFileDiagnosticId,
        "Resource group is missing a required localization file",
        "Resource group '{0}' has no '{1}' resource file, but '{1}' is used by other resource groups in the project");

    /// <summary>Reports a satellite file whose resource group has no neutral file.</summary>
    public static readonly DiagnosticDescriptor OrphanedSatelliteRule = CreateRule(
        OrphanedSatelliteDiagnosticId,
        "Satellite resource file has no neutral resource file",
        "'{0}' is a culture-specific satellite resource file, but its neutral resource file '{1}' does not exist");

    /// <summary>Reports a key that does not match the configured naming convention.</summary>
    public static readonly DiagnosticDescriptor KeyNamingConventionRule = CreateRule(
        KeyNamingConventionDiagnosticId,
        "Resource key does not match the configured naming convention",
        "Resource key '{0}' in '{1}' does not match the configured naming convention '{2}'");

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
