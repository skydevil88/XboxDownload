#!/usr/bin/env bash
# ============================================
# XboxDownload Launcher (macOS / Linux)
# ============================================

set -e

# ----------------------------
# Resolve script path / directory
# ----------------------------
SCRIPT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/$(basename "${BASH_SOURCE[0]}")"
SCRIPT_DIR="$(cd "$(dirname "$SCRIPT_PATH")" && pwd)"

# ----------------------------
# Detect OS
# ----------------------------
case "$(uname)" in
    Darwin) PLATFORM="macos" ;;
    Linux)  PLATFORM="linux" ;;
    *)
        echo "❌ Unsupported OS: $(uname)"
        exit 1
        ;;
esac

# ----------------------------
# Detect Architecture
# ----------------------------
case "$(uname -m)" in
    x86_64)        ARCH="x64" ;;
    arm64|aarch64) ARCH="arm64" ;;
    *)
        echo "❌ Unsupported architecture: $(uname -m)"
        exit 1
        ;;
esac

# ----------------------------
# Candidate executable paths
# ----------------------------
CANDIDATES=(
    "$SCRIPT_DIR/XboxDownload"
    "$SCRIPT_DIR/Release/XboxDownload-$PLATFORM-$ARCH/XboxDownload"
)

APP=""

for file in "${CANDIDATES[@]}"; do
    if [ -f "$file" ]; then
        APP="$file"
        break
    fi
done

# ----------------------------
# Validation
# ----------------------------
if [ -z "$APP" ]; then
    echo "❌ XboxDownload executable not found"
    echo "   Platform: $PLATFORM"
    echo "   Arch:     $ARCH"
    echo "   Searched paths:"
    for f in "${CANDIDATES[@]}"; do
        echo "     - $f"
    done
    exit 1
fi

# Ensure executable bit
chmod +x "$APP"

# ----------------------------
# Print banner once
# ----------------------------
if [ -z "${LAUNCHER_ALREADY_PRINTED:-}" ]; then
    echo "🚀 Launching XboxDownload"
    echo "   Path: $APP"
    echo "   OS:   $PLATFORM"
    echo "   Arch: $ARCH"
    echo ""
fi

# ----------------------------
# Acquire sudo once, then re-exec as root
# ----------------------------
if [ "$(id -u)" -ne 0 ]; then
    sudo -v
    exec sudo LAUNCHER_ALREADY_PRINTED=1 /usr/bin/env bash "$SCRIPT_PATH" "$@"
fi

# ----------------------------
# macOS permission repair
# ----------------------------
if [[ "$PLATFORM" == "macos" ]]; then
    REAL_USER="${SUDO_USER:-$(logname 2>/dev/null || id -un)}"
    REAL_GROUP="$(id -gn "$REAL_USER")"
    TARGET_UID="$(id -u "$REAL_USER")"
    TARGET_GID="$(id -g "$REAL_USER")"

    REAL_HOME="$(dscl . -read "/Users/$REAL_USER" NFSHomeDirectory 2>/dev/null | awk '{print $2}')"
    : "${REAL_HOME:=/Users/$REAL_USER}"

    APP_DIR="$REAL_HOME/.net/XboxDownload"
    NET_DIR="$REAL_HOME/.net"

    fix_if_broken() {
        local dir="$1"
        [ -d "$dir" ] || return 0

        local current_owner
        current_owner="$(stat -f "%u:%g" "$dir" 2>/dev/null || true)"

        if [[ "$current_owner" == "$TARGET_UID:$TARGET_GID" && -r "$dir" && -w "$dir" && -x "$dir" ]]; then
            return 0
        fi

        echo "🔧 Fixing permissions for $dir..."
        chown -R "$REAL_USER:$REAL_GROUP" "$dir"
        chmod -R u+rwX "$dir"
    }

    # Fix child first, then parent
    fix_if_broken "$APP_DIR"
    fix_if_broken "$NET_DIR"

    # Remove quarantine from executable if needed
    if xattr -p com.apple.quarantine "$APP" >/dev/null 2>&1; then
        echo "🛡 Removing macOS quarantine attribute..."
        xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true
    fi
fi

# ----------------------------
# Run with root (required for DNS / hosts / ports)
# ----------------------------
exec nohup "$APP" "$@" >/dev/null 2>&1