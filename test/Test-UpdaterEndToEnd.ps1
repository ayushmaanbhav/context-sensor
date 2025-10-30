<#
.SYNOPSIS
    End-to-end test for ContextSensor updater service
.DESCRIPTION
    This script:
    1. Starts the test update server
    2. Creates a test manifest
    3. Configures the updater to use localhost
    4. Monitors the update process
    5. Validates the update workflow
.PARAMETER TestVersion
    The version number to use for testing (default: 1.0.1)
.PARAMETER Port
    The port to run the test server on (default: 8000)
#>

param(
    [string]$TestVersion = "1.0.1",
    [int]$Port = 8000,
    [switch]$SkipServerStart
)

$ErrorActionPreference = "Stop"

# Color output functions
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }

Write-Info "=== ContextSensor Updater End-to-End Test ==="
Write-Info ""

# Step 1: Check if ContextSensor is installed
Write-Info "Step 1: Checking ContextSensor installation..."
$updaterService = Get-Service ContextSensor.Updater -ErrorAction SilentlyContinue
if (-not $updaterService) {
    Write-Error "ContextSensor.Updater service not found. Please install ContextSensor first."
    exit 1
}
Write-Success "  ✓ Updater service found"

# Get current version
$currentVersion = "1.0.0"
try {
    $regKey = Get-ItemProperty -Path "HKLM:\Software\ContextSensor" -ErrorAction SilentlyContinue
    if ($regKey.Version) {
        $currentVersion = $regKey.Version
    }
} catch {
    Write-Warning "  Could not read current version from registry, assuming 1.0.0"
}
Write-Info "  Current version: $currentVersion"

# Step 2: Set up test update server directory
Write-Info "`nStep 2: Setting up test update server..."
$testServerDir = Join-Path $PSScriptRoot "UpdateServer"
$manifestPath = Join-Path $testServerDir "manifest.json"

if (-not (Test-Path $testServerDir)) {
    New-Item -Path $testServerDir -ItemType Directory -Force | Out-Null
}
Write-Success "  ✓ Test server directory ready: $testServerDir"

# Step 3: Create fake MSI for testing
Write-Info "`nStep 3: Creating test MSI file..."
$testMsiPath = Join-Path $testServerDir "ContextSensor-$TestVersion.msi"
$testContent = "This is a fake MSI file for testing purposes - DO NOT USE IN PRODUCTION"
Set-Content -Path $testMsiPath -Value $testContent -Encoding ASCII
Write-Success "  ✓ Test MSI created: $testMsiPath"

# Step 4: Calculate checksum
Write-Info "`nStep 4: Calculating checksum..."
$hash = Get-FileHash -Path $testMsiPath -Algorithm SHA256
$checksum = "sha256:" + $hash.Hash.ToLower()
Write-Success "  ✓ Checksum: $checksum"

# Step 5: Create manifest
Write-Info "`nStep 5: Creating update manifest..."
$manifest = @{
    version = $TestVersion
    releaseDate = (Get-Date).ToUniversalTime().ToString("o")
    downloadUrl = "http://localhost:$Port/ContextSensor-$TestVersion.msi"
    checksum = $checksum
    releaseNotes = "Test update for end-to-end validation - NOT A REAL UPDATE"
    minimumVersion = "1.0.0"
    isMandatory = $false
} | ConvertTo-Json -Depth 10

Set-Content -Path $manifestPath -Value $manifest
Write-Success "  ✓ Manifest created: $manifestPath"
Write-Info "  Manifest content:"
Write-Host $manifest -ForegroundColor Gray

# Step 6: Start test update server
if (-not $SkipServerStart) {
    Write-Info "`nStep 6: Starting test update server..."
    
    # Check if UpdateServer project exists
    $updateServerProject = Join-Path $testServerDir "UpdateServer.csproj"
    
    if (Test-Path $updateServerProject) {
        Write-Info "  Using UpdateServer project..."
        
        # Start the server in background
        $serverJob = Start-Job -ScriptBlock {
            param($ProjectPath, $ServerPort)
            Set-Location (Split-Path $ProjectPath)
            dotnet run --urls "http://localhost:$ServerPort"
        } -ArgumentList $updateServerProject, $Port
        
        Write-Success "  ✓ Test server started (Job ID: $($serverJob.Id))"
        Write-Info "  Waiting for server to be ready..."
        Start-Sleep -Seconds 3
    } else {
        Write-Warning "  UpdateServer project not found, using simple HTTP server..."
        Write-Info "  Please start manually:"
        Write-Host "    cd $testServerDir" -ForegroundColor Yellow
        Write-Host "    python -m http.server $Port" -ForegroundColor Yellow
        Write-Host "  Or:" -ForegroundColor Yellow
        Write-Host "    dotnet serve -p $Port -d $testServerDir" -ForegroundColor Yellow
        Write-Host "`n  Press Enter after starting the server..." -ForegroundColor Yellow
        Read-Host
    }
} else {
    Write-Warning "`nStep 6: Skipping server start (--SkipServerStart specified)"
    Write-Info "  Make sure the server is running at http://localhost:$Port"
}

