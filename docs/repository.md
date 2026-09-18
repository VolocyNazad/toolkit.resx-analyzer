# Repository guide

Paths in this document are relative to the repository root.
Read and follow the [development policy](policies/development.md) alongside this guide.

## About

Source for `VolocyNazad.ResxAnalyzer` (NuGet package `VolocyNazad.ResxAnalyzer`):
a Roslyn `DiagnosticAnalyzer` that inspects .resx resource groups (a neutral
file such as `Strings.resx` plus its culture-specific satellites, e.g.
`Strings.ru.resx`) and reports:

- **RESX001** - a key from the neutral file is missing in a satellite.
- **RESX002** - a satellite declares a key that doesn't exist in the neutral file.
- **RESX003** - the same key is declared twice within one `.resx` file.
- **RESX004** - a translation's `{0}`, `{1}`, ... placeholders don't match the neutral value's.
- **RESX005** - a satellite translation is empty or whitespace-only.
- **RESX006** - a resource group has no satellite file for a culture used by other groups in the project.
- **RESX007** - a satellite file exists but its group has no neutral file at all.

It is a plain analyzer package with no Revit API or WPF dependency, usable in
any C# project that ships localized `.resx` resources - independent of how
those resources are consumed at runtime (`ResourceManager`,
`IStringLocalizer`, a generated accessor, ...).

The analyzer is a single `netstandard2.0` project (required for analyzers),
packaged directly as an `analyzers/dotnet/cs` NuGet package - no source
generator or code fix is involved, so there is no separate packaging shell
project, unlike `toolkit.xaml-constructor`.

## Repository structure

```
.
├── src/
│   └── Toolkit.ResxAnalyzer/
│       ├── DiagnosticDescriptors.cs       RESX001-RESX007 descriptors
│       ├── ResxCompletenessAnalyzer.cs    the DiagnosticAnalyzer (all 7 rules)
│       ├── ResxDocument.cs                .resx -> resource entries (name + value)
│       ├── ResxResourceName.cs            file name -> (group, culture) parsing
│       ├── AnalyzerReleases.Shipped.md
│       ├── AnalyzerReleases.Unshipped.md
│       └── build/
│           └── VolocyNazad.ResxAnalyzer.props   auto-imported into consumers;
│                                                  exposes .resx EmbeddedResource
│                                                  items as AdditionalFiles
└── tests/
    └── Toolkit.ResxAnalyzer.Tests/         analyzer tests against in-memory
                                             AdditionalText instances
```

## Technology stack

- `Microsoft.NET.Sdk`, `netstandard2.0`, `IsRoslynComponent=true`,
  `EnforceExtendedAnalyzerRules=true`.
- Packaged directly: `IncludeBuildOutput=false` + a `None` item packs the
  built analyzer DLL into `analyzers/dotnet/cs`; `DevelopmentDependency=true`
  so the package does not flow as a normal compile-time dependency to
  consumers of a library that references it.
- Central package management (`Directory.Packages.props`,
  `ManagePackageVersionsCentrally=true`), matching `toolkit.xaml-constructor`.
- Repo-wide global analyzers pinned in `Directory.Packages.props`:
  Roslynator.Analyzers, SonarAnalyzer.CSharp.
- `Microsoft.CodeAnalysis` / `.CSharp` are exact-pinned (`[4.14.0]`) - the
  Roslyn version shipped with the latest Visual Studio 2022 17.14 line, so
  the analyzer stays loadable by any current VS 2022 install. See the
  [Roslyn compatibility policy](policies/development.md#roslyn-compatibility).
- MinVer with the `v` tag prefix, as configured by `MinVerTagPrefix` in
  `Directory.Build.props`.
- Tests target `net10.0` and use xUnit v3 through Microsoft.Testing.Platform,
  driving the analyzer directly through `CSharpCompilation.WithAnalyzers` with
  a hand-rolled `AdditionalText` implementation instead of the
  `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` helper package.
- No Revit API dependency.

## Documentation layout

- `AGENTS.md` links to the required repository guidance.
- `docs/policies/development.md` contains the development policy.
- `docs/repository.md` describes the project, repository structure, and technology stack.
- `CHANGELOG.md` records changes under `Unreleased` before release.

The root solution exposes the documentation files under a `docs` solution folder in Visual Studio, preserving their subfolder structure. When adding documentation files, also add them as solution items; solution folders do not automatically include new files.

The root `global.json` selects stable .NET SDK 10.0 (minimum `10.0.103`, `rollForward: latestFeature`). CI and publishing install the SDK from this file. Additional SDK installations may provide older test runtimes. See the [SDK selection policy](policies/development.md#net-sdk-selection).

## Solution items

The root solution exposes repository-level documents and configuration under `solutionItems`, GitHub files and maintenance scripts in matching subfolders, and documentation under `docs/`. The list is explicit, not a filesystem glob; keep links up to date when files change. See the [solution items policy](policies/development.md#solution-items).

## Repository validation

`scripts/Validate-Repository.ps1` enforces the required repository documents,
their navigation links, and complete, valid Solution Items. The
`.github/workflows/repository-policy.yml` workflow runs it for pushes and pull
requests. See the [development policy](policies/development.md#repository-validation).

## Formatting

The root `.editorconfig` defines the portable formatting baseline. Existing repositories may add stricter C# or analyzer-specific settings. See the [development policy](policies/development.md#formatting-baseline).

## Testing

The automated test suite is in `tests/Toolkit.ResxAnalyzer.Tests` and verifies
resource-group matching (culture parsing, missing-neutral-file skip) and all
seven diagnostics: RESX001 (missing keys, including several at once and
unrelated groups), RESX002 (orphaned satellite keys), RESX003 (duplicate keys,
including in a satellite file with no neutral counterpart), RESX004
(mismatched placeholders, placeholder order, escaped braces), RESX005
(empty/whitespace translations, and that the neutral file itself is exempt),
RESX006 (a group missing another group's culture, and that groups sharing
the same cultures produce no diagnostic), and RESX007 (a satellite file with
no neutral file for its group, and that a satellite with a neutral file
present produces no diagnostic).

## Versioning and release tags

The package uses MinVer 8 with stable tags in the `vMAJOR.MINOR.PATCH` format. The tag without its `v` prefix is the NuGet package version. Manual publishing accepts exactly one matching tag at HEAD and verifies the package ID and version before pushing to NuGet.
