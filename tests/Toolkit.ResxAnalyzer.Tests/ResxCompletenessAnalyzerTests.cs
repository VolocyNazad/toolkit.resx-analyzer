using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Toolkit.ResxAnalyzer.Tests;

public sealed class ResxCompletenessAnalyzerTests
{
    [Fact]
    public async Task ReportsMissingKey_WhenSatelliteResxIsMissingAKeyPresentInTheNeutralResx()
    {
        TestAdditionalText neutral = new(
            "Strings.resx",
            CreateResx(("Greeting", "Hello"), ("Farewell", "Bye")));
        TestAdditionalText russian = new(
            "Strings.ru.resx",
            CreateResx(("Greeting", "Привет")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, russian);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.MissingResourceKeyDiagnosticId, diagnostic.Id);
        Assert.Contains("Farewell", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Strings.resx", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Strings.ru.resx", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotReportDiagnostic_WhenAllSatelliteResxFilesHaveEveryNeutralKey()
    {
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText russian = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));
        TestAdditionalText german = new("Strings.de.resx", CreateResx(("Greeting", "Hallo")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, russian, german);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReportKeyDiagnostics_ForUnrelatedResxGroups()
    {
        TestAdditionalText neutralA = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText neutralB = new("Errors.resx", CreateResx(("NotFound", "Not found")));
        TestAdditionalText russianA = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutralA, neutralB, russianA);

        // "Errors" is never compared key-by-key against "Strings.ru.resx" (that would be
        // RESX001/002/004/005) - it only gets flagged by RESX006, since the "Errors" group has
        // no "ru" satellite of its own while "ru" is used elsewhere in the project.
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.MissingCultureFileDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task ReportsMissingCultureFile_WhenAnotherGroupHasACultureThisGroupLacks()
    {
        TestAdditionalText stringsNeutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText stringsRu = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));
        TestAdditionalText errorsNeutral = new("Errors.resx", CreateResx(("NotFound", "Not found")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(stringsNeutral, stringsRu, errorsNeutral);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.MissingCultureFileDiagnosticId, diagnostic.Id);
        Assert.Contains("Errors", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("ru", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotReportMissingCultureFile_WhenAllGroupsHaveTheSameCultures()
    {
        TestAdditionalText stringsNeutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText stringsRu = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));
        TestAdditionalText errorsNeutral = new("Errors.resx", CreateResx(("NotFound", "Not found")));
        TestAdditionalText errorsRu = new("Errors.ru.resx", CreateResx(("NotFound", "Не найдено")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(stringsNeutral, stringsRu, errorsNeutral, errorsRu);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsMissingCultureFile_ForGroupMissingAnotherGroupsCulture()
    {
        TestAdditionalText stringsNeutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText stringsRu = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));
        TestAdditionalText stringsDe = new("Strings.de.resx", CreateResx(("Greeting", "Hallo")));
        TestAdditionalText errorsNeutral = new("Errors.resx", CreateResx(("NotFound", "Not found")));
        TestAdditionalText errorsRu = new("Errors.ru.resx", CreateResx(("NotFound", "Не найдено")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(
            stringsNeutral, stringsRu, stringsDe, errorsNeutral, errorsRu);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.MissingCultureFileDiagnosticId, diagnostic.Id);
        Assert.Contains("Errors", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("de", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsOrphanedSatellite_WhenGroupHasNoNeutralFile()
    {
        TestAdditionalText russian = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(russian);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.OrphanedSatelliteDiagnosticId, diagnostic.Id);
        Assert.Contains("Strings.ru.resx", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Strings.resx", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotReportOrphanedSatellite_WhenNeutralFileExists()
    {
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText russian = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, russian);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReportKeyDiagnostics_WhenNoNeutralResxExistsForTheGroup()
    {
        TestAdditionalText russian = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));
        TestAdditionalText german = new("Strings.de.resx", CreateResx());

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(russian, german);

        // No neutral file to compare keys against, so RESX001/002/004/005 never run - but both
        // satellites are still each flagged as orphaned by RESX007.
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticDescriptors.OrphanedSatelliteDiagnosticId, d.Id));
    }

    [Fact]
    public async Task ReportsOneDiagnosticPerMissingKey_WhenSeveralKeysAreMissing()
    {
        TestAdditionalText neutral = new(
            "Strings.resx",
            CreateResx(("A", "1"), ("B", "2"), ("C", "3")));
        TestAdditionalText french = new("Strings.fr.resx", CreateResx());

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, french);

        Assert.Equal(3, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticDescriptors.MissingResourceKeyDiagnosticId, d.Id));
    }

    [Fact]
    public async Task DoesNotTreatADottedBaseNameSegmentAsACulture()
    {
        // "v2" is not a registered CultureInfo name, so "Strings.v2.resx" must stay a distinct
        // resource group from "Strings.resx" rather than being read as a "v2" satellite.
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText other = new("Strings.v2.resx", CreateResx());

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, other);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IgnoresAdditionalFilesThatAreNotResx()
    {
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText russian = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));
        TestAdditionalText unrelated = new("Strings.ru.json", "{}");

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, russian, unrelated);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsExtraKey_WhenSatelliteResxHasAKeyNotPresentInTheNeutralResx()
    {
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText russian = new(
            "Strings.ru.resx",
            CreateResx(("Greeting", "Привет"), ("Obsolete", "Устарело")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, russian);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.ExtraResourceKeyDiagnosticId, diagnostic.Id);
        Assert.Contains("Obsolete", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Strings.ru.resx", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Strings.resx", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsDuplicateKey_WhenTheSameNameIsDeclaredTwiceInOneFile()
    {
        TestAdditionalText neutral = new(
            "Strings.resx",
            CreateResx(("Greeting", "Hello"), ("Greeting", "Hi")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.DuplicateResourceKeyDiagnosticId, diagnostic.Id);
        Assert.Contains("Greeting", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("2", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Strings.resx", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsDuplicateKey_ForACultureFileEvenWithoutAMatchingNeutralFile()
    {
        // Duplicate-name detection runs per file, independently of grouping, so it still catches
        // a broken satellite file even when there is no neutral file to pair it with - which is
        // itself separately flagged by RESX007.
        TestAdditionalText russian = new(
            "Strings.ru.resx",
            CreateResx(("Greeting", "Привет"), ("Greeting", "Здравствуйте")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(russian);

        Assert.Equal(2, diagnostics.Length);
        Assert.Equal(1, diagnostics.Count(d => d.Id == DiagnosticDescriptors.DuplicateResourceKeyDiagnosticId));
        Assert.Equal(1, diagnostics.Count(d => d.Id == DiagnosticDescriptors.OrphanedSatelliteDiagnosticId));
    }

    [Fact]
    public async Task ReportsMismatchedPlaceholders_WhenTranslationDropsAPlaceholder()
    {
        TestAdditionalText neutral = new(
            "Strings.resx",
            CreateResx(("Welcome", "Hello {0}, you have {1} messages")));
        TestAdditionalText french = new(
            "Strings.fr.resx",
            CreateResx(("Welcome", "Bonjour {0}")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, french);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.MismatchedPlaceholdersDiagnosticId, diagnostic.Id);
        Assert.Contains("Welcome", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("{1}", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotReportMismatchedPlaceholders_WhenPlaceholdersMatchInADifferentOrder()
    {
        TestAdditionalText neutral = new(
            "Strings.resx",
            CreateResx(("Welcome", "Hello {0}, you have {1} messages")));
        TestAdditionalText french = new(
            "Strings.fr.resx",
            CreateResx(("Welcome", "Vous avez {1} messages, {0}")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, french);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReportMismatchedPlaceholders_ForEscapedBraces()
    {
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Braces", "Use {{0}} literally")));
        TestAdditionalText french = new("Strings.fr.resx", CreateResx(("Braces", "Utilisez {{0}} littéralement")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, french);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsEmptyValue_WhenTranslationIsWhitespaceOnly()
    {
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Greeting", "Hello")));
        TestAdditionalText russian = new("Strings.ru.resx", CreateResx(("Greeting", "   ")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, russian);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticDescriptors.EmptyResourceValueDiagnosticId, diagnostic.Id);
        Assert.Contains("Greeting", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotReportEmptyValue_ForTheNeutralFileItself()
    {
        // An empty value in the neutral file is a different, author-side concern - only
        // satellite translations are checked for emptiness.
        TestAdditionalText neutral = new("Strings.resx", CreateResx(("Greeting", "")));
        TestAdditionalText russian = new("Strings.ru.resx", CreateResx(("Greeting", "Привет")));

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(neutral, russian);

        Assert.Empty(diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(params AdditionalText[] additionalFiles)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Tests",
            [],
            CompilationFactory.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        AnalyzerOptions options = new([.. additionalFiles]);
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            [new ResxCompletenessAnalyzer()],
            options);

        ImmutableArray<Diagnostic> diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return [.. diagnostics.OrderBy(d => d.Id, StringComparer.Ordinal).ThenBy(d => d.GetMessage(CultureInfo.InvariantCulture), StringComparer.Ordinal)];
    }

    private static string CreateResx(params (string Name, string Value)[] entries)
    {
        string data = string.Join(
            Environment.NewLine,
            entries.Select(e => $"""  <data name="{e.Name}" xml:space="preserve"><value>{e.Value}</value></data>"""));

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
            {data}
            </root>
            """;
    }
}
