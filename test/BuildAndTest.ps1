<#
.SYNOPSIS
    Complete build and test script for ContextSensor installer and auto-update
.DESCRIPTION
    Builds two versions of the installer, sets up local update server, and tests the complete update flow
#>

param(
    [switch]$SkipBuild,
    [switch]$SkipServerSetup,
    [int]$UpdateCheckSeconds = 60
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "ContextSensor Install & Update Test" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Get paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionDir = Split-Path -Parent $scriptPath
$testDir = $scriptPath
$updateServerDir = Join-Path $testDir "UpdateServer"
$wwwrootDir = Join-Path $updateServerDir "wwwroot"

Write-Host "Solution Directory: $solutionDir" -ForegroundColor Gray
Write-Host "Test Directory: $testDir" -ForegroundColor Gray
Write-Host ""

# Step 1: Build Version 1.0.0
if (-not $SkipBuild) {
    Write-Host "Step 1: Building Version 1.0.0..." -ForegroundColor Yellow
    Write-Host "-----------------------------------" -ForegroundColor Gray
    
    # Update version in Product.wxs to 1.0.0
    $productWxs = Join-Path $solutionDir "installer\Product.wxs"
    $content = Get-Content $productWxs -Raw
    $content = $content -replace 'Version="[\d\.]+"', 'Version="1.0.0"'
    Set-Content $productWxs $content
    
    # Build solution
    Write-Host "Building solution..." -ForegroundColor Gray
    Push-Location $solutionDir
    try {
        dotnet build ContextSensor.sln -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw "Solution build failed" }
    }
    finally {
        Pop-Location
    }
    
    # Build installer
    Write-Host "Building installer v1.0.0..." -ForegroundColor Gray
    Push-Location (Join-Path $solutionDir "installer")
    try {
        dotnet build ContextSensor.Installer.wixproj -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw "Installer build failed" }
    }
    finally {
        Pop-Location
    }
    
    # Copy v1.0.0 MSI
    $msi100Path = Join-Path $solutionDir "installer\bin\Release\ContextSensor.msi"
    $msi100Dest = Join-Path $testDir "ContextSensor-1.0.0.msi"
    Copy-Item $msi100Path $msi100Dest -Force
    Write-Host "Created: $msi100Dest" -ForegroundColor Green
    
    # Calculate checksum for v1.0.0
    $hash100 = Get-FileHash $msi100Dest -Algorithm SHA256
    Write-Host "SHA256: $($hash100.Hash.ToLower())" -ForegroundColor Gray
    Write-Host ""
    
    # Step 2: Build Version 1.0.1
    Write-Host "Step 2: Building Version 1.0.1..." -ForegroundColor Yellow
    Write-Host "-----------------------------------" -ForegroundColor Gray
    
    # Update version in Product.wxs to 1.0.1
    $content = Get-Content $productWxs -Raw
    $content = $content -replace 'Version="1\.0\.0"', 'Version="1.0.1"'
    Set-Content $productWxs $content
    
    # Build solution again
    Write-Host "Building solution..." -ForegroundColor Gray
    Push-Location $solutionDir
    try {
        dotnet build ContextSensor.sln -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw "Solution build failed" }
    }
    finally {
        Pop-Location
    }
    
    # Build installer
    Write-Host "Building installer v1.0.1..." -ForegroundColor Gray
    Push-Location (Join-Path $solutionDir "installer")
    try {
        dotnet build ContextSensor.Installer.wixproj -c Release --nologo
        if ($LASTEXITCODE -ne 0) { throw "Installer build failed" }
    }
    finally {
        Pop-Location
    }
    
    # Copy v1.0.1 MSI
    $msi101Path = Join-Path $solutionDir "installer\bin\Release\ContextSensor.msi"
    $msi101Dest = Join-Path $testDir "ContextSensor-1.0.1.msi"
    Copy-Item $msi101Path $msi101Dest -Force
    Write-Host "Created: $msi101Dest" -ForegroundColor Green
    
    # Calculate checksum for v1.0.1
    $hash101 = Get-FileHash $msi101Dest -Algorithm SHA256
    Write-Host "SHA256: $($hash101.Hash.ToLower())" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Build Complete!" -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "Skipping build (using existing MSI files)..." -ForegroundColor Yellow
    $msi100Dest = Join-Path $testDir "ContextSensor-1.0.0.msi"
    $msi101Dest = Join-Path $testDir "ContextSensor-1.0.1.msi"
    
    if (-not (Test-Path $msi100Dest)) { throw "v1.0.0 MSI not found: $msi100Dest" }
    if (-not (Test-Path $msi101Dest)) { throw "v1.0.1 MSI not found: $msi101Dest" }
    
    $hash100 = Get-FileHash $msi100Dest -Algorithm SHA256
    $hash101 = Get-FileHash $msi101Dest -Algorithm SHA256
    Write-Host ""
}

