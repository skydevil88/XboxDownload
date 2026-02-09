## ⚡ First-Time Run Instructions (macOS Special Notes)

macOS enables Gatekeeper by default, which may block unsigned scripts or binaries.
For the first-time run, please follow these steps:
1. Open Terminal.
2. Remove the quarantine attribute (only needs to be done once; afterward, macOS will no longer block the program):
```bash
sudo xattr -dr com.apple.quarantine /Users/{username}/Downloads/XboxDownload-macos-arm64
```
3. Double-click to run the startup script run_xboxdownload.command。


## ⚡ Linux First-Time Run Instructions
Method 1: Run via File Manager (Recommended)

Locate the startup script run_xboxdownload.sh.

Right-click the file and select Properties.

Enable “Allow executing file as program”.

Close the dialog.

Right-click the script and choose Run as Program.

On some desktop environments (such as GNOME or KDE), you can also double-click the script and select Run.



//////////////////////////////////////////////////////////////////////


## ⚡ 首次运行条件（macOS 特殊说明）

macOS 默认启用了 Gatekeeper，会阻止未签名的脚本或二进制运行。  
首次运行时，请按照以下步骤操作：
1. 打开终端（Terminal）。
2. 移除隔离标签（quarantine）：只需执行一次，之后系统将不会再阻止运行
```bash
sudo xattr -dr com.apple.quarantine /Users/{username}/Downloads/XboxDownload-macos-arm64
```
3. 双击运行启动脚本 run_xboxdownload.command。


## ⚡ Linux 首次运行说明
方法一：文件管理器右键运行（推荐）

找到启动脚本 run_xboxdownload.sh。

右键 → 属性（Properties）。

勾选 “允许作为程序执行” / “Allow executing file as program”。

关闭窗口。

右键脚本 → 以程序方式运行（Run as Program）。

在部分桌面环境（如 GNOME、KDE）中，也可以直接双击并选择 运行。

## Linux 使用本地代理服务
https://github.com/skydevil88/XboxDownload/discussions/128
