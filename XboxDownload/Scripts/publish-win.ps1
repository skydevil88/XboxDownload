Write-Host "Publishing XboxDownload..." -ForegroundColor Cyan

# --------------------------------------------------
# Paths
# --------------------------------------------------
$projectPath = (Resolve-Path "$PSScriptRoot\..").Path
$projectFile = Join-Path $projectPath "XboxDownload.csproj"
$outputRoot  = Join-Path $projectPath "Scripts\Release"

# --------------------------------------------------
# Shared dotnet publish arguments
# --------------------------------------------------
$commonArgs = @(
    "-c", "Release",
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "--self-contained", "true",
    "/p:PublishTrimmed=false",
    "/p:DebugType=none",
    "/p:DebugSymbols=false"
)

# --------------------------------------------------
# Utils
# --------------------------------------------------
function Clean-ReleaseDir {
    if (Test-Path $outputRoot) {
        Write-Host "Cleaning Release directory: $outputRoot" -ForegroundColor Yellow
        Remove-Item $outputRoot -Recurse -Force
        Write-Host "[OK] Release directory removed" -ForegroundColor Green
    }
}

# --------------------------------------------------
# Publish one target
# --------------------------------------------------
function Publish-Target {
    param (
        [string]$rid,
        [string]$outputFolder
    )

    $outputDir = Join-Path $outputRoot "XboxDownload-$outputFolder"
    Write-Host "Publishing for $rid -> $outputDir" -ForegroundColor Yellow

    dotnet publish $projectFile -r $rid -o $outputDir @commonArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to publish for $rid" -ForegroundColor Red
        return
    }

    Write-Host "[OK] Success: $outputDir" -ForegroundColor Green

    # -------------------------------
    # Copy extra files only for Linux/macOS
    # -------------------------------
    if ($rid -like "linux*" -or $rid -like "osx*") {
        $files = @("Readme.md", "run_xboxdownload.sh")
        foreach ($file in $files) {
            $src = Join-Path $PSScriptRoot $file
            if (Test-Path $src) {
                $destName = if ($rid -like "osx*" -and $file -eq "run_xboxdownload.sh") { "run_xboxdownload.command" } else { $file }
                Copy-Item $src (Join-Path $outputDir $destName) -Force
            } else {
                Write-Host "[WARN] File not found: $src" -ForegroundColor Yellow
            }
        }
    }

    # -------------------------------
    # Create ZIP
    # -------------------------------
    $zipPath = "$outputDir.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Write-Host "Creating ZIP: $zipPath" -ForegroundColor Cyan
    Push-Location $outputRoot
    Compress-Archive -Path "XboxDownload-$outputFolder" -DestinationPath $zipPath -Force
    Pop-Location
    Write-Host "[OK] ZIP created: $zipPath" -ForegroundColor Green
}

# --------------------------------------------------
# Publish current system
# --------------------------------------------------
function Publish-Current {
    $os   = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()

    if ($os -match "Windows") {
        switch ($arch) {
            "X64"   { Publish-Target "win-x64"   "windows-x64" }
            "Arm64" { Publish-Target "win-arm64" "windows-arm64" }
            "X86"   { Publish-Target "win-x86"   "windows-x86" }
            default { Write-Host "Unsupported Windows architecture: $arch" -ForegroundColor Red }
        }
    }
    elseif ($os -match "Darwin|macOS") {
        switch ($arch) {
            "X64"   { Publish-Target "osx-x64"   "macos-x64" }
            "Arm64" { Publish-Target "osx-arm64" "macos-arm64" }
            default { Write-Host "Unsupported macOS architecture: $arch" -ForegroundColor Red }
        }
    }
    elseif ($os -match "Linux") {
        switch ($arch) {
            "X64"   { Publish-Target "linux-x64"   "linux-x64" }
            "Arm64" { Publish-Target "linux-arm64" "linux-arm64" }
            default { Write-Host "Unsupported Linux architecture: $arch" -ForegroundColor Red }
        }
    }
    else {
        Write-Host "Unsupported OS: $os" -ForegroundColor Red
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

# --------------------------------------------------
# Menu
# --------------------------------------------------
function Show-Menu {
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "       XboxDownload - Publish Tool       " -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host " 1) Publish for Current System"
    Write-Host " 2) Publish for Windows (x64 + arm64)"
    Write-Host " 3) Publish for macOS   (x64 + arm64)"
    Write-Host " 4) Publish for Linux   (x64 + arm64)"
    Write-Host " 5) Publish All (Windows, macOS, Linux)"
    Write-Host " 6) Exit"
    Write-Host ""
}

# --------------------------------------------------
# Main loop
# --------------------------------------------------
do {
    Show-Menu
    $choice = Read-Host "Enter your choice (1-6) [Default: 1]"
    if ([string]::IsNullOrWhiteSpace($choice)) { $choice = "1" }

    $start = Get-Date

    switch ($choice) {
        "1" { Publish-Current }
        "2" { Publish-Windows }
        "3" { Publish-MacOS }
        "4" { Publish-Linux }
        "5" {
            Clean-ReleaseDir
            Publish-Windows
            Publish-MacOS
            Publish-Linux
        }
        "6" { break }
        default { Write-Host "Invalid selection." -ForegroundColor Red }
    }

    $elapsed = (Get-Date) - $start
    Write-Host "Done in $([Math]::Round($elapsed.TotalSeconds,2))s" -ForegroundColor Cyan
    Write-Host ""
}
while ($choice -ne "6")