# Step 3: Setup Update Server
if (-not $SkipServerSetup) {
    Write-Host "Step 3: Setting up Update Server..." -ForegroundColor Yellow
    Write-Host "-----------------------------------" -ForegroundColor Gray
    
    # Create wwwroot directory
    if (-not (Test-Path $wwwrootDir)) {
        New-Item -Path $wwwrootDir -ItemType Directory -Force | Out-Null
    }
    
    # Copy v1.0.1 MSI to wwwroot
    Copy-Item $msi101Dest $wwwrootDir -Force
    Write-Host "Copied v1.0.1 MSI to update server" -ForegroundColor Gray
    
    # Create manifest.json
    $manifest = @{
        version = "1.0.1"
        releaseDate = (Get-Date).ToUniversalTime().ToString("o")
        downloadUrl = "http://localhost:8080/ContextSensor-1.0.1.msi"
        checksum = "sha256:$($hash101.Hash.ToLower())"
        releaseNotes = "Test update from v1.0.0 to v1.0.1. This update includes:`n- Testing data preservation`n- Testing automatic update`n- Testing service restart"
        minimumVersion = "1.0.0"
        mandatoryUpdate = $false
    }
    
    $manifestPath = Join-Path $wwwrootDir "manifest.json"
    $manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath
    Write-Host "Created manifest.json" -ForegroundColor Gray
    Write-Host ""
    
    # Display manifest
    Write-Host "Update Manifest:" -ForegroundColor Cyan
    Get-Content $manifestPath | Write-Host -ForegroundColor Gray
    Write-Host ""
}

# Step 4: Instructions
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Testing Instructions" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "PHASE 1: Install v1.0.0" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Gray
Write-Host "1. Install the initial version:" -ForegroundColor White
Write-Host "   msiexec /i `"$msi100Dest`" /l*v install-v1.0.0.log" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Verify installation:" -ForegroundColor White
Write-Host "   Get-Service ContextSensor.*" -ForegroundColor Cyan
Write-Host "   Get-Process ContextSensor.App" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Check version:" -ForegroundColor White
Write-Host "   Get-ItemProperty -Path 'HKLM:\Software\ContextSensor' | Select Version" -ForegroundColor Cyan
Write-Host "   Expected: 1.0.0" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Create test data:" -ForegroundColor White
Write-Host "   New-Item -Path 'C:\ProgramData\ContextSensor\data\test-preservation.txt' -Value 'This should survive upgrade!' -Force" -ForegroundColor Cyan
Write-Host ""

Write-Host "PHASE 2: Start Update Server" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Gray
Write-Host "5. Open NEW PowerShell window and run:" -ForegroundColor White
Write-Host "   cd `"$updateServerDir`"" -ForegroundColor Cyan
Write-Host "   dotnet run" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Server will start on http://localhost:8080" -ForegroundColor Gray
Write-Host "   Leave this window open!" -ForegroundColor Gray
Write-Host ""
Write-Host "6. Verify server (in another window):" -ForegroundColor White
Write-Host "   Invoke-RestMethod -Uri 'http://localhost:8080/manifest.json'" -ForegroundColor Cyan
Write-Host ""

Write-Host "PHASE 3: Configure & Test Auto-Update" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Gray
Write-Host "7. Configure updater for fast testing:" -ForegroundColor White
Write-Host "   `$config = Get-Content 'C:\Program Files\ContextSensor\updater.config.json' | ConvertFrom-Json" -ForegroundColor Cyan
Write-Host "   `$config.Update.UpdateCheckUrl = 'http://localhost:8080/manifest.json'" -ForegroundColor Cyan
Write-Host "   `$config.Update.CheckIntervalHours = $($UpdateCheckSeconds / 3600)" -ForegroundColor Cyan
Write-Host "   `$config | ConvertTo-Json -Depth 10 | Set-Content 'C:\Program Files\ContextSensor\updater.config.json'" -ForegroundColor Cyan
Write-Host ""
Write-Host "8. Restart updater service:" -ForegroundColor White
Write-Host "   Restart-Service ContextSensor.Updater" -ForegroundColor Cyan
Write-Host ""
Write-Host "9. Monitor updater logs (in real-time):" -ForegroundColor White
Write-Host "   Get-Content 'C:\ProgramData\ContextSensor\logs\updater.log' -Wait -Tail 20" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Wait for update to be detected and installed (~$UpdateCheckSeconds seconds)" -ForegroundColor Gray
Write-Host "   You should see:" -ForegroundColor Gray
Write-Host "   - 'Checking for updates...'" -ForegroundColor Gray
Write-Host "   - 'New version available: 1.0.1'" -ForegroundColor Gray
Write-Host "   - 'Downloading update...'" -ForegroundColor Gray
Write-Host "   - 'Installing update...'" -ForegroundColor Gray
Write-Host "   - 'Update installed successfully'" -ForegroundColor Gray
Write-Host ""

Write-Host "PHASE 4: Verify Update Success" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Gray
Write-Host "10. Check new version:" -ForegroundColor White
Write-Host "    Get-ItemProperty -Path 'HKLM:\Software\ContextSensor' | Select Version" -ForegroundColor Cyan
Write-Host "    Expected: 1.0.1" -ForegroundColor Gray
Write-Host ""
Write-Host "11. Verify data preserved:" -ForegroundColor White
Write-Host "    Get-Content 'C:\ProgramData\ContextSensor\data\test-preservation.txt'" -ForegroundColor Cyan
Write-Host "    Expected: 'This should survive upgrade!'" -ForegroundColor Gray
Write-Host ""
Write-Host "12. Check services running:" -ForegroundColor White
Write-Host "    Get-Service ContextSensor.* | Format-Table Name, Status" -ForegroundColor Cyan
Write-Host "    Get-Process ContextSensor.App" -ForegroundColor Cyan
Write-Host ""

Write-Host "=====================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "MSI Files:" -ForegroundColor White
Write-Host "  v1.0.0: $msi100Dest" -ForegroundColor Gray
Write-Host "  v1.0.1: $msi101Dest" -ForegroundColor Gray
Write-Host ""
Write-Host "Update Server: $updateServerDir" -ForegroundColor White
Write-Host ""
Write-Host "Follow the testing instructions above!" -ForegroundColor Yellow
Write-Host ""
