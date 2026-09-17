using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Toolkit.ResxAnalyzer.Tests;

/// <summary>
/// A minimal in-memory <see cref="AdditionalText"/> used to feed .resx content to the analyzer
/// in tests without touching disk.
/// </summary>
internal sealed class TestAdditionalText : AdditionalText
{
    private readonly SourceText _text;

    public TestAdditionalText(string path, string content)
    {
        Path = path;
        _text = SourceText.From(content);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
}
