# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- RESX006: warn when a resource group has no satellite `.resx` file for a culture that at
  least one other resource group in the project does have (for example, `Strings.ru.resx`
  exists but `Errors.ru.resx` doesn't). The set of required cultures is inferred automatically
  as the union of every culture used anywhere in the project - there is no configuration for
  it. Reported once per missing group/culture combination, at the project level rather than
  against a specific file, since there is no file to point at.
- RESX007: warn when a culture-specific satellite `.resx` file (for example `Strings.ru.resx`)
  exists but its group has no neutral file (`Strings.resx`) at all. Reported against the
  orphaned satellite file itself; unlike RESX006, this doesn't depend on any other resource
  group existing.

## [1.0.1] - 2026-09-18

### Fixed

- `build/VolocyNazad.ResxAnalyzer.props` failed every consuming project's build with
  `MSB4190: The reference to the built-in metadata "Extension" ... is not allowed in this
  condition`. The `%(EmbeddedResource.Extension)` item-metadata reference was used in a
  `Condition` on a plain, top-level `ItemGroup` - item metadata in a `Condition` is only valid
  inside an MSBuild `Target`. The `AdditionalFiles` item is now added by a
  `Target BeforeTargets="CoreCompile"` instead, which also guarantees the project's
  default-globbed `@(EmbeddedResource)` items already exist by the time it runs.

## [1.0.0] - 2026-09-17

### Added

- RESX001: warn when a culture-specific `.resx` satellite file (for example `Strings.ru.resx`)
  is missing a key that is present in the neutral `.resx` (`Strings.resx`) of the same
  resource group. The analyzer inspects the `.resx` files directly, so it applies
  regardless of how resources are read at runtime (`ResourceManager`,
  `IStringLocalizer`, a generated strongly-typed accessor, and so on).
- RESX002: warn when a satellite `.resx` file declares a key that does not exist in the
  group's neutral `.resx` file (a stale translation left behind after a rename/removal).
- RESX003: warn when the same resource key is declared more than once within a single
  `.resx` file. Checked per file, independently of resource grouping.
- RESX004: warn when a translation's `{0}`, `{1}`, ... format placeholders don't match the
  neutral value's placeholders.
- RESX005: warn when a satellite translation value is empty or whitespace-only.
