#!/bin/bash
# XboxDownload Cross-Platform Publish Script
# Supports self-contained single-file publishing for Windows/macOS/Linux
# Debug symbols are disabled for smaller output size.

set -e

# Get the directory of the current script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# Define project root (assumed to be one level up from script directory)
PROJECT_ROOT="$SCRIPT_DIR/.."
# Define output directory for published files
OUTPUT_ROOT="$PROJECT_ROOT/Scripts/Release"

# Common dotnet publish arguments
COMMON_ARGS=(
    -c Release                             # Build configuration: Release
    /p:PublishSingleFile=true              # Publish as a single executable file
    /p:IncludeNativeLibrariesForSelfExtract=true  # Include native libs for self-extraction
    --self-contained true                  # Publish self-contained (includes .NET runtime)
    /p:PublishTrimmed=false                # Disable trimming (optional)
    /p:DebugType=none                      # Disable debug info generation (no PDB files)
    /p:DebugSymbols=false                  # Disable debug symbols generation
)

# Function to print the interactive menu
print_menu() {
    echo "========================================="
    echo "       XboxDownload - Publish Tool       "
    echo "========================================="
    echo
    echo "Select target to publish:"
    echo
    echo " 1) Publish All (Windows, macOS, Linux)"
    echo " 2) Publish for Windows (x64 + arm64)"
    echo " 3) Publish for macOS   (x64 + arm64)"
    echo " 4) Publish for Linux   (x64 + arm64)"
    echo " 5) Exit"
    echo
}

# Function to publish for a given runtime identifier (RID)
publish_target() {
    local rid="$1"

    # Normalize naming for output directory
    local output_rid="$rid"
    if [[ "$rid" == win-* ]]; then
        output_rid="windows-${rid#win-}"
    elif [[ "$rid" == osx-* ]]; then
        output_rid="macos-${rid#osx-}"
    elif [[ "$rid" == linux-* ]]; then
        output_rid="linux-${rid#linux-}"
    fi

    local output_dir="$OUTPUT_ROOT/XboxDownload-$output_rid"
    echo
    echo "--- Publishing for $rid ---"
    if ! dotnet publish "$PROJECT_ROOT" -r "$rid" -o "$output_dir" "${COMMON_ARGS[@]}"; then
        echo "❌ Publish failed for $rid"
        return 1
    fi
    echo "✅ Success: $output_dir"
}


# Publish Windows targets (x64 and arm64)
publish_windows() {
    publish_target "win-x64"
    publish_target "win-arm64"
}

# Publish macOS targets (x64 and arm64)
publish_macos() {
    publish_target "osx-x64"
    publish_target "osx-arm64"
}

# Publish Linux targets (x64 and arm64)
publish_linux() {
    publish_target "linux-x64"
    publish_target "linux-arm64"
}

# Main interactive loop
while true; do
    print_menu
    read -rp "Enter your choice [1-5]: " choice
    case "$choice" in
        1)
            publish_windows
            publish_macos
            publish_linux
            ;;
        2)
            publish_windows
            ;;
        3)
            publish_macos
            ;;
        4)
            publish_linux
            ;;
        5)
            echo "Exiting..."
            exit 0
            ;;
        *)
            echo "Invalid choice. Please enter 1-5."
            ;;
    esac
    echo
    read -rp "Press Enter to return to menu..." _
done
