# Build and toolchain policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: changing the SDK, a project, a package version or a warning rule.

A build is reproducible from a clean checkout with the SDK selected by the repository and the package
versions declared in the repository. It states rules, not build recipes: commands and stack facts
belong to the [repository guide](../repository.md).

## SDK selection

- Take the .NET SDK from the repository-root `global.json`; it is the only source of the SDK for
  local builds, CI and publishing.
- Keep `global.json` on a stable SDK `10.0.103` or later in the `10.0` major/minor line, with
  `rollForward: latestFeature` and `allowPrerelease: false`. Do not move to another major/minor line
  without changing this policy and `global.json` in the same change.
- Read the SDK in every GitHub Actions setup step with `global-json-file: global.json` after
  checkout. Do not install a floating or explicitly versioned SDK in the workflow instead.
- This policy does not change project target frameworks; a target-framework change updates the
  [repository guide](../repository.md) with the stack facts.

## Projects

- `src/Toolkit.ResxAnalyzer` is a `Microsoft.NET.Sdk` Roslyn analyzer targeting `netstandard2.0`
  with `IsRoslynComponent=true` and `EnforceExtendedAnalyzerRules=true`. It packages directly into
  `analyzers/dotnet/cs` and has no Revit API dependency; the [repository guide](../repository.md)
  records the stack.
- `tests/Toolkit.ResxAnalyzer.Tests` is the headless test project and is not a package.
- A new project arrives with its solution entry, its tests where the [testing policy](testing.md)
  requires them, and the matching CI step in the same change.
- Do not add a project that only exists to work around a build setting; fix the shared configuration
  instead.

## Central package management

- Pin every package version in the nearest `Directory.Packages.props`. The root file serves the
  library; `tests/Directory.Packages.props` imports it and holds the test-only versions.
- Keep central package management enabled (`ManagePackageVersionsCentrally=true`); a
  `PackageReference` in a project carries no `Version`.
- Add repository-wide analyzer and generator packages through `GlobalPackageReference` with private
  assets. Do not repeat them per project.
- Change a package version in one place only. When a project resolves a version you did not expect,
  look for a nearer props file before editing the project.

## Warnings

- Treat compiler and analyzer warnings as delayed errors. Do not leave new warnings in code you
  touch; fix them in the same change.
- Suppress a deliberate false positive as narrowly as possible and add a nearby comment explaining
  why.
- Keep every build warning-free. `.github/workflows/build-and-test.yml` and `publish-nuget.yml` build
  with `-p:TreatWarningsAsErrors=true`, so a warning fails both the pull request and the publish.
- Do not rely on a local build to find warnings; build the configuration you touch before opening the
  pull request.

## Analyzers

- The root `Directory.Build.props` enables `AnalysisMode=AllEnabledByDefault` for every project.
  Treat the resulting diagnostics as part of the build contract, not as optional advice.
- A rule that is wrong for this library is disabled in `.editorconfig` with a comment naming the
  reason, never with a file-wide `#pragma warning disable` in source.
- A new analyzer package follows the central package rule above and is reviewed like production code:
  a diagnostic that fires on the existing code is fixed or explicitly disabled in the same change.

## Roslyn compatibility

- The distributed analyzer targets Microsoft.CodeAnalysis 4.14, matching Visual Studio 2022
  version 17.14.
- Keep Roslyn references private implementation dependencies and verify the analyzer tests before
  changing this compatibility baseline.
- A Roslyn baseline change is a major release when it drops a supported host, as the
  [release policy](releases.md) defines.

## Public API documentation

`GenerateDocumentationFile` is enabled repository-wide. The strict audit promoted `CS1591` to an
error runs in CI with `-p:EnforcePublicApiDocumentation=true`; the [documentation policy](documentation.md)
owns what a public API must document.

The rest of the contributor workflow is defined by the [development policy](development.md).
