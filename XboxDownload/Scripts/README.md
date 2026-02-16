# 🚀 Getting Started

This section explains how to run **XboxDownload** for the first time on
macOS and Linux.

---

## 🍎 macOS First-Time Setup

> ⚠️ **Important**\
> macOS enables Gatekeeper by default. Unsigned applications or scripts
> may be blocked on first launch.

### Step 1 --- Open Terminal
Press `Command + Space`, type **Terminal**, and open it.
### Step 2 --- Prepare and Launch (One-Time Setup)

Run the following:

```bash
cd /path/to/XboxDownload
sudo xattr -dr com.apple.quarantine run_xboxdownload.command XboxDownload
chmod +x run_xboxdownload.command XboxDownload
./run_xboxdownload.command
```

Replace `/path/to/XboxDownload` with the actual path to your
XboxDownload directory.

> ✅ This only needs to be done once after downloading.

After the first successful run, you can simply double-click:
-   `run_xboxdownload.sh` — Runs with root privileges
-   `XboxDownload` — Runs with normal user privileges

> 💡 Most features are available when running with normal privileges.\
> Some advanced features (such as port listening or modifying system
> network settings) may require root privileges.\
> If the application cannot run properly under a regular user\
> (e.g., it fails with the error Failed to commit extracted files to directory [/Users/devil/.net/XboxDownload/...]),\
> please run the following command in the terminal to fix the permissions:


```bash
sudo chown -R $(whoami):staff ~/.net/XboxDownload && chmod -R u+rwX ~/.net/XboxDownload
```

---

## 🐧 Linux First-Time Setup

### Method 1 — Run via Terminal

```bash
cd /path/to/XboxDownload
chmod +x run_xboxdownload.sh XboxDownload
./run_xboxdownload.sh
```

Replace `/path/to/XboxDownload` with the actual path to your
XboxDownload directory.

---

### Method 2 — Run via File Manager

1.  Open the **XboxDownload** directory.
2.  Locate `run_xboxdownload.sh` or `XboxDownload`.
3.  Right-click → **Properties**.
4.  Enable **Allow executing file as program**.
5.  Close the dialog.
6.  Right-click one of the following files and choose **Run as Program**:
    -   `run_xboxdownload.sh` — Runs with root privileges
    -   `XboxDownload` — Runs with normal user privileges
> 💡 Most features are available when running with normal privileges.\
> Some advanced features (such as port listening or modifying system
> network settings) may require root privileges.

On GNOME, KDE, and some other desktop environments, you can also
double-click the file and select **Run**.

---

# 🚀 使用说明

本章节说明 **XboxDownload** 在 macOS 与 Linux 下的首次运行步骤。

---

## 🍎 macOS 首次运行说明

> ⚠️ **重要说明**\
> macOS 默认启用了 Gatekeeper，可能会阻止未签名的程序或脚本运行。

### 步骤 1 — 打开终端

按 `Command + Space`，输入 **Terminal** 并打开。

### 步骤 2 — 初始化并运行（仅首次需要）

执行以下命令：

```bash
cd /path/to/XboxDownload
sudo xattr -dr com.apple.quarantine run_xboxdownload.command XboxDownload
chmod +x run_xboxdownload.command XboxDownload
./run_xboxdownload.command
```

将 `/path/to/XboxDownload` 替换为程序所在目录。

> ✅ 该操作仅在首次下载后需要执行一次

首次成功运行后，今后可直接双击：
-   `run_xboxdownload.command` — 以 root 权限运行
-   `XboxDownload` — 以普通用户权限运行
> 💡 在普通用户权限下可以使用大部分功能。\
> 某些高级功能（例如监听端口或修改系统网络设置）可能需要以 root 权限运行。\
> 如果应用程序在普通用户权限下无法正常运行\
> （例如报错：Failed to commit extracted files to directory [/Users/devil/.net/XboxDownload/...]），\
> 请在终端执行以下命令修复权限：

```bash
sudo chown -R $(whoami):staff ~/.net/XboxDownload && chmod -R u+rwX ~/.net/XboxDownload
```

---

## 🐧 Linux 首次运行说明

### 方法 1 — 终端运行

```bash
cd /path/to/XboxDownload
chmod +x run_xboxdownload.sh XboxDownload
./run_xboxdownload.sh
```

将 `/path/to/XboxDownload` 替换为程序所在目录。

---

### 方法 2 — 文件管理器运行

1.  打开 **XboxDownload** 目录。
2.  找到 `run_xboxdownload.sh` 或 `XboxDownload`。
3.  右键 → **属性**。
4.  勾选 **允许作为程序执行**。
5.  关闭窗口。
6.  右键以下任意文件之一并选择 **以程序方式运行**：
    -   `run_xboxdownload.sh` — 以 root 权限运行
    -   `XboxDownload` — 以普通用户权限运行
> 💡 在普通用户权限下可以使用大部分功能。\
> 某些高级功能（例如监听端口或修改系统网络设置）可能需要以 root 权限运行。

在 GNOME、KDE 等桌面环境中，也可以直接双击并选择 **运行**。
