# Changelog

All notable changes to XboxFastz are documented here.

## [Unreleased]

### Added

- Added validation for downloaded IP lists before they replace local data.
- Added JSON validation for `Translation.json` updates.

### Changed

- IP updates continue to use the existing XboxFastz upstream source, proxy endpoints, and jsDelivr fallback.
- Valid update files are written through a temporary file before replacing the cached file.

### Fixed

- Fixed malformed or empty update responses replacing valid local IP data.
- Failed IP updates now retain the existing cached data and write a diagnostic message containing the affected filename.

### Documentation

- Rebranded user-facing documentation as XboxFastz while preserving XboxDownload attribution.
- Added consolidated English and Chinese documentation.
- Added contribution guidance and documented upstream attribution and license status.

## [3.0.0.65]

- Preserved the upstream XboxDownload release as the foundation for XboxFastz.
