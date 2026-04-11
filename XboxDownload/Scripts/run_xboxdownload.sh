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
echo "   Path: $APP"
echo "   OS:   $PLATFORM"
echo "   Arch: $ARCH"
echo ""

if [[ "$PLATFORM" == "macos" ]]; then
    TARGET_UID=$(id -u)
    TARGET_GID=$(dscl . -read /Groups/staff PrimaryGroupID 2>/dev/null | awk '{print $2}')
    : "${TARGET_GID:=20}"
    CURRENT_USER=$(id -un)

    fix_if_broken() {
        local dir="$1"
        # Exit if directory does not exist
        [ ! -d "$dir" ] && return
    
        # 1. Initialize environment context
        local uid=${TARGET_UID:-$(id -u)}
        local gid=${TARGET_GID:-20}
        local user=${CURRENT_USER:-$(id -un)}
        local target="$uid:$gid"
    
        # 2. Retrieve metadata (single system call for performance)
        local current_owner
        current_owner=$(stat -f "%u:%g" "$dir" 2>/dev/null)
    
        # 3. Fast Path: If owner matches AND directory is writable, return immediately
        if [[ "$current_owner" == "$target" && -w "$dir" ]]; then
            return
        fi
    
        # 4. Corrective actions required
        echo "Fixing permissions for $dir..."
    
        # 5. Optional diagnostic warning
        if [ -z "$current_owner" ]; then
            echo "Warning: unable to determine owner for $dir" >&2
        fi
    
        # 6. Fix ownership only if it doesn't match the target
        if [[ -n "$current_owner" && "$current_owner" != "$target" ]]; then
            sudo chown -R "$user":staff "$dir"
        fi
    
        # 7. Fix permissions (try non-sudo first, fallback to sudo if denied)
        # u+rwX ensures directories are traversable and files are readable/writable
        chmod -R u+rwX "$dir" 2>/dev/null || \
        sudo chmod -R u+rwX "$dir"
    }

    APP_DIR="$HOME/.net/XboxDownload"
    NET_DIR="$HOME/.net"

    # ✅ Ensure parent directory ownership and 'execute' permissions first
    if [ -d "$NET_DIR" ]; then
        CURRENT_OWNER=$(stat -f '%u:%g' "$NET_DIR")

        if [[ "$CURRENT_OWNER" != "$TARGET_UID:$TARGET_GID" ]]; then
            sudo chown "$CURRENT_USER":staff "$NET_DIR"
        fi

        if [ ! -x "$NET_DIR" ]; then
            chmod u+x "$NET_DIR" 2>/dev/null || sudo chmod u+x "$NET_DIR"
        fi
    fi

    # ✅ Process child directories before parent directories (Bottom-up approach)
    fix_if_broken "$APP_DIR"
    fix_if_broken "$NET_DIR"

    # ✅ Handle macOS quarantine attributes
    if [ -d "$APP_DIR" ]; then
        if xattr -p com.apple.quarantine "$APP_DIR" >/dev/null 2>&1; then
            echo "🛡 Removing macOS quarantine attribute..."
            xattr -dr com.apple.quarantine "$APP_DIR" 2>/dev/null || \
            sudo xattr -dr com.apple.quarantine "$APP_DIR"
        fi
    fi
fi

# ----------------------------
# Run with sudo (required for DNS / hosts / ports)
# ----------------------------
#exec sudo "$APP" "$@"
exec sudo nohup "$APP" "$@" >/dev/null
