# XboxFastz

XboxFastz is an independent desktop toolkit for obtaining Microsoft Store game download links and improving download workflows for Xbox and PC users. It is based on [XboxDownload by skydevil88](https://github.com/skydevil88/XboxDownload), with the original project credited and its technical foundation preserved.

> XboxFastz is an independent fork based on XboxDownload by skydevil88. It is not affiliated with Microsoft or the original author.

Target Framework: .NET 10.0, Avalonia 12

## What it does

XboxFastz provides local DNS and HTTP(S) listening services, download-link inspection, CDN/IP speed testing, Hosts management, local upload, and Xbox storage tools. It can assist downloads for Xbox, Microsoft Store, PlayStation, Nintendo Switch, EA, Battle.net, Epic, Ubisoft, Riot Games, and Rockstar Games.

## Features

- Microsoft Store game and application link lookup.
- Local DNS and HTTP(S) services for console and PC download workflows.
- CDN/IP testing with location search, imported IP lists, and custom mappings.
- Hosts editing, domain resolution, and network diagnostics.
- PC download and local upload back to Xbox.
- Xbox external-drive PC/Xbox mode conversion tools.
- Windows, macOS, and Linux builds; Windows includes Microsoft Store installation tools.

## Supported platforms

Self-contained packages are published for Windows x64/ARM64, macOS x64/Apple Silicon, and Linux x64/ARM64. Download the appropriate archive from [XboxFastz Releases](https://github.com/DreamOpenS/XboxFastZ/releases). No separate .NET runtime is required for release packages.

For macOS and Linux first-run steps, see [Scripts/README.md](XboxDownload/Scripts/README.md).

## Basic Xbox setup

Pause the current Xbox download before changing settings.

1. Open **Speed Test**, import the IP list, test nearby addresses, and choose a suitable IP.
2. Open **Services**, choose the PC IP on the same LAN as the Xbox, enable **DNS Service** and **HTTP(S) Service** as needed, then select **Start Listening**.
3. On Xbox, open **Settings > General > Network settings > Advanced settings > DNS settings > Manual**. Set the primary DNS to the PC IP shown by XboxFastz and leave the secondary DNS empty. PC Xbox App users can skip this step.
4. Restore Xbox DNS to automatic after downloading. If Xbox uses IPv6, disable IPv6 on the router while using this setup.

For a PC Wi-Fi hotspot, select **Any IP**, disable the DNS service, and disable **Set Local DNS**.

## PC download functionality

Enable **Show Xbox Game Download Links**, pause the console download, and copy the displayed URL. Use a PC download tool to retrieve the files. The same workflow can be used with PC Xbox Game Pass, PlayStation, and EA downloads. Microsoft Store games downloaded on PC can be installed from **Tools > Install Microsoft Store Games and Apps**.

The **Local Upload** workflow points XboxFastz at a PC download folder and uploads the files as the console requests them. External-drive import requires converting the drive to PC mode, placing the files on it under the Content ID, and converting it back to Xbox mode. Do not launch games directly from the external drive; the console may download them again. Application imports are not supported by the drive workflow.

## Network, DNS, and CDN functionality

XboxFastz can listen for DNS and HTTP(S) requests, use configurable upstream DNS servers, map download domains, edit Hosts entries, resolve domains, and test CDN/IP endpoints. IP availability and CDN cache behavior vary by region, carrier, and title; use local test results rather than copying an address blindly.

If the PC should not remain on during downloads, configure URL rewriting on an OpenWrt router with Lighttpd, Nginx, or Caddy. See [README_OpenWrt.md](README_OpenWrt.md) for the router guide. IPv6, DNS caching, firewall rules, and router configuration can affect the result.

## Troubleshooting

- No log entries: verify that the listening IP is on the same network as the console, then check the OS firewall and security software.
- Port conflict: allow the application to stop the conflicting process, or identify it with `netstat -an`.
- Network access lost after exit: use the DNS repair action or restore the system DNS to automatic.
- Slow or inconsistent downloads: retest the relevant CDN/IP for the current region and title; results are not permanent guarantees.

## Important warnings

The application can change DNS, Hosts, certificates, ports, and local network listeners. Review each setting, run with the required administrator/root privileges only, and restore console and system settings after use. XboxFastz is not an official Microsoft, Xbox, Sony, Nintendo, EA, Valve, Ubisoft, or other platform-vendor product.

## Documentation

- [中文文档](README-zh-CN.md)
- [OpenWrt guide](README_OpenWrt.md)
- [Contributing](CONTRIBUTING.md)
- [Changelog](CHANGELOG.md)

## Attribution and license

XboxFastz is an independent fork based on [XboxDownload by skydevil88](https://github.com/skydevil88/XboxDownload). The upstream project remains the source of the original implementation and attribution is intentionally preserved.

This checkout contains no standalone `LICENSE` file, so XboxFastz does not invent or replace the upstream license terms. Before redistributing or modifying the project, review the current upstream repository for its license and copyright requirements, and retain the original attribution.
