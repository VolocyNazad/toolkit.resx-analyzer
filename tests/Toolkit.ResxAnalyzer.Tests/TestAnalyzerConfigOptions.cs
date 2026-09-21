using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Toolkit.ResxAnalyzer.Tests;

/// <summary>
/// A minimal in-memory <see cref="AnalyzerConfigOptionsProvider"/>/<see cref="AnalyzerConfigOptions"/>
/// pair used to feed .editorconfig-style key/value pairs (such as RESX008's
/// <c>resx_analyzer.key_naming_convention</c>) to the analyzer in tests, per <see cref="AdditionalText"/>.
/// </summary>
internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly Dictionary<string, string> _options;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string>? options = null)
    {
        _options = options ?? [];
    }

    public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_options);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestAnalyzerConfigOptions(_options);

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestAnalyzerConfigOptions(_options);
}

internal sealed class TestAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, out string value)
    {
        if (options.TryGetValue(key, out string? found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
