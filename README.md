# Toolkit.ResxAnalyzer

[![NuGet](https://img.shields.io/nuget/vpre/VolocyNazad.ResxAnalyzer?logo=nuget)](https://www.nuget.org/packages/VolocyNazad.ResxAnalyzer/)
[![GitHub release](https://img.shields.io/github/release/VolocyNazad/toolkit.resx-analyzer.svg?logo=github)](https://github.com/VolocyNazad/toolkit.resx-analyzer/releases/latest)
[![GitHub license](https://img.shields.io/github/license/VolocyNazad/toolkit.resx-analyzer)](https://raw.githubusercontent.com/VolocyNazad/toolkit.resx-analyzer/main/LICENSE)
![build-and-test.yml](https://github.com/VolocyNazad/toolkit.resx-analyzer/workflows/.github/workflows/build-and-test.yml/badge.svg)

A Roslyn analyzer that warns at build time about incomplete or broken `.resx`
resource tables: a culture-specific satellite file (`Strings.ru.resx`,
`Strings.de.resx`, ...) missing a key from the neutral file (`Strings.resx`)
of the same group, a satellite with a key the neutral file no longer has, a
key declared twice in one file, a translation whose `{0}`/`{1}`/... format
placeholders don't match the neutral value's, or a translation that's empty.
See [Diagnostics](#diagnostics) below for the full list (RESX001-RESX005).

The check works on the `.resx` files themselves, so it applies no matter how
the resources are consumed at runtime - `ResourceManager`,
`Microsoft.Extensions.Localization.IStringLocalizer`, a generated
strongly-typed accessor, or anything else.

## Installation

- Grab the latest package on [NuGet](https://www.nuget.org/packages/VolocyNazad.ResxAnalyzer/).
- Reference it as an analyzer-only, build-time dependency:

```xml
<ItemGroup>
  <PackageReference Include="VolocyNazad.ResxAnalyzer" Version="x.y.z">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

No further setup is required: the package automatically exposes every
`.resx` `EmbeddedResource` item in the referencing project to the analyzer.

## How resource groups are matched

Files are grouped by their path with the trailing culture segment removed.
`Strings.resx` is the neutral file of the group; `Strings.ru.resx`,
`Strings.pt-BR.resx`, `Strings.zh-Hans.resx`, etc. are its culture-specific
satellites, matched by parsing the segment before `.resx` as a
`CultureInfo` name. A group without a neutral file (no plain
`<Name>.resx`) is skipped for RESX001/RESX002/RESX004/RESX005, since there
is then no key set to treat as canonical. RESX003 (duplicate key in one
file) is checked per file and does not depend on grouping, so it still
applies even to a satellite file with no matching neutral file.

## Basic usage

Given:

```
Strings.resx      Greeting, Farewell
Strings.ru.resx    Greeting
```

building the project reports:

```
warning RESX001: Resource key 'Farewell' defined in 'Strings.resx' is missing in 'Strings.ru.resx'
```

That's one example (RESX001); the other four diagnostics fire the same way,
as ordinary build warnings, whenever their condition is met - see
[Diagnostics](#diagnostics).

## Diagnostics

### RESX001

A resource key defined in a group's neutral `.resx` file is missing from one
of its culture-specific satellite `.resx` files.

### RESX002

A culture-specific satellite `.resx` file declares a key that does not exist
in the group's neutral `.resx` file - typically a stale translation left
behind after the key was renamed or removed.

### RESX003

The same resource key is declared more than once within a single `.resx`
file. Checked independently of resource grouping, so it also catches a
broken satellite file that has no matching neutral file.

### RESX004

A translation's `{0}`, `{1}`, ... format placeholders don't match the
neutral value's placeholders (order doesn't matter, but the set of indices
must). This is the most common cause of a `FormatException` at runtime after
a translation edit. Escaped braces (`{{`/`}}`) are not treated as
placeholders.

### RESX005

A translation value is empty or contains only whitespace - the key exists
in the satellite file but was never actually translated. Only checked for
satellite files, not the neutral file itself.

## Development documentation

- [Development policy](docs/policies/development.md)
- [Repository guide and technology stack](docs/repository.md)

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting changes.
