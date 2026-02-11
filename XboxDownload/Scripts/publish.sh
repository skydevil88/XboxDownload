#!/bin/bash
set -e

echo -e "\033[96mPublishing XboxDownload...\033[0m"

# --------------------------------------------------
# Paths
# --------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_FILE="$SCRIPT_DIR/../XboxDownload.csproj"
OUTPUT_ROOT="$SCRIPT_DIR/Release"

# --------------------------------------------------
# Shared dotnet publish arguments
# --------------------------------------------------
COMMON_ARGS=(
    -c Release
    /p:PublishSingleFile=true
    /p:IncludeNativeLibrariesForSelfExtract=true
    --self-contained true
    /p:PublishTrimmed=false
    /p:DebugType=none
    /p:DebugSymbols=false
)

# --------------------------------------------------
# Utils
# --------------------------------------------------
clean_release_dir() {
    if [[ -d "$OUTPUT_ROOT" ]]; then
        echo -e "\033[33mCleaning Release directory: $OUTPUT_ROOT\033[0m"
        rm -rf "$OUTPUT_ROOT"
        echo -e "\033[32m[OK] Release directory removed\033[0m"
    fi
}

# --------------------------------------------------
# Menu
# --------------------------------------------------
print_menu() {
    echo -e "\033[96m=========================================\033[0m"
    echo -e "\033[96m       XboxDownload - Publish Tool       \033[0m"
    echo -e "\033[96m=========================================\033[0m"
    echo
    echo "Select target to publish:"
    echo
    echo " 1) Publish for Current System"
    echo " 2) Publish for Windows (x64 + arm64)"
    echo " 3) Publish for macOS   (x64 + arm64)"
    echo " 4) Publish for Linux   (x64 + arm64)"
    echo " 5) Publish All (Windows, macOS, Linux)"
    echo " 6) Exit"
    echo
}

# --------------------------------------------------
# Publish one target
# --------------------------------------------------
publish_target() {
    local rid="$1"
    local output_rid="$2"

    local output_dir="$OUTPUT_ROOT/XboxDownload-$output_rid"

    # -------------------------------
    # Clean old directory
    # -------------------------------
    if [[ -d "$output_dir" ]]; then
        rm -rf "$output_dir"
    fi

    mkdir -p "$output_dir"

    echo
    echo -e "\033[93mPublishing for $rid -> $output_dir\033[0m"
    dotnet publish "$PROJECT_FILE" -r "$rid" -o "$output_dir" "${COMMON_ARGS[@]}"
    echo -e "\033[92m[OK] Publish success: $output_dir\033[0m"

    # -------------------------------
    # Copy extra files for Linux/macOS
    # -------------------------------
    if [[ "$rid" == linux* || "$rid" == osx* ]]; then
        # run script
        local run_file="run_xboxdownload.sh"
        local src="$SCRIPT_DIR/$run_file"
        if [[ -f "$src" ]]; then
            local dest="$run_file"
            [[ "$rid" == osx* ]] && dest="run_xboxdownload.command"
            cp -f "$src" "$output_dir/$dest"
            chmod +x "$output_dir/$dest"
        fi

        # Readme.md (no exec permission)
        src="$SCRIPT_DIR/Readme.md"
        if [[ -f "$src" ]]; then
            cp -f "$src" "$output_dir/Readme.md"
        fi

        # Set main executable
        exe="$output_dir/XboxDownload"
        if [[ -f "$exe" ]]; then
            chmod +x "$exe"
        fi
    fi

    # -------------------------------
    # Create ZIP
    # -------------------------------
    zip_file="$output_dir.zip"
    [[ -f "$zip_file" ]] && rm -f "$zip_file"
    echo -e "\033[96mCreating ZIP: $zip_file\033[0m"
    (cd "$OUTPUT_ROOT" && zip -r "$(basename "$zip_file")" "XboxDownload-$output_rid" > /dev/null)
    echo -e "\033[92m[OK] ZIP created: $zip_file\033[0m"
}

# --------------------------------------------------
# Publish current system
# --------------------------------------------------
publish_current() {
    local os arch rid output_folder
    os="$(uname -s)"
    arch="$(uname -m)"
    rid=""
    output_folder=""

    case "$os" in
        Linux)
            case "$arch" in
                x86_64) rid="linux-x64";           output_folder="linux-x64" ;;
                aarch64|arm64) rid="linux-arm64";  output_folder="linux-arm64" ;;
                i386|i686) rid="linux-x86";        output_folder="linux-x86" ;;
                armv7l|arm|armhf) rid="linux-arm"; output_folder="linux-arm" ;;
                *) echo "[ERROR] Unsupported Linux arch: $arch"; exit 1 ;;
            esac
            ;;
        Darwin)
            case "$arch" in
                x86_64) rid="osx-x64";   output_folder="macos-x64" ;;
                arm64)  rid="osx-arm64"; output_folder="macos-arm64" ;;
                *) echo "[ERROR] Unsupported macOS arch: $arch"; exit 1 ;;
            esac
            ;;
        MINGW*|MSYS*|CYGWIN*|Windows_NT)
            case "$arch" in
                x86_64|AMD64) rid="win-x64";    output_folder="windows-x64" ;;
                aarch64|arm64) rid="win-arm64"; output_folder="windows-arm64" ;;
                i386|i686) rid="win-x86";       output_folder="windows-x86" ;;
                *) echo "[ERROR] Unsupported Windows arch: $arch"; exit 1 ;;
            esac
            ;;
        *)
            echo "[ERROR] Unsupported OS: $os"; exit 1 ;;
    esac

    echo -e "\033[96m-----------------------------------------\033[0m"
    echo -e "\033[93mDetected system  : $os\033[0m"
    echo -e "\033[93mCPU Architecture : $arch\033[0m"
    echo -e "\033[93mTarget RID       : $rid\033[0m"
    echo -e "\033[93mOutput folder    : XboxDownload-$output_folder\033[0m"
    echo -e "\033[96m-----------------------------------------\033[0m"

    publish_target "$rid" "$output_folder"
}

publish_windows() {
    publish_target "win-x64"   "windows-x64"
    publish_target "win-arm64" "windows-arm64"
}

publish_macos() {
    publish_target "osx-x64"   "macos-x64"
    publish_target "osx-arm64" "macos-arm64"
}

publish_linux() {
    publish_target "linux-x64"   "linux-x64"
    publish_target "linux-arm64" "linux-arm64"
}

# --------------------------------------------------
# Main loop
# --------------------------------------------------
while true; do
    print_menu
    read -rp "Enter your choice [1-6] (Default: 1): " choice
    choice="${choice:-1}"

    start_time=$(date +%s.%N)

    case "$choice" in
        1) publish_current ;;
        2) publish_windows ;;
        3) publish_macos ;;
        4) publish_linux ;;
        5)
            clean_release_dir
            publish_windows
            publish_macos
            publish_linux
            ;;
        6) echo "Exiting..."; exit 0 ;;
        *) echo "Invalid choice. Please enter 1-6." ;;
    esac

    end_time=$(date +%s.%N)
    elapsed=$(echo "$end_time - $start_time" | bc)

    printf "\033[0;96mDone in %.2fs\033[0m\n" "$elapsed"
    echo
done