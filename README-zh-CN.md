# XboxFastz

XboxFastz 是基于 [skydevil88/XboxDownload](https://github.com/skydevil88/XboxDownload) 的独立分支。它用于获取微软商店游戏下载链接，并为 Xbox、PC Microsoft Store、PlayStation、Nintendo Switch、EA、Battle.net、Epic、Ubisoft、Riot Games 和 Rockstar Games 等下载提供网络、CDN 与存储工具。

> 本项目保留原作者 skydevil88 的致谢。XboxFastz 不代表原项目，也不是微软官方产品。

Target Framework: .NET 10.0, Avalonia 12

## 主要功能

- 获取微软商店游戏和应用的下载链接。
- 通过本地 DNS 与 HTTP(S) 监听服务，为主机或 PC 下载提供域名解析和链接转发。
- 测试附近 CDN/IP 的连通性和速度，并导入自定义 IP。
- 支持 Hosts 管理、CDN 域名测试、本地上传和 Xbox 外置硬盘模式转换。
- 支持 Windows、macOS 和 Linux；Windows 提供额外的微软商店游戏安装工具。

网页版商店：<https://xbox.skydevil.xyz>

## 安装与运行

从 [XboxFastz Releases](https://github.com/DreamOpenS/XboxFastZ/releases) 下载对应平台的自包含压缩包，无需另外安装 .NET 运行时。

Windows 用户解压后直接运行程序。macOS 和 Linux 用户可参考 [Scripts/README.md](XboxDownload/Scripts/README.md)，部分监听端口、修改 DNS 或 Hosts 的功能需要 root/管理员权限。

## Xbox 主机设置

1. Xbox 正在下载时先暂停下载。
2. 启动 XboxFastz，在“测速”中导入 IP，测试并选择附近速度较快的 IP。
3. 在“服务”中选择与 Xbox 同一网段的本机 IP，按需启用“DNS 服务”和“HTTP(S) 服务”，点击“开始监听”。
4. Xbox 进入“设置 > 常规 > 网络设置 > 高级设置 > DNS 设置 > 手动”，将主 DNS 设置为电脑 IP，辅助 DNS 留空。PC Xbox App 用户不需要此步骤。
5. 下载完成后将 Xbox DNS 恢复为自动获取，否则关闭 XboxFastz 后可能无法联网。

如果 Xbox 使用 IPv6，请在路由器中关闭 IPv6。使用电脑 Wi-Fi 热点时，监听 IP 选择“任意 IP”，关闭 DNS 服务和“设置本机 DNS”。

## PC 下载与回传

勾选“显示 Xbox 游戏下载链接”，暂停主机下载后右键复制链接，再使用 PC 下载工具下载。PC Xbox App、PlayStation 和 EA 下载也可以使用相同方式获取链接。

PC Xbox App 游戏下载完成后，可在“工具 > 安装微软商店游戏和应用”中安装。

也可以使用“本地上传”：将本地上传文件夹设置为 PC 下载目录，启用本地上传并开始监听，然后让 Xbox 重新下载，XboxFastz 会从 PC 上传文件。

外置硬盘导入需要先将硬盘转换为 PC 模式，把游戏文件放入硬盘并重命名为 Content ID，再转换回 Xbox 模式。此方法可能需要暂时关闭杀毒软件；不要直接从外置硬盘启动游戏，否则主机可能重新下载。应用不支持从硬盘直接导入应用。

## DNS、Hosts 与 CDN

XboxFastz 可以监听 DNS 和 HTTP(S) 请求，支持自定义上游 DNS、域名映射、Hosts 编辑、CDN/IP 测速以及下载链接解析。不同地区和不同域名的可用 IP 可能不同，请使用本地测速结果，不要盲目套用示例 IP。

不想让电脑持续运行时，可以在 OpenWrt 上使用 Lighttpd、Nginx 或 Caddy 做 URL 重写，将 `assets*.xboxlive.com` 等域名跳转到对应的 `.cn` CDN。完整路由器配置见 [README_OpenWrt.md](README_OpenWrt.md)。此方案同样需要注意 IPv6 与 DNS 缓存问题。

## 故障排查与警告

- 没有日志：先确认监听 IP 与 Xbox 在同一网段，再检查系统防火墙、网络管家和安全软件。
- 端口被占用：可先允许程序强制结束占用进程；也可用 `netstat -an` 查找占用者。
- 关闭程序后无法联网：在服务页面使用 DNS 修复，或将系统 DNS 恢复为自动获取。
- 下载域名、CDN 缓存和 IP 会随地区、运营商及游戏变化；测速结果不是永久保证。
- 该工具会修改本机或主机的 DNS、Hosts、证书、端口或网络监听设置。请理解每项设置，下载完成后恢复原配置。
- XboxFastz 与 Xbox、Microsoft、Sony、Nintendo、EA、Valve、Ubisoft 或其他平台厂商没有隶属关系。

## 贡献

请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。提交问题时请说明系统、架构、应用版本、语言、网络拓扑和可复现步骤，不要公开账号、令牌或个人网络信息。

## 致谢与许可证

XboxFastz is an independent fork based on XboxDownload by skydevil88. 原项目地址：[skydevil88/XboxDownload](https://github.com/skydevil88/XboxDownload)。本仓库保留原项目的来源信息和技术命名。

当前代码快照中没有单独的 `LICENSE` 文件，因此本分支不擅自声明新的许可证；使用、分发或修改前请同时查看原项目仓库中的最新许可证和版权要求，并继续保留原作者致谢。
