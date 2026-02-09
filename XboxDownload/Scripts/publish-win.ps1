Write-Host "Publishing XboxDownload..." -ForegroundColor Cyan

# Root paths
$projectPath = (Resolve-Path "$PSScriptRoot\..").Path
$outputRoot  = Join-Path $projectPath "Scripts\Release"

# Shared dotnet publish arguments
$commonArgs = @(
    "-c", "Release",
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "--self-contained", "true",
    "/p:PublishTrimmed=false",
    "/p:DebugType=none",
    "/p:DebugSymbols=false"
)

function Publish-Target {
    param (
        [string]$rid,
        [string]$outputFolder
    )

    $outputDir = Join-Path $outputRoot "XboxDownload-$outputFolder"
    Write-Host "Publishing for $rid -> $outputDir" -ForegroundColor Yellow

    dotnet publish $projectPath -r $rid -o $outputDir @commonArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to publish for $rid" -ForegroundColor Red
        return
    }
    elseif (Test-Path $outputDir) {
        $absPath = (Resolve-Path $outputDir).Path
        Write-Host "[OK] Success: $absPath" -ForegroundColor Green
    }
    else {
        Write-Host "[WARN] Output directory not found: $outputDir" -ForegroundColor Yellow
        return
    }

    # =========================
    # Post-Publish: Copy extra files
    # =========================
    $filesToCopy = @("Readme.md", "run_xboxdownload.sh")
    foreach ($file in $filesToCopy) {
        $src = Join-Path $PSScriptRoot $file
        if (Test-Path $src) {
            if ($rid -like "osx*") {
                $destFileName = if ($file -eq "run_xboxdownload.sh") { "run_xboxdownload.command" } else { $file }
                Copy-Item $src -Destination (Join-Path $outputDir $destFileName) -Force
            }
            elseif ($rid -like "linux*") {
                Copy-Item $src -Destination $outputDir -Force
            }
        }
        else {
            Write-Host "[WARN] File not found: $src" -ForegroundColor Yellow
        }
    }

    # =========================
    # Post-Publish: ZIP the folder
    # =========================
    $zipPath = "$outputDir.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Write-Host "Creating ZIP: $zipPath" -ForegroundColor Cyan
    Compress-Archive -Path $outputDir\* -DestinationPath $zipPath -Force

    if (Test-Path $zipPath) {
        Write-Host "[OK] ZIP created: $zipPath" -ForegroundColor Green
    }
    else {
        Write-Host "[WARN] Failed to create ZIP: $zipPath" -ForegroundColor Yellow
    }
}

# Only "Publish Current System" supports x86
function Publish-Current {
    $os   = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()

    switch ($true) {
        ($os -match "Windows") {
            switch ($arch) {
                "X64"   { Publish-Target "win-x64"   "windows-x64" }
                "Arm64" { Publish-Target "win-arm64" "windows-arm64" }
                "X86"   { Publish-Target "win-x86"   "windows-x86" }
                default { Write-Host "Unsupported Windows architecture: $arch" -ForegroundColor Red }
            }
        }
        ($os -match "Darwin|macOS") {
            switch ($arch) {
                "X64"   { Publish-Target "osx-x64"   "macos-x64" }
                "Arm64" { Publish-Target "osx-arm64" "macos-arm64" }
                default { Write-Host "Unsupported macOS architecture: $arch" -ForegroundColor Red }
            }
        }
        ($os -match "Linux") {
            switch ($arch) {
                "X64"   { Publish-Target "linux-x64"   "linux-x64" }
                "Arm64" { Publish-Target "linux-arm64" "linux-arm64" }
                default { Write-Host "Unsupported Linux architecture: $arch" -ForegroundColor Red }
            }
        }
        default {
            Write-Host "Unsupported OS: $os" -ForegroundColor Red
        }
    }
}

function Publish-Windows {
    Publish-Target "win-x64"   "windows-x64"
    Publish-Target "win-arm64" "windows-arm64"
}

function Publish-MacOS {
    Publish-Target "osx-x64"   "macos-x64"
    Publish-Target "osx-arm64" "macos-arm64"
}

function Publish-Linux {
    Publish-Target "linux-x64"   "linux-x64"
    Publish-Target "linux-arm64" "linux-arm64"
}

function Show-Menu {
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "       XboxDownload - Publish Tool       " -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Select target to publish:" ""
    Write-Host " 1) Publish for Current System (Default, supports x86)"
    Write-Host " 2) Publish for Windows (x64 + arm64)"
    Write-Host " 3) Publish for macOS   (x64 + arm64)"
    Write-Host " 4) Publish for Linux   (x64 + arm64)"
    Write-Host " 5) Publish All (Windows, macOS, Linux)"
    Write-Host " 6) Exit"
    Write-Host ""
}

# Main interactive loop
do {
    Show-Menu
    $choice = Read-Host "Enter your choice (1-6) [Default: 1]"
    if ([string]::IsNullOrWhiteSpace($choice)) { $choice = "1" }

    $startTime = Get-Date

    switch ($choice) {
        "1" { Publish-Current }
        "2" { Publish-Windows }
        "3" { Publish-MacOS }
        "4" { Publish-Linux }
        "5" { Publish-Windows; Publish-MacOS; Publish-Linux }
        "6" { break }
        default { Write-Host "Invalid selection. Please choose 1-6." -ForegroundColor Red }
    }

    $duration = (Get-Date) - $startTime
    Write-Host "Done in $([Math]::Round($duration.TotalSeconds,2)) seconds." -ForegroundColor Cyan
    Write-Host ""
}
while ($choice -ne "6")
