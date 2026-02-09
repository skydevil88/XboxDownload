#!/bin/bash
# XboxDownload Cross-Platform Publish Script
# Supports self-contained single-file publishing for Windows/macOS/Linux
# Only "Publish for Current System" supports x86
# Debug symbols are disabled for smaller output size.

set -e

# -------------------------------
# Paths
# -------------------------------
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_FILE="$SCRIPT_DIR/../XboxDownload.csproj"
OUTPUT_ROOT="$SCRIPT_DIR/Release"

# -------------------------------
# Shared dotnet publish args
# -------------------------------
COMMON_ARGS=(
    -c Release
    /p:PublishSingleFile=true
    /p:IncludeNativeLibrariesForSelfExtract=true
    --self-contained true
    /p:PublishTrimmed=false
    /p:DebugType=none
    /p:DebugSymbols=false
)

# -------------------------------
# Menu
# -------------------------------
print_menu() {
    echo "========================================="
    echo "       XboxDownload - Publish Tool       "
    echo "========================================="
    echo
    echo "Select target to publish:"
    echo
    echo " 1) Publish for Current System (x64 / arm64 / x86, Default)"
    echo " 2) Publish for Windows (x64 + arm64)"
    echo " 3) Publish for macOS   (x64 + arm64)"
    echo " 4) Publish for Linux   (x64 + arm64)"
    echo " 5) Publish All (Windows, macOS, Linux)"
    echo " 6) Exit"
    echo
}

# -------------------------------
# Publish target
# -------------------------------
publish_target() {
    local rid="$1"
    local output_rid="$rid"

    if [[ "$rid" == win-* ]]; then
        output_rid="windows-${rid#win-}"
    elif [[ "$rid" == osx-* ]]; then
        output_rid="macos-${rid#osx-}"
    elif [[ "$rid" == linux-* ]]; then
        output_rid="linux-${rid#linux-}"
    fi

    local output_dir="$OUTPUT_ROOT/XboxDownload-$output_rid"
    mkdir -p "$output_dir"

    echo
    echo "Publishing for $rid -> $output_dir"

    if ! dotnet publish "$PROJECT_FILE" -r "$rid" -o "$output_dir" "${COMMON_ARGS[@]}"; then
        echo "[ERROR] Failed to publish for $rid"
        return 1
    fi

    echo "[OK] Success: $output_dir"

    # -------------------------------
    # Copy extra files
    # -------------------------------
    local files=("Readme.md" "run_xboxdownload.sh")
    for f in "${files[@]}"; do
        local src="$SCRIPT_DIR/$f"
        if [[ -f "$src" ]]; then
            if [[ "$rid" == osx* ]]; then
                local dest_name="$f"
                if [[ "$f" == "run_xboxdownload.sh" ]]; then
                    dest_name="run_xboxdownload.command"
                fi
                cp -f "$src" "$output_dir/$dest_name"
            elif [[ "$rid" == linux* ]]; then
                cp -f "$src" "$output_dir/"
            fi
        else
            echo "[WARN] File not found: $src"
        fi
    done

    # -------------------------------
    # Create ZIP
    # -------------------------------
    local zip_file="$output_dir.zip"
    if [[ -f "$zip_file" ]]; then
        rm -f "$zip_file"
    fi
    echo "Creating ZIP: $zip_file"
    (cd "$OUTPUT_ROOT" && zip -r "$(basename "$zip_file")" "XboxDownload-$output_rid" > /dev/null)
    echo "[OK] ZIP created: $zip_file"
}

# -------------------------------
# Publish current system
# -------------------------------
publish_current() {
    local os arch rid
    os="$(uname -s)"
    arch="$(uname -m)"

    case "$os" in
        Linux)
            case "$arch" in
                x86_64) rid="linux-x64" ;;
                aarch64|arm64) rid="linux-arm64" ;;
                i686|i386) rid="linux-x86" ;;
                *) echo "[ERROR] Unsupported Linux architecture: $arch"; return 1 ;;
            esac
            ;;
        Darwin)
            case "$arch" in
                x86_64) rid="osx-x64" ;;
                arm64)  rid="osx-arm64" ;;
                *) echo "[ERROR] Unsupported macOS architecture: $arch"; return 1 ;;
            esac
            ;;
        MINGW*|MSYS*|CYGWIN*)
            case "$arch" in
                x86_64) rid="win-x64" ;;
                aarch64|arm64) rid="win-arm64" ;;
                i686|i386) rid="win-x86" ;;
                *) echo "[ERROR] Unsupported Windows architecture: $arch"; return 1 ;;
            esac
            ;;
        *)
            echo "[ERROR] Unsupported OS: $os"
            return 1
            ;;
    esac

    echo "Detected system: $os / $arch"
    echo "Using RID: $rid"
    publish_target "$rid"
}

publish_windows() {
    publish_target "win-x64"
    publish_target "win-arm64"
}

publish_macos() {
    publish_target "osx-x64"
    publish_target "osx-arm64"
}

publish_linux() {
    publish_target "linux-x64"
    publish_target "linux-arm64"
}

# -------------------------------
# Main loop
# -------------------------------
while true; do
    print_menu
    read -rp "Enter your choice [1-6] (Default: 1): " choice
    choice="${choice:-1}"

    case "$choice" in
        1) publish_current ;;
        2) publish_windows ;;
        3) publish_macos ;;
        4) publish_linux ;;
        5) publish_windows; publish_macos; publish_linux ;;
        6) echo "Exiting..."; exit 0 ;;
        *) echo "Invalid choice. Please enter 1-6." ;;
    esac

    echo
    read -rp "Press Enter to return to menu..." _
done
