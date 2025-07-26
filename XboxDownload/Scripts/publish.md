
# 📦 Xbox下载助手  发布说明

Xbox下载助手 支持将项目发布为各平台的**独立可执行文件**，无需目标系统安装 .NET 运行时。

---

## ✅ 支持平台

| 平台         | 目标 RID                      | 说明                          |
|------------|-----------------------------|-----------------------------|
| 🪟 Windows | `win-x64` / `win-arm64`     | 适用于常规和 ARM 架构的 Windows      |
| 🍎 macOS   | `osx-x64` / `osx-arm64`     | 支持 Intel 与 Apple Silicon 芯片 |
| 🐧 Linux   | `linux-x64` / `linux-arm64` | 支持主流桌面 Linux 发行版            |

---

## 🪟 Windows 系统发布指南

1. 打开 PowerShell 或 CMD
2. 执行脚本：
   ```bat
   Scripts\publish-win.bat
   ```

---

## 🍎 macOS / 🐧 Linux 系统发布指南

1. 首次使用需授权执行权限：
   ```bash
   chmod +x Scripts/publish.sh
   ```

2. 运行脚本并选择发布目标：
   ```bash
   ./Scripts/publish.sh
   ```

---

## 📁 发布输出结构

所有构建输出统一位于 `Scripts/Release/` 目录下，结构如下：

```
Scripts/
├── Release/
│   ├── XboxDownload-windows-x64/
│   ├── XboxDownload-windows-arm64/
│   ├── XboxDownload-macos-x64/
│   ├── XboxDownload-macos-arm64/
│   ├── XboxDownload-linux-x64/
│   └── XboxDownload-linux-arm64/
```

每个子目录下为该平台的完整自包含可执行文件。

---

## ⚙️ 构建特性

- ✅ **自包含（Self-contained）**：无需安装 .NET 运行时；
- ✅ **单文件发布**：封装为一个可执行文件；
- ✅ **支持多平台跨编译**；
- ✅ **调试信息与符号移除**，构建更轻量；
- ✅ **构建脚本位于 `Scripts/` 目录**，包括：
    - `publish.sh`（macOS/Linux Bash）
    - `publish-win.bat`（Windows 批处理）
    - `publish-win.ps1`（PowerShell 脚本）

---

## 📌 注意事项

- 如构建失败，请先执行：
  ```bash
  dotnet restore
  ```
- macOS 上执行 arm64 构建请确认未开启 Rosetta 模拟。
- 若在 macOS 上运行 `osx-arm64` 版本失败但 `osx-x64` 正常，建议使用 `osx-x64` 兼容构建。

---
