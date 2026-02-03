#!/usr/bin/env bash
# ============================================
# XboxDownload Launcher (macOS / Linux)
# ============================================

set -e

# Resolve script directory (macOS-safe)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

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

echo "🚀 Launching XboxDownload"

if [[ "$PLATFORM" == "macos" ]]; then
if xattr "$APP" 2>/dev/null | grep -q com.apple.quarantine; then
echo "🛡 Removing macOS quarantine attribute..."
sudo xattr -dr com.apple.quarantine "$(dirname "$APP")"
fi
fi

echo "   Path: $APP"
echo "   OS:   $PLATFORM"
echo "   Arch: $ARCH"
echo ""

# ----------------------------
# Run with sudo (required for DNS / hosts / ports)
# ----------------------------
exec sudo "$APP" "$@"