# Step 7: Test manifest accessibility
Write-Info "`nStep 7: Testing manifest accessibility..."
try {
    $testManifest = Invoke-RestMethod -Uri "http://localhost:$Port/manifest.json" -ErrorAction Stop
    Write-Success "  ✓ Manifest accessible at http://localhost:$Port/manifest.json"
    Write-Info "  Available version: $($testManifest.version)"
} catch {
    Write-Error "  ✗ Cannot access manifest. Is the server running?"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 8: Backup current updater configuration
Write-Info "`nStep 8: Backing up updater configuration..."
$updaterConfigPath = "C:\Program Files\ContextSensor\updater.config.json"
$backupConfigPath = "C:\Program Files\ContextSensor\updater.config.json.backup"

if (Test-Path $updaterConfigPath) {
    Copy-Item -Path $updaterConfigPath -Destination $backupConfigPath -Force
    Write-Success "  ✓ Configuration backed up to: $backupConfigPath"
} else {
    Write-Warning "  Updater config not found at expected location"
}

# Step 9: Configure updater for testing
Write-Info "`nStep 9: Configuring updater for test..."
$updaterConfig = @{
    Update = @{
        UpdateCheckUrl = "http://localhost:$Port/manifest.json"
        CheckIntervalHours = 0.05  # 3 minutes for testing
        AutoInstall = $false  # Manual approval for safety
        VerifyChecksum = $true
        BackupBeforeUpdate = $true
        AllowDowngrade = $false
        StartupDelayMinutes = 0.1
    }
} | ConvertTo-Json -Depth 10

if (Test-Path $updaterConfigPath) {
    Set-Content -Path $updaterConfigPath -Value $updaterConfig
    Write-Success "  ✓ Updater configured for test (AutoInstall=false for safety)"
} else {
    Write-Warning "  Could not update config file"
}

# Step 10: Restart updater service
Write-Info "`nStep 10: Restarting updater service..."
try {
    Restart-Service ContextSensor.Updater -Force
    Start-Sleep -Seconds 2
    $service = Get-Service ContextSensor.Updater
    if ($service.Status -eq 'Running') {
        Write-Success "  ✓ Updater service restarted successfully"
    } else {
        Write-Warning "  Service status: $($service.Status)"
    }
} catch {
    Write-Error "  Failed to restart service: $($_.Exception.Message)"
}

# Step 11: Monitor updater logs
Write-Info "`nStep 11: Monitoring updater logs..."
Write-Info "  Watching for update detection (this may take up to 3 minutes)..."
Write-Info "  Log location: C:\ProgramData\ContextSensor\logs\updater.log"
Write-Host ""

$logPath = "C:\ProgramData\ContextSensor\logs\updater.log"
$timeout = 240  # 4 minutes timeout
$elapsed = 0
$updateDetected = $false

while ($elapsed -lt $timeout -and -not $updateDetected) {
    if (Test-Path $logPath) {
        $recentLogs = Get-Content $logPath -Tail 20 -ErrorAction SilentlyContinue
        
        # Check for update detection
        if ($recentLogs -match "New version available") {
            Write-Success "`n  ✓ UPDATE DETECTED!"
            $updateDetected = $true
            
            # Show relevant log lines
            Write-Host "`n  Recent log entries:" -ForegroundColor Cyan
            $recentLogs | Select-Object -Last 10 | ForEach-Object {
                if ($_ -match "version|update|download") {
                    Write-Host "    $_" -ForegroundColor Yellow
                } else {
                    Write-Host "    $_" -ForegroundColor Gray
                }
            }
            break
        }
    }
    
    Start-Sleep -Seconds 5
    $elapsed += 5
    Write-Host "." -NoNewline
}

if (-not $updateDetected) {
    Write-Warning "`n`n  Update not detected within timeout period"
    Write-Info "  Check logs manually: Get-Content '$logPath' -Tail 50"
}

# Step 12: Summary and cleanup instructions
Write-Info "`n`n=== Test Summary ==="
Write-Info ""
Write-Info "Test Configuration:"
Write-Info "  Current Version: $currentVersion"
Write-Info "  Test Version: $TestVersion"  
Write-Info "  Manifest URL: http://localhost:$Port/manifest.json"
Write-Info "  AutoInstall: false (safe mode)"
Write-Info ""

if ($updateDetected) {
    Write-Success "✓ Updater service successfully detected the update!"
    Write-Info ""
    Write-Info "What happened:"
    Write-Success "  1. Updater checked manifest at localhost:$Port"
    Write-Success "  2. Detected version $TestVersion > $currentVersion"
    Write-Success "  3. Logged 'New version available' message"
    Write-Info ""
    Write-Warning "Note: AutoInstall is disabled, so update was NOT installed"
    Write-Info "This is intentional for safety during testing."
} else {
    Write-Warning "⚠ Update detection timeout - check logs"
}

Write-Info "`nCleanup Instructions:"
Write-Info "  1. Stop test server (if running as job):"
Write-Host "     Get-Job | Stop-Job; Get-Job | Remove-Job" -ForegroundColor Yellow
Write-Info ""
Write-Info "  2. Restore original updater config:"
Write-Host "     Copy-Item '$backupConfigPath' '$updaterConfigPath' -Force" -ForegroundColor Yellow
Write-Host "     Restart-Service ContextSensor.Updater" -ForegroundColor Yellow
Write-Info ""
Write-Info "  3. Clean up test files:"
Write-Host "     Remove-Item '$testServerDir\ContextSensor-*.msi'" -ForegroundColor Yellow
Write-Host "     Remove-Item '$testServerDir\manifest.json'" -ForegroundColor Yellow
Write-Info ""

Write-Info "To view updater logs:"
Write-Host "  Get-Content 'C:\ProgramData\ContextSensor\logs\updater.log' -Tail 50" -ForegroundColor Yellow
Write-Info ""

Write-Info "Test completed!"