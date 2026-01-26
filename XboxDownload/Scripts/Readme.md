## ⚡ First-Time Run Instructions (macOS Special Notes)

macOS enables Gatekeeper by default, which may block unsigned scripts or binaries.
For the first-time run, please follow these steps:
1. Open Terminal.
2. Remove the quarantine attribute (only needs to be done once; afterward, macOS will no longer block the program):
```bash
sudo xattr -dr com.apple.quarantine /Users/username/Downloads/XboxDownload-macos-arm64
```
3. Double-click to run the startup script run_xboxdownload.command。


## ⚡ 首次运行条件（macOS 特殊说明）

macOS 默认启用了 Gatekeeper，会阻止未签名的脚本或二进制运行。  
首次运行时，请按照以下步骤操作：
1. 打开终端（Terminal）。
2. 移除隔离标签（quarantine）：只需执行一次，之后系统将不会再阻止运行
```bash
sudo xattr -dr com.apple.quarantine /Users/username/Downloads/XboxDownload-macos-arm64
```
3. 双击运行启动脚本 run_xboxdownload.command。