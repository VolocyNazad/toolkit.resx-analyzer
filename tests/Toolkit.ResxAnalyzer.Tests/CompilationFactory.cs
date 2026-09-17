using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Toolkit.ResxAnalyzer.Tests;

/// <summary>
/// Builds throwaway <see cref="Microsoft.CodeAnalysis.CSharp.CSharpCompilation"/> instances for
/// analyzer tests, referencing every assembly already loaded into the test process instead of
/// pulling in a separate reference-assembly package.
/// </summary>
internal static class CompilationFactory
{
    public static readonly ImmutableArray<MetadataReference> References =
    [
        .. AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => (MetadataReference)MetadataReference.CreateFromFile(assembly.Location)),
    ];
}
