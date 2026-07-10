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
        rm -rf -- "$OUTPUT_ROOT"
        echo -e "\033[32m[OK] Release directory removed\033[0m"
    fi
}

remove_symbol_files() {
    local output_dir="$1"
    find "$output_dir" -type f -name "*.pdb" -delete
}

copy_readme() {
    local output_dir="$1"
    local src="$SCRIPT_DIR/README.md"
    [[ -f "$src" ]] && cp -f "$src" "$output_dir/README.md"
}

copy_run_script() {
    local output_dir="$1"
    local file_name="$2"
    local src="$SCRIPT_DIR/run_xboxdownload.sh"

    if [[ -f "$src" ]]; then
        cp -f "$src" "$output_dir/$file_name"
        chmod +x "$output_dir/$file_name"
    fi
}

get_project_property() {
    local property_name="$1"
    sed -n "s:.*<$property_name>\\(.*\\)</$property_name>.*:\\1:p" "$PROJECT_FILE" | head -n 1
}

create_macos_icon() {
    local icon_source="$SCRIPT_DIR/../Assets/xbox.ico"
    local resources_dir="$1"
    local iconset_dir="$resources_dir/XboxDownload.iconset"
    local icns_file="$resources_dir/XboxDownload.icns"
    local icon_png="$resources_dir/XboxDownload.icon.png"

    if [[ "$(uname -s)" != "Darwin" ]]; then
        echo -e "\033[31m[ERROR] macOS .app icon generation requires macOS because iconutil/sips are unavailable\033[0m"
        return 1
    fi

    if ! command -v sips >/dev/null 2>&1 || ! command -v iconutil >/dev/null 2>&1; then
        echo -e "\033[31m[ERROR] macOS .app icon generation requires sips and iconutil\033[0m"
        return 1
    fi

    if [[ ! -f "$icon_source" ]]; then
        echo -e "\033[31m[ERROR] Icon source not found: $icon_source\033[0m"
        return 1
    fi

    rm -rf -- "$iconset_dir"
    mkdir -p "$iconset_dir"

    sips -s format png "$icon_source" --out "$icon_png" >/dev/null

    local icon_specs=(
        "16:icon_16x16.png"
        "32:icon_16x16@2x.png"
        "32:icon_32x32.png"
        "64:icon_32x32@2x.png"
        "128:icon_128x128.png"
        "256:icon_128x128@2x.png"
        "256:icon_256x256.png"
        "512:icon_256x256@2x.png"
        "512:icon_512x512.png"
        "1024:icon_512x512@2x.png"
    )

    local spec size file_name
    for spec in "${icon_specs[@]}"; do
        size="${spec%%:*}"
        file_name="${spec#*:}"
        sips -z "$size" "$size" "$icon_png" --out "$iconset_dir/$file_name" >/dev/null
    done

    iconutil -c icns "$iconset_dir" -o "$icns_file"
    rm -rf -- "$iconset_dir" "$icon_png"
}

create_macos_app_bundle() {
    local output_dir="$1"
    local output_rid="$2"
    local app_name="XboxDownload"
    local app_dir="$output_dir/$app_name.app"
    local contents_dir="$app_dir/Contents"
    local macos_dir="$contents_dir/MacOS"
    local resources_dir="$contents_dir/Resources"
    local staging_dir="$output_dir/.app-staging"
    local bundle_version short_version

    bundle_version="$(get_project_property "FileVersion")"
    bundle_version="${bundle_version:-1.0.0}"
    short_version="$(echo "$bundle_version" | sed -E 's/^([0-9]+(\.[0-9]+){0,2}).*$/\1/')"

    echo -e "\033[96mCreating macOS app bundle: $app_dir\033[0m"

    rm -rf -- "$app_dir" "$staging_dir"
    mkdir -p "$macos_dir" "$resources_dir" "$staging_dir"

    find "$output_dir" -mindepth 1 -maxdepth 1 \
        ! -name "$app_name.app" \
        ! -name ".app-staging" \
        -exec mv {} "$staging_dir/" \;

    cp -R "$staging_dir/." "$macos_dir/"
    cat > "$macos_dir/${app_name}Launcher" <<'EOF'
#!/usr/bin/env bash
set -e

APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$APP_DIR/XboxDownload" "$@"
EOF
    chmod +x "$macos_dir/$app_name" "$macos_dir/${app_name}Launcher"

    create_macos_icon "$resources_dir"

    cat > "$contents_dir/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "https://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>zh_CN</string>
    <key>CFBundleDisplayName</key>
    <string>XboxDownload</string>
    <key>CFBundleExecutable</key>
    <string>XboxDownloadLauncher</string>
    <key>CFBundleIconFile</key>
    <string>XboxDownload</string>
    <key>CFBundleIdentifier</key>
    <string>com.xboxdownload.app</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>XboxDownload</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$short_version</string>
    <key>CFBundleVersion</key>
    <string>$bundle_version</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

    rm -rf -- "$staging_dir"

    copy_readme "$output_dir"

    echo -e "\033[92m[OK] macOS app bundle created: $app_dir ($output_rid)\033[0m"
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
    local native_extract=true
    [[ "$rid" == osx* ]] && native_extract=false

    # -------------------------------
    # Clean old directory
    # -------------------------------
    if [[ -d "$output_dir" ]]; then
        rm -rf -- "$output_dir"
    fi

    mkdir -p "$output_dir"

    echo
    echo -e "\033[93mPublishing for $rid -> $output_dir\033[0m"
    dotnet publish "$PROJECT_FILE" -r "$rid" -o "$output_dir" "${COMMON_ARGS[@]}" /p:IncludeNativeLibrariesForSelfExtract="$native_extract"
    remove_symbol_files "$output_dir"
    echo -e "\033[92m[OK] Publish success: $output_dir\033[0m"

    if [[ "$rid" == osx* ]]; then
        create_macos_app_bundle "$output_dir" "$output_rid"
        copy_run_script "$output_dir" "run_xboxdownload.command"
    fi

    # -------------------------------
    # Copy extra files for Linux
    # -------------------------------
    if [[ "$rid" == linux* ]]; then
        copy_run_script "$output_dir" "run_xboxdownload.sh"
        copy_readme "$output_dir"

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
    [[ -f "$zip_file" ]] && rm -f -- "$zip_file"
    echo -e "\033[96mCreating ZIP: $zip_file\033[0m"
    (cd "$OUTPUT_ROOT" && zip -r "$(basename "$zip_file")" "XboxDownload-$output_rid" > /dev/null)
    echo -e "\033[92m[OK] ZIP created: $zip_file\033[0m"

    rm -rf -- "$output_dir"
    echo -e "\033[92m[OK] Output directory removed: $output_dir\033[0m"
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
    echo -e "\033[93mOutput folder    : ./Release/XboxDownload-${output_folder}\033[0m"
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
	elapsed=$(awk "BEGIN {print $end_time - $start_time}")
	printf "\033[0;96mDone in %.4fs\033[0m\n" "$elapsed"
    echo
done
