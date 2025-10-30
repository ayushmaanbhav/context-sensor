<#
.SYNOPSIS
    Simple manual installation script for testing (bypasses WiX issues)
.DESCRIPTION
    Installs ContextSensor manually for testing monitoring and update services
#>

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "ContextSensor Simple Install v$Version" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$solutionDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$installDir = "C:\Program Files\ContextSensor"
$dataDir = "C:\ProgramData\ContextSensor"

# Check admin
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Step 1: Build projects
Write-Host "Step 1: Building projects..." -ForegroundColor Yellow
Push-Location $solutionDir
try {
    dotnet build ContextSensor.sln -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Host "Build successful!" -ForegroundColor Green
}
finally {
    Pop-Location
}
Write-Host ""

# Step 2: Create directories
Write-Host "Step 2: Creating directories..." -ForegroundColor Yellow
@($installDir, "$dataDir\data", "$dataDir\logs", "$dataDir\temp\updates") | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -Path $_ -ItemType Directory -Force | Out-Null
        Write-Host "Created: $_" -ForegroundColor Gray
    }
}
Write-Host ""

# Step 3: Copy files
Write-Host "Step 3: Copying application files..." -ForegroundColor Yellow

$appBin = Join-Path $solutionDir "src\ContextSensor.App\bin\Release\net8.0-windows"
$monitorBin = Join-Path $solutionDir "src\ContextSensor.Monitor\bin\Release\net8.0-windows\win-x64"
$updaterBin = Join-Path $solutionDir "src\ContextSensor.Updater\bin\Release\net8.0-windows\win-x64"

# Copy main app
Get-ChildItem $appBin -File | ForEach-Object {
    Copy-Item $_.FullName $installDir -Force
    Write-Host "Copied: $($_.Name)" -ForegroundColor Gray
}

# Copy monitor service
Get-ChildItem $monitorBin -File | ForEach-Object {
    Copy-Item $_.FullName $installDir -Force
}

# Copy updater service  
Get-ChildItem $updaterBin -File | ForEach-Object {
    Copy-Item $_.FullName $installDir -Force
}

Write-Host "Files copied successfully!" -ForegroundColor Green
Write-Host ""

# Step 4: Configure paths
Write-Host "Step 4: Configuring services..." -ForegroundColor Yellow

# Update monitor config
$monitorConfig = Get-Content (Join-Path $installDir "monitor.config.json") | ConvertFrom-Json
$monitorConfig.Monitoring.ProcessPath = Join-Path $installDir "ContextSensor.App.exe"
$monitorConfig.Logging.LogPath = Join-Path $dataDir "logs\monitor.log"
$monitorConfig | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $installDir "monitor.config.json")

# Update updater config
$updaterConfig = Get-Content (Join-Path $installDir "updater.config.json") | ConvertFrom-Json
$updaterConfig.Update.UpdateCheckUrl = "http://localhost:8080/manifest.json"
$updaterConfig.Update.CheckIntervalHours = 0.017  # ~1 minute for testing
$updaterConfig.Logging.LogPath = Join-Path $dataDir "logs\updater.log"
$updaterConfig | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $installDir "updater.config.json")

Write-Host "Configuration updated!" -ForegroundColor Green
Write-Host ""

