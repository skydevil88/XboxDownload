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
# Menu
# --------------------------------------------------
function Show-Menu {
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "       XboxDownload - Publish Tool       " -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Select target to publish:"
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
# Publish one target
# --------------------------------------------------
function Publish-Target {
    param (
        [string]$rid,
        [string]$outputFolder
    )

    Write-Host ""
    $outputDir = Join-Path $outputRoot "XboxDownload-$outputFolder"

    # -------------------------------
    # Clean old directory
    # -------------------------------
    if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }

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
    # Create ZIP (tar preferred, fallback to Compress-Archive)
    # -------------------------------
    $zipPath = "$outputDir.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Write-Host "Creating ZIP: $zipPath" -ForegroundColor Cyan
    Push-Location $outputRoot

    $tarCmd = Get-Command "tar.exe" -ErrorAction SilentlyContinue

    if ($tarCmd) {
        # ---- Use tar if available ----
        & tar.exe -a -cf "$outputDir.zip" "XboxDownload-$outputFolder"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] ZIP created using tar.exe: $zipPath" -ForegroundColor Green
        } else {
            Write-Host "[ERROR] tar.exe failed, trying Compress-Archive..." -ForegroundColor Yellow
            Compress-Archive -Path "XboxDownload-$outputFolder" -DestinationPath $zipPath -Force
            Write-Host "[OK] ZIP created using Compress-Archive: $zipPath" -ForegroundColor Green
        }
    }
    else {
        # ---- Fallback ----
        Write-Host "[INFO] tar.exe not found, using Compress-Archive" -ForegroundColor Yellow
        Compress-Archive -Path "XboxDownload-$outputFolder" -DestinationPath $zipPath -Force
        Write-Host "[OK] ZIP created using Compress-Archive: $zipPath" -ForegroundColor Green
    }

    Pop-Location
}

# --------------------------------------------------
# Publish current system
# --------------------------------------------------
function Publish-Current {
    $os   = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    $rid  = ""
    $outputFolder = ""

    # ---------- ѡ�� RID ----------
    if ($os -match "Windows") {
        switch ($arch) {
            "X64"   { $rid = "win-x64";   $outputFolder = "windows-x64" }
            "Arm64" { $rid = "win-arm64"; $outputFolder = "windows-arm64" }
            "X86"   { $rid = "win-x86";   $outputFolder = "windows-x86" }
            default { Write-Host "Unsupported Windows architecture: $arch" -ForegroundColor Red; return }
        }
    }
    elseif ($os -match "Darwin|macOS") {
        switch ($arch) {
            "X64"   { $rid = "osx-x64";   $outputFolder = "macos-x64" }
            "Arm64" { $rid = "osx-arm64"; $outputFolder = "macos-arm64" }
            default { Write-Host "Unsupported macOS architecture: $arch" -ForegroundColor Red; return }
        }
    }
    elseif ($os -match "Linux") {
        switch ($arch) {
            "X64"   { $rid = "linux-x64";   $outputFolder = "linux-x64" }
            "Arm64" { $rid = "linux-arm64"; $outputFolder = "linux-arm64" }
            "X86"   { $rid = "linux-x86";   $outputFolder = "linux-x86" }
            "Arm"   { $rid = "linux-arm";   $outputFolder = "linux-arm" }
            default { Write-Host "Unsupported Linux architecture: $arch" -ForegroundColor Red; return }
        }
    }
    else {
        Write-Host "Unsupported OS: $os" -ForegroundColor Red
        return
    }

    Write-Host "-----------------------------------------" -ForegroundColor Cyan
    Write-Host "Detected system  : $os" -ForegroundColor Yellow
    Write-Host "CPU Architecture : $arch" -ForegroundColor Yellow
    Write-Host "Target RID       : $rid" -ForegroundColor Yellow
    Write-Host "Output folder    : .\Release\XboxDownload-$($outputFolder)" -ForegroundColor Yellow
    Write-Host "-----------------------------------------" -ForegroundColor Cyan

    Publish-Target $rid $outputFolder
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