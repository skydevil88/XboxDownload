# Changelog

All notable changes to XboxFastz are documented here.

## XboxFastz v1.2.0 — Smart Endpoint Selection

### Added

- Added `EndpointSelectorService` for automatic, low-bandwidth endpoint/IP selection that does not assume the upstream author's preferred IP is optimal for every user.
- Selection uses a cheap-to-expensive funnel: ICMP reachability/latency → small HTTP latency probes (packet-loss estimation) → small 4 MB ranged speed test on the most promising candidates → rank by speed (tie-break by latency) → automatic selection.
- Wired the smart selector into the existing "Fastest Akamai IP" auto-feature, with the legacy race-based selector retained as a fallback.

### Performance

- Cheap connectivity/latency checks run before any speed test; the speed-test stage downloads at most 4 MB per finalist instead of the 30/50 MB used by the Speed Test tab.
- Concurrent ICMP stage with a hard 3-second cap and early stop, minimizing wasted probing.

### Preserved

- Existing Speed Test tab workflow and per-IP full speed test are unchanged.
- Network Diagnostics from v1.1.0, XboxFastz branding, and original author attribution/donation information are preserved.

## XboxFastz v1.1.1 — Maintenance Update

### Fixed

- Verified the in-app update checker points to the XboxFastZ releases (`https://github.com/DreamOpenS/XboxFastZ`) instead of the original XboxDownload repository.
- Version/release checking now resolves the latest tag from XboxFastZ releases.
- The Help → Download menu and the About dialog project link continue to point to XboxFastZ.

### Preserved

- Network Diagnostics introduced in v1.1.0.
- Improved English README and XboxFastz branding/rebranding.
- Original author attribution and crypto donation information.

## XboxFastz v1.1.0 — 2026-08-21

### Development Task

Added network/download diagnostics and safe performance improvements while preserving the existing UI and networking architecture.

### Added

- Added network diagnostics for a selected download endpoint.
- Added DNS resolution, selected-IP reachability, latency, packet-loss, duration, and success/failure reporting.
- Added cancellation, clipboard copy, and text export for diagnostic results.

### Performance

- Reused one DNS resolution and one selected address for each diagnostic run.
- Reused the existing HTTP latency path and disposed each response after measurement.
- Canceled the three-attempt diagnostic operation through a linked cancellation token.

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
