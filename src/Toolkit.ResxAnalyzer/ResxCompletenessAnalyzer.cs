using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Toolkit.ResxAnalyzer;

/// <summary>
/// Inspects .resx resource groups (a neutral file such as <c>Strings.resx</c> plus its
/// culture-specific satellites, e.g. <c>Strings.ru.resx</c>) and reports RESX001-RESX007:
/// missing keys, orphaned keys, duplicate keys within one file, mismatched format placeholders,
/// empty translations, resource groups missing a satellite file for a culture that is present
/// in other resource groups in the project, and satellite files that have no neutral file at
/// all.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class ResxCompletenessAnalyzer : DiagnosticAnalyzer
{
    private static readonly Regex PlaceholderPattern = new(@"\{(\d+)(?:,-?\d+)?(?::[^{}]*)?\}", RegexOptions.None, TimeSpan.FromSeconds(1));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.MissingResourceKeyRule,
        DiagnosticDescriptors.ExtraResourceKeyRule,
        DiagnosticDescriptors.DuplicateResourceKeyRule,
        DiagnosticDescriptors.MismatchedPlaceholdersRule,
        DiagnosticDescriptors.EmptyResourceValueRule,
        DiagnosticDescriptors.MissingCultureFileRule,
        DiagnosticDescriptors.OrphanedSatelliteRule);

    public override void Initialize(AnalysisContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        ImmutableArray<AdditionalText> resxFiles = [.. context.Options.AdditionalFiles
            .Where(file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))];

        if (resxFiles.Length == 0)
            return;

        foreach (AdditionalText file in resxFiles)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            AnalyzeDuplicates(context, file);
        }

        ImmutableArray<ResourceGroup> groups = [.. GroupResxFiles(resxFiles, context.CancellationToken)];

        foreach (ResourceGroup group in groups)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (group.Neutral is null)
            {
                if (group.Cultures.Count > 0)
                    AnalyzeOrphanedSatellites(context, group);

                continue;
            }

            if (group.Cultures.Count == 0)
                continue;

            AnalyzeGroup(context, group);
        }

        AnalyzeRequiredCultures(context, groups);
    }

    private static IEnumerable<ResourceGroup> GroupResxFiles(ImmutableArray<AdditionalText> resxFiles, CancellationToken cancellationToken)
    {
        Dictionary<string, ResourceGroup> groups = new(StringComparer.Ordinal);

        foreach (AdditionalText file in resxFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ResxResourceName.TryParse(file.Path, out string groupKey, out CultureInfo? culture))
                continue;

            if (!groups.TryGetValue(groupKey, out ResourceGroup? group))
            {
                group = new ResourceGroup();
                groups[groupKey] = group;
            }

            if (culture is null)
                group.Neutral = file;
            else
                group.Cultures[culture.Name] = file;
        }

        return groups.Values;
    }

    private static void AnalyzeDuplicates(CompilationAnalysisContext context, AdditionalText file)
    {
        SourceText? text = file.GetText(context.CancellationToken);
        if (text is null)
            return;

        ImmutableArray<ResxEntry> entries = ResxDocument.ReadEntries(text.ToString());
        if (entries.Length == 0)
            return;

        Location location = CreateLocation(file.Path);
        string fileName = Path.GetFileName(file.Path);

        foreach (IGrouping<string, ResxEntry> duplicates in entries.GroupBy(entry => entry.Name, StringComparer.Ordinal))
        {
            int count = duplicates.Count();
            if (count <= 1)
                continue;

            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.DuplicateResourceKeyRule,
                location,
                duplicates.Key,
                count,
                fileName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeGroup(CompilationAnalysisContext context, ResourceGroup group)
    {
        AdditionalText neutralFile = group.Neutral!;
        SourceText? neutralText = neutralFile.GetText(context.CancellationToken);
        if (neutralText is null)
            return;

        Dictionary<string, string> neutralEntries = ToFirstOccurrenceMap(ResxDocument.ReadEntries(neutralText.ToString()));
        if (neutralEntries.Count == 0)
            return;

        string neutralFileName = Path.GetFileName(neutralFile.Path);

        foreach (AdditionalText cultureFile in group.Cultures.Values)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            AnalyzeCulture(context, neutralFileName, neutralEntries, cultureFile);
        }
    }

    private static void AnalyzeOrphanedSatellites(CompilationAnalysisContext context, ResourceGroup group)
    {
        string expectedNeutralFileName = GetGroupDisplayName(group) + ".resx";

        foreach (string satellitePath in group.Cultures.Values.Select(satelliteFile => satelliteFile.Path))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.OrphanedSatelliteRule,
                CreateLocation(satellitePath),
                Path.GetFileName(satellitePath),
                expectedNeutralFileName));
        }
    }

    private static void AnalyzeCulture(CompilationAnalysisContext context, string neutralFileName, Dictionary<string, string> neutralEntries, AdditionalText cultureFile)
    {
        SourceText? cultureText = cultureFile.GetText(context.CancellationToken);
        if (cultureText is null)
            return;

        Dictionary<string, string> cultureEntries = ToFirstOccurrenceMap(ResxDocument.ReadEntries(cultureText.ToString()));
        Location location = CreateLocation(cultureFile.Path);
        string cultureFileName = Path.GetFileName(cultureFile.Path);

        foreach (KeyValuePair<string, string> neutral in neutralEntries)
        {
            if (!cultureEntries.TryGetValue(neutral.Key, out string? cultureValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MissingResourceKeyRule,
                    location,
                    neutral.Key,
                    neutralFileName,
                    cultureFileName));

                continue;
            }

            if (string.IsNullOrWhiteSpace(cultureValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.EmptyResourceValueRule,
                    location,
                    neutral.Key,
                    cultureFileName));
            }

            ImmutableSortedSet<int> neutralPlaceholders = ExtractPlaceholders(neutral.Value);
            ImmutableSortedSet<int> culturePlaceholders = ExtractPlaceholders(cultureValue);

            if (!neutralPlaceholders.SetEquals(culturePlaceholders))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MismatchedPlaceholdersRule,
                    location,
                    neutral.Key,
                    cultureFileName,
                    FormatPlaceholders(culturePlaceholders),
                    neutralFileName,
                    FormatPlaceholders(neutralPlaceholders)));
            }
        }

        foreach (string cultureKey in cultureEntries.Keys.Where(key => !neutralEntries.ContainsKey(key)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExtraResourceKeyRule,
                location,
                cultureKey,
                cultureFileName,
                neutralFileName));
        }
    }

    private static void AnalyzeRequiredCultures(CompilationAnalysisContext context, ImmutableArray<ResourceGroup> groups)
    {
        // A culture is "required" project-wide once at least one resource group has a satellite
        // file for it; every other group is then expected to have a matching file for that same
        // culture. This is deliberately not scoped to comparing keys within a group - it only
        // checks that the .resx file itself exists.
        string[] requiredCultureNames = [.. groups
            .SelectMany(group => group.Cultures.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (requiredCultureNames.Length == 0)
            return;

        foreach (ResourceGroup group in groups)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            string groupName = GetGroupDisplayName(group);

            foreach (string cultureName in requiredCultureNames.Where(name => !group.Cultures.ContainsKey(name)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MissingCultureFileRule,
                    Location.None,
                    groupName,
                    cultureName));
            }
        }
    }

    private static string GetGroupDisplayName(ResourceGroup group)
    {
        if (group.Neutral is not null)
            return Path.GetFileNameWithoutExtension(group.Neutral.Path);

        // No neutral file: fall back to the first satellite, with its own culture segment
        // stripped back off (e.g. "Errors.ru" -> "Errors").
        KeyValuePair<string, AdditionalText> first = group.Cultures.First();
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(first.Value.Path);
        string cultureSuffix = "." + first.Key;

        return fileNameWithoutExtension.EndsWith(cultureSuffix, StringComparison.OrdinalIgnoreCase)
            ? fileNameWithoutExtension.Substring(0, fileNameWithoutExtension.Length - cultureSuffix.Length)
            : fileNameWithoutExtension;
    }

    private static Dictionary<string, string> ToFirstOccurrenceMap(ImmutableArray<ResxEntry> entries)
    {
        // Duplicate names within one file are reported by AnalyzeDuplicates (RESX003); here the
        // first declaration wins, matching how the .NET resource generator resolves duplicates.
        return entries
            .GroupBy(entry => entry.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);
    }

    private static ImmutableSortedSet<int> ExtractPlaceholders(string value)
    {
        string sanitized = value.Replace("{{", string.Empty).Replace("}}", string.Empty);
        ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateBuilder<int>();

        foreach (Match match in PlaceholderPattern.Matches(sanitized))
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
                builder.Add(index);
        }

        return builder.ToImmutable();
    }

    private static string FormatPlaceholders(IEnumerable<int> placeholders)
    {
        List<string> formatted = [.. placeholders.Select(index => "{" + index.ToString(CultureInfo.InvariantCulture) + "}")];
        return formatted.Count == 0 ? "(none)" : string.Join(", ", formatted);
    }

    private static Location CreateLocation(string path)
    {
        return Location.Create(
            path,
            new TextSpan(0, 0),
            new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
    }

    private sealed class ResourceGroup
    {
        public AdditionalText? Neutral { get; set; }

        /// <summary>Satellite files in this group, keyed by their parsed culture name.</summary>
        public Dictionary<string, AdditionalText> Cultures { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