# Step 5: Create registry keys
Write-Host "Step 5: Creating registry keys..." -ForegroundColor Yellow
$regPath = "HKLM:\Software\ContextSensor"
if (-not (Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
}
Set-ItemProperty -Path $regPath -Name "InstallPath" -Value $installDir
Set-ItemProperty -Path $regPath -Name "Version" -Value $Version
Set-ItemProperty -Path $regPath -Name "DataPath" -Value $dataDir
Set-ItemProperty -Path $regPath -Name "UpdateUrl" -Value "http://localhost:8080/manifest.json"
Write-Host "Registry keys created!" -ForegroundColor Green
Write-Host ""

# Step 6: Install services
Write-Host "Step 6: Installing Windows Services..." -ForegroundColor Yellow

# Check if services exist
$monitorExists = Get-Service ContextSensor.Monitor -ErrorAction SilentlyContinue
$updaterExists = Get-Service ContextSensor.Updater -ErrorAction SilentlyContinue

# Remove existing services if they exist
if ($monitorExists) {
    Write-Host "Removing existing Monitor service..." -ForegroundColor Gray
    Stop-Service ContextSensor.Monitor -Force -ErrorAction SilentlyContinue
    sc.exe delete ContextSensor.Monitor | Out-Null
    Start-Sleep -Seconds 2
}

if ($updaterExists) {
    Write-Host "Removing existing Updater service..." -ForegroundColor Gray
    Stop-Service ContextSensor.Updater -Force -ErrorAction SilentlyContinue
    sc.exe delete ContextSensor.Updater | Out-Null
    Start-Sleep -Seconds 2
}

# Create Monitor service
Write-Host "Creating Monitor service..." -ForegroundColor Gray
sc.exe create ContextSensor.Monitor `
    binPath= "`"$installDir\ContextSensor.Monitor.exe`"" `
    start= auto `
    DisplayName= "ContextSensor Monitor Service" | Out-Null

sc.exe description ContextSensor.Monitor "Monitors and maintains the ContextSensor application" | Out-Null

# Create Updater service
Write-Host "Creating Updater service..." -ForegroundColor Gray
sc.exe create ContextSensor.Updater `
    binPath= "`"$installDir\ContextSensor.Updater.exe`"" `
    start= delayed-auto `
    DisplayName= "ContextSensor Update Service" | Out-Null

sc.exe description ContextSensor.Updater "Automatically checks for and installs updates" | Out-Null

Write-Host "Services created!" -ForegroundColor Green
Write-Host ""

# Step 7: Start services
Write-Host "Step 7: Starting services..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

Start-Service ContextSensor.Monitor
Write-Host "Monitor service started" -ForegroundColor Gray

Start-Service ContextSensor.Updater
Write-Host "Updater service started" -ForegroundColor Gray
Write-Host ""

# Step 8: Wait for app to start
Write-Host "Step 8: Waiting for application to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Verify
Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

Write-Host "Verification:" -ForegroundColor Cyan
Write-Host ""

# Check services
$monitor = Get-Service ContextSensor.Monitor
$updater = Get-Service ContextSensor.Updater
Write-Host "Monitor Service: $($monitor.Status)" -ForegroundColor $(if ($monitor.Status -eq 'Running') { 'Green' } else { 'Red' })
Write-Host "Updater Service: $($updater.Status)" -ForegroundColor $(if ($updater.Status -eq 'Running') { 'Green' } else { 'Red' })

# Check app
$app = Get-Process ContextSensor.App -ErrorAction SilentlyContinue
if ($app) {
    Write-Host "Application: Running (PID: $($app.Id))" -ForegroundColor Green
} else {
    Write-Host "Application: Not running (check logs)" -ForegroundColor Yellow
}

# Check version
$ver = Get-ItemProperty -Path "HKLM:\Software\ContextSensor" | Select-Object -ExpandProperty Version
Write-Host "Installed Version: $ver" -ForegroundColor Cyan

Write-Host ""
Write-Host "Directories:" -ForegroundColor Cyan
Write-Host "  Install: $installDir" -ForegroundColor Gray
Write-Host "  Data: $dataDir" -ForegroundColor Gray
Write-Host ""

Write-Host "Logs:" -ForegroundColor Cyan
Write-Host "  Monitor: $dataDir\logs\monitor.log" -ForegroundColor Gray
Write-Host "  Updater: $dataDir\logs\updater.log" -ForegroundColor Gray
Write-Host "  App: $dataDir\logs\contextsensor.log" -ForegroundColor Gray
Write-Host ""

Write-Host "Next steps for testing:" -ForegroundColor Yellow
Write-Host "1. Create test data:" -ForegroundColor White
Write-Host "   New-Item 'C:\ProgramData\ContextSensor\data\test.txt' -Value 'PRESERVE ME!' -Force" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Start update server (new window):" -ForegroundColor White
Write-Host "   cd test\UpdateServer" -ForegroundColor Cyan
Write-Host "   dotnet run" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Monitor update logs:" -ForegroundColor White
Write-Host "   Get-Content 'C:\ProgramData\ContextSensor\logs\updater.log' -Wait -Tail 20" -ForegroundColor Cyan
Write-Host ""