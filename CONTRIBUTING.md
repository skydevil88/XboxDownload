# Contributing to XboxFastz

Thank you for helping maintain XboxFastz, an independent fork based on [skydevil88/XboxDownload](https://github.com/skydevil88/XboxDownload).

## Before opening an issue

- Search existing issues and confirm the problem is reproducible on the latest release.
- Include the OS, CPU architecture, Xbox or PC setup, app version, language, and exact steps.
- Remove account identifiers, access tokens, public IP addresses, and private network details from logs.
- For DNS, CDN, proxy, or download issues, include the affected hostname and region when it is safe to do so.

## Development

1. Install the .NET 10 SDK.
2. Restore and build:
   ```powershell
   dotnet restore .\XboxDownload\XboxDownload.csproj
   dotnet build .\XboxDownload\XboxDownload.csproj
   ```
3. Keep changes focused and avoid changing DNS, proxy, CDN, HTTP(S), storage, or download behavior unless the change is explicitly required.
4. Preserve the original attribution and existing technical compatibility identifiers.

## Pull requests

Explain the user-visible behavior, the files changed, and how you tested it. Update the relevant English and Chinese documentation for user-facing changes. Do not add dependencies without a clear need.
