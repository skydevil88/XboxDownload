Write-Host "Publishing XboxDownload..." -ForegroundColor Cyan

# Root paths
$projectPath = (Resolve-Path "$PSScriptRoot\..").Path
$outputRoot = Join-Path $projectPath "Scripts\Release"

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
        [string]$outputFolder  # friendly output name
    )

    $outputDir = Join-Path $outputRoot "XboxDownload-$outputFolder"
    Write-Host "Publishing for $rid -> $outputDir" -ForegroundColor Yellow

    dotnet publish $projectPath -r $rid -o $outputDir @commonArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to publish for $rid" -ForegroundColor Red
    } elseif (Test-Path $outputDir) {
        $absPath = (Resolve-Path $outputDir).Path
        Write-Host "Success: $absPath" -ForegroundColor Green
    } else {
        Write-Host "Output directory not found: $outputDir" -ForegroundColor Yellow
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
    Write-Host "Select target to publish:"
    Write-Host ""
    Write-Host " 1) Publish All (Windows, macOS, Linux)"
    Write-Host " 2) Publish for Windows (x64 + arm64)"
    Write-Host " 3) Publish for macOS   (x64 + arm64)"
    Write-Host " 4) Publish for Linux   (x64 + arm64)"
    Write-Host " 5) Exit"
    Write-Host ""
}

# Main interactive loop
do {
    Show-Menu
    $choice = Read-Host "Enter your choice (1-5)"

    $startTime = Get-Date

    switch ($choice) {
        "1" { Publish-Windows; Publish-MacOS; Publish-Linux }
        "2" { Publish-Windows }
        "3" { Publish-MacOS }
        "4" { Publish-Linux }
        "5" { break }
        default { Write-Host "Invalid selection. Please choose 1-5." }
    }

    $duration = (Get-Date) - $startTime
    Write-Host "Done in $($duration.TotalSeconds) seconds." -ForegroundColor Cyan
    Write-Host ""
} while ($choice -ne "5")
