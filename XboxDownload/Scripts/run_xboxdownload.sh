#!/usr/bin/env bash
# Launch XboxDownload with sudo, selecting executable based on current OS

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
APP=""

# Detect OS
OS_TYPE="$(uname)"
case "$OS_TYPE" in
    Linux*)   PLATFORM="linux" ;;
    Darwin*)  PLATFORM="macos" ;;
    *)        echo "❌ Unsupported OS: $OS_TYPE"; exit 1 ;;
esac

# Determine architecture
ARCH_TYPE="$(uname -m)"
case "$ARCH_TYPE" in
    x86_64) ARCH="x64" ;;
    aarch64|arm64) ARCH="arm64" ;;
    *)       echo "❌ Unsupported architecture: $ARCH_TYPE"; exit 1 ;;
esac

# Possible paths
POSSIBLE_PATHS=(
    "./XboxDownload"
    "./Release/XboxDownload-$PLATFORM-$ARCH/XboxDownload"
)

# Find existing executable
for path in "${POSSIBLE_PATHS[@]}"; do
    FULL_PATH="$SCRIPT_DIR/$path"
    if [ -f "$FULL_PATH" ]; then
        # Normalize path
        APP="$(realpath "$FULL_PATH")"
        break
    fi
done

# If not found, exit
if [ -z "$APP" ]; then
    echo "❌ XboxDownload executable not found for $PLATFORM-$ARCH."
    exit 1
fi

echo "Launching XboxDownload from: $APP"

# Start with sudo
exec sudo "$APP" "$@"

