# ContextSensor Testing Guide

This comprehensive guide covers all testing procedures for the ContextSensor application, including installer testing, performance testing, resource usage monitoring, and auto-update service validation.

## Table of Contents

1. [Overview](#overview)
2. [Test Projects](#test-projects)
3. [Test Environment Setup](#test-environment-setup)
4. [Pre-Test Checklist](#pre-test-checklist)
5. [Manual Testing Procedures](#manual-testing-procedures)
6. [Automated Test Scripts](#automated-test-scripts)
7. [Test Scenarios](#test-scenarios)
8. [Verification Procedures](#verification-procedures)
9. [Common Test Issues](#common-test-issues)
10. [Test Reporting](#test-reporting)

## Overview

The ContextSensor testing strategy includes multiple test types to ensure comprehensive coverage:

### Test Categories

| Category | Purpose | Location | Documentation |
|----------|---------|----------|---------------|
| **Installer Tests** | MSI package installation, upgrade, uninstall | [`test/`](../../test/) | This guide |
| **Performance Tests** | Throughput, latency, scalability benchmarks | [`test/ContextSensor.PerformanceTests/`](../../test/ContextSensor.PerformanceTests/) | [Performance Testing Guide](PERFORMANCE_TESTING.md) |
| **Resource Usage Tests** | Memory, CPU, handles, threads monitoring | [`test/ContextSensor.PerformanceTests/`](../../test/ContextSensor.PerformanceTests/) | [Performance Testing Guide](PERFORMANCE_TESTING.md) |
| **Updater Tests** | Auto-update functionality, manifest parsing | [`test/ContextSensor.UpdaterTests/`](../../test/ContextSensor.UpdaterTests/) | [Updater Testing Guide](UPDATER_TESTING.md) |
| **Integration Tests** | End-to-end workflows, service interactions | Distributed across test projects | This guide + specific guides |

### Testing Philosophy

Our testing approach follows these principles:

1. **Comprehensive Coverage**: Test all critical functionality
2. **Automated Where Possible**: Reduce manual testing burden
3. **Fast Feedback**: Quick test execution for rapid iteration
4. **Production-Like Environments**: Test in realistic conditions
5. **Continuous Testing**: Integrate tests into CI/CD pipeline

## Test Projects

### 1. ContextSensor.PerformanceTests

**Location**: [`test/ContextSensor.PerformanceTests/`](../../test/ContextSensor.PerformanceTests/)

**Purpose**: Performance benchmarking and resource usage monitoring

**Key Features**:
- BenchmarkDotNet micro-benchmarks
- Event processing performance tests
- Memory usage tracking
- CPU usage monitoring
- Resource leak detection
- Throughput and latency measurements

**Technologies**:
- BenchmarkDotNet for micro-benchmarks
- xUnit for unit tests
- FluentAssertions for readable assertions
- System.Diagnostics.PerformanceCounter for metrics

**Quick Start**:
```powershell
cd test/ContextSensor.PerformanceTests

# Run all performance tests
dotnet test -c Release

# Run benchmarks
dotnet run -c Release --benchmark
```

**See**: [Performance Testing Guide](PERFORMANCE_TESTING.md) for detailed information

### 2. ContextSensor.UpdaterTests

**Location**: [`test/ContextSensor.UpdaterTests/`](../../test/ContextSensor.UpdaterTests/)

**Purpose**: Auto-update service functionality testing

**Key Features**:
- Update manifest parsing
- Version comparison logic
- Download and checksum verification
- Update installation workflow
- Error handling and recovery

**Technologies**:
- xUnit for unit tests
- Moq for mocking HTTP clients
- FluentAssertions for assertions

**Quick Start**:
```powershell
cd test/ContextSensor.UpdaterTests

# Run all updater tests
dotnet test -c Release
```

**See**: [Updater Testing Guide](UPDATER_TESTING.md) for detailed information

### 3. Manual Installer Tests

**Location**: [`test/`](../../test/) - PowerShell scripts and documentation

**Purpose**: MSI installer package validation

**Key Features**:
- Installation verification
- Upgrade path testing
- Service lifecycle testing
- Uninstall cleanup validation

**See**: Sections below for detailed procedures

## Test Environment Setup

### Required Test Machines

**Minimum Test Matrix**:
- Windows 10 (21H2 or later) - Clean VM
- Windows 11 (22H2 or later) - Clean VM
- Windows 10 with existing installation - For upgrade testing
- Windows 11 with existing installation - For upgrade testing

**Recommended Tools**:
- Hyper-V or VMware for VMs
- VM snapshots for quick rollback
- PowerShell 7+
- Remote Desktop for accessing test VMs

### VM Preparation

```powershell
# Create clean Windows 10/11 VM
# 1. Install Windows Updates
# 2. Create baseline snapshot
# 3. Install .NET 8.0 Runtime (for positive tests)
# 4. Create another snapshot

# VM Specifications (Minimum)
# - CPU: 2 cores
# - RAM: 4 GB
# - Disk: 60 GB
# - Network: Enabled
```

### Test Environment Checklist

- [ ] Clean Windows 10 VM ready
- [ ] Clean Windows 11 VM ready
- [ ] .NET 8.0 Runtime available for installation
- [ ] MSI package built and ready
- [ ] Network share accessible (for enterprise testing)
- [ ] Test update server configured (for update testing)
- [ ] PowerShell execution policy configured: `Set-ExecutionPolicy RemoteSigned`

## Pre-Test Checklist

### Before Testing

- [ ] Built MSI package in Release mode
- [ ] Verified MSI file size (should be 10-50 MB)
- [ ] Calculated SHA256 checksum
- [ ] Reviewed installation logs from previous tests
- [ ] Prepared test data/scenarios
- [ ] Created test update manifest (for update testing)
- [ ] Documented expected behavior
- [ ] Prepared rollback plan

### Build Verification

```powershell
# Verify MSI was built successfully
$msiPath = "installer\bin\Release\ContextSensor.msi"
Test-Path $msiPath

# Check file size
(Get-Item $msiPath).Length / 1MB

# Calculate checksum
Get-FileHash $msiPath -Algorithm SHA256
```

## Manual Testing Procedures

### Test 1: Fresh Installation on Windows 10

**Objective**: Verify clean installation on Windows 10

**Prerequisites**:
- Clean Windows 10 VM (snapshot)
- .NET 8.0 Runtime installed
- Administrator access

**Steps**:

1. **Copy MSI to test machine**
   ```powershell
   Copy-Item .\ContextSensor.msi -Destination C:\Temp\
   ```

2. **Launch installer**
   ```powershell
   Start-Process C:\Temp\ContextSensor.msi
   ```

3. **Follow wizard**:
   - Click "Next" on Welcome screen
   - Accept license agreement
   - Choose installation directory (use default)
   - Click "Install"
   - Wait for completion
   - Click "Finish"

4. **Verify installation** (see [Verification Procedures](#verification-procedures))

**Expected Results**:
- ✅ Installation completes without errors
- ✅ Services are installed and running
- ✅ Main application starts automatically
- ✅ Start Menu shortcuts created
- ✅ Registry keys created
- ✅ Data directories created

**Test Duration**: ~5 minutes

---

### Test 2: Fresh Installation on Windows 11

**Objective**: Verify clean installation on Windows 11

**Steps**: Same as Test 1, but on Windows 11 VM

**Expected Results**: Same as Test 1

---

### Test 3: Silent Installation

**Objective**: Verify silent installation works correctly

**Prerequisites**: Clean Windows 10/11 VM

**Steps**:

```powershell
# Silent install with logging
msiexec /i C:\Temp\ContextSensor.msi /quiet /norestart /l*v C:\Temp\install.log

# Wait for completion (check Process)
Get-Process msiexec

# Once complete, verify installation
Start-Sleep -Seconds 30
Get-Service ContextSensor.*
```

**Expected Results**:
- ✅ Installation completes silently (no UI)
- ✅ Exit code is 0 (success)
- ✅ Services installed and running
- ✅ No user interaction required
- ✅ Log file created with no errors

**Test Duration**: ~3 minutes

---

### Test 4: Installation Without .NET Runtime

**Objective**: Verify .NET prerequisite check

**Prerequisites**: Clean VM WITHOUT .NET 8.0 Runtime

**Steps**:

1. Launch installer
2. Proceed through wizard

**Expected Results**:
- ✅ Installer detects missing .NET Runtime
- ✅ Error message displayed
- ✅ Installation halts
- ✅ User directed to download .NET

**Test Duration**: ~2 minutes

---

### Test 5: Upgrade from Previous Version

**Objective**: Verify upgrade preserves data

**Prerequisites**: 
- VM with v1.0.0 installed
- Test data in `C:\ProgramData\ContextSensor\data\`
- Custom configuration in config files

**Setup**:
```powershell
# Create test data
New-Item -Path "C:\ProgramData\ContextSensor\data\test-events.log" -ItemType File -Force
Set-Content -Path "C:\ProgramData\ContextSensor\data\test-events.log" -Value "Test data before upgrade"

# Note current version
Get-ItemProperty -Path "HKLM:\Software\ContextSensor" | Select-Object Version
```

**Steps**:

1. Build v1.0.1 MSI (update version in Product.wxs)
2. Run new installer
3. Proceed with upgrade

**Expected Results**:
- ✅ Installer detects existing version
- ✅ Upgrade proceeds without uninstall prompt
- ✅ Services stopped during upgrade
- ✅ Data files preserved
- ✅ Log files preserved
- ✅ Custom configurations preserved (merged)
- ✅ Services restarted after upgrade
- ✅ Registry updated with new version
- ✅ Old program files replaced

**Verification**:
```powershell
# Verify data preserved
Get-Content "C:\ProgramData\ContextSensor\data\test-events.log"

# Verify version updated
Get-ItemProperty -Path "HKLM:\Software\ContextSensor" | Select-Object Version

# Verify services running
Get-Service ContextSensor.* | Format-Table Name, Status
```

**Test Duration**: ~8 minutes

---

### Test 6: Service Auto-Start After Reboot

**Objective**: Verify services start automatically after system reboot

**Prerequisites**: Installed ContextSensor

**Steps**:

1. **Before reboot**:
   ```powershell
   # Verify services are running
   Get-Service ContextSensor.* | Format-Table Name, Status, StartType
   ```

2. **Reboot machine**:
   ```powershell
   Restart-Computer -Force
   ```

3. **After reboot** (wait 2 minutes):
   ```powershell
   # Verify services started automatically
   Get-Service ContextSensor.* | Format-Table Name, Status, StartType
   
   # Verify app is running
   Get-Process ContextSensor.App -ErrorAction SilentlyContinue
   ```

**Expected Results**:
- ✅ Monitor service starts automatically (Automatic)
- ✅ Updater service starts automatically (Automatic-Delayed)
- ✅ Monitor service starts the main application
- ✅ All services in "Running" state

**Test Duration**: ~10 minutes

---

### Test 7: Application Crash Recovery

**Objective**: Verify Monitor service restarts crashed application

**Prerequisites**: Installed and running ContextSensor

**Steps**:

1. **Verify app is running**:
   ```powershell
   Get-Process ContextSensor.App | Format-Table Name, Id, StartTime
   ```

2. **Kill application process**:
   ```powershell
   Stop-Process -Name ContextSensor.App -Force
   ```

3. **Wait and verify restart**:
   ```powershell
   # Wait for monitor to detect and restart (check interval + restart delay)
   Start-Sleep -Seconds 45
   
   # Verify app restarted
   Get-Process ContextSensor.App | Format-Table Name, Id, StartTime
   ```

4. **Check monitor logs**:
   ```powershell
   Get-Content "C:\ProgramData\ContextSensor\logs\monitor.log" -Tail 20
   ```

**Expected Results**:
- ✅ Monitor service detects app stopped
- ✅ App automatically restarted within ~40 seconds
- ✅ New process ID assigned
- ✅ Restart logged in monitor.log
- ✅ App functions normally after restart

**Test Duration**: ~3 minutes

---

### Test 8: Multiple Crash Recovery

**Objective**: Verify restart backoff and rate limiting

**Prerequisites**: Installed ContextSensor

**Steps**:

```powershell
# Kill app multiple times rapidly
1..5 | ForEach-Object {
    Write-Host "Crash attempt $_"
    Stop-Process -Name ContextSensor.App -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 15
    
    # Check if running
    $proc = Get-Process ContextSensor.App -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "Restarted after attempt $_"
    } else {
        Write-Host "Not running after attempt $_"
    }
}

# Review logs
Get-Content "C:\ProgramData\ContextSensor\logs\monitor.log" -Tail 50
```

**Expected Results**:
- ✅ First restart: immediate
- ✅ Second restart: ~10s delay
- ✅ Third restart: ~20s delay
- ✅ After max attempts: stops restarting
- ✅ Rate limiting prevents excessive restarts
- ✅ All attempts logged

**Test Duration**: ~5 minutes

---

### Test 9: Update Check and Installation

**Objective**: Verify auto-update functionality

**Prerequisites**: 
- Installed ContextSensor v1.0.0
- Test update server with manifest
- MSI v1.0.1 available

**Setup**:

1. **Create test manifest**:
   ```json
   {
     "version": "1.0.1",
     "releaseDate": "2025-11-01T00:00:00Z",
     "downloadUrl": "http://localhost:8000/ContextSensor-1.0.1.msi",
     "checksum": "sha256:ACTUAL_CHECKSUM_HERE",
     "releaseNotes": "Test update"
   }
   ```

2. **Host files locally**:
   ```powershell
   # Start simple HTTP server
   cd C:\TestUpdateServer
   python -m http.server 8000
   # Or: dotnet serve -p 8000
   ```

3. **Configure updater**:
   ```powershell
   # Edit updater config
   $config = Get-Content "C:\Program Files\ContextSensor\updater.config.json" | ConvertFrom-Json
   $config.Update.UpdateCheckUrl = "http://localhost:8000/manifest.json"
   $config.Update.CheckIntervalHours = 0.05  # 3 minutes for testing
   $config | ConvertTo-Json -Depth 10 | Set-Content "C:\Program Files\ContextSensor\updater.config.json"
   
   # Restart updater
   Restart-Service ContextSensor.Updater
   ```

**Steps**:

1. **Monitor updater logs**:
   ```powershell
   Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Wait -Tail 20
   ```

2. **Wait for update check** (~3-5 minutes)

3. **Verify update detected and installed**

**Expected Results**:
- ✅ Updater detects new version
- ✅ MSI downloaded to temp directory
- ✅ Checksum verified
- ✅ Services stopped before install
- ✅ Update installed silently
- ✅ Services restarted automatically
- ✅ Version updated in registry
- ✅ Data preserved
- ✅ Update logged

**Test Duration**: ~10 minutes

---

### Test 10: Uninstallation

**Objective**: Verify clean uninstallation

**Prerequisites**: Installed ContextSensor

**Steps**:

1. **Note current state**:
   ```powershell
   # Check what exists
   Test-Path "C:\Program Files\ContextSensor"
   Test-Path "C:\ProgramData\ContextSensor"
   Get-Service ContextSensor.*
   Get-ItemProperty -Path "HKLM:\Software\ContextSensor"
   ```

2. **Uninstall via Programs & Features** (or silent):
   ```powershell
   msiexec /x ContextSensor.msi /quiet /norestart /l*v C:\Temp\uninstall.log
   ```

3. **Verify removal**:
   ```powershell
   # Program Files should be removed
   Test-Path "C:\Program Files\ContextSensor"  # Should be False
   
   # Services should be removed
   Get-Service ContextSensor.* -ErrorAction SilentlyContinue  # Should error
   
   # Registry should be removed
   Test-Path "HKLM:\Software\ContextSensor"  # Should be False
   
   # ProgramData preserved (by design)
   Test-Path "C:\ProgramData\ContextSensor"  # Should be True
   ```

**Expected Results**:
- ✅ Uninstallation completes successfully
- ✅ Services stopped and removed
- ✅ Program Files removed
- ✅ Registry keys removed
- ✅ Start Menu shortcuts removed
- ✅ ProgramData directory preserved (contains logs/data)
- ✅ No orphaned files
- ✅ Clean uninstall log

**Test Duration**: ~5 minutes

---

## Automated Test Scripts

### Complete Installation Test Script

Save as `Test-Installation.ps1`:

```powershell
<#
.SYNOPSIS
    Automated ContextSensor installation test
.DESCRIPTION
    Tests installation, service startup, and basic functionality
#>

param(
    [string]$MsiPath = ".\ContextSensor.msi",
    [switch]$Silent
)

$ErrorActionPreference = "Stop"
$testResults = @()

function Test-Result {
    param($Name, $Test, $Expected)
    $result = [PSCustomObject]@{
        Test = $Name
        Expected = $Expected
        Actual = $Test
        Result = if ($Test -eq $Expected) { "PASS" } else { "FAIL" }
        Timestamp = Get-Date
    }
    $testResults += $result
    
    $color = if ($result.Result -eq "PASS") { "Green" } else { "Red" }
    Write-Host "[$($result.Result)] $Name" -ForegroundColor $color
    
    return $result
}

Write-Host "=== ContextSensor Installation Test ===" -ForegroundColor Cyan
Write-Host "MSI Path: $MsiPath"
Write-Host ""

# Test 1: MSI file exists
Write-Host "Test 1: MSI file exists" -ForegroundColor Yellow
Test-Result "MSI File Exists" (Test-Path $MsiPath) $true

# Test 2: Install MSI
Write-Host "`nTest 2: Installing MSI" -ForegroundColor Yellow
$installArgs = "/i `"$MsiPath`" /quiet /norestart /l*v C:\Temp\install-test.log"
$process = Start-Process msiexec -ArgumentList $installArgs -Wait -PassThru
Test-Result "Installation Exit Code" $process.ExitCode 0

# Wait for services to start
Start-Sleep -Seconds 10

# Test 3: Services installed
Write-Host "`nTest 3: Services installed" -ForegroundColor Yellow
$monitorService = Get-Service ContextSensor.Monitor -ErrorAction SilentlyContinue
$updaterService = Get-Service ContextSensor.Updater -ErrorAction SilentlyContinue
Test-Result "Monitor Service Exists" ($null -ne $monitorService) $true
Test-Result "Updater Service Exists" ($null -ne $updaterService) $true

# Test 4: Services running
Write-Host "`nTest 4: Services running" -ForegroundColor Yellow
Test-Result "Monitor Service Running" ($monitorService.Status -eq 'Running') $true
Test-Result "Updater Service Running" ($updaterService.Status -eq 'Running') $true

# Test 5: Application running
Write-Host "`nTest 5: Application running" -ForegroundColor Yellow
Start-Sleep -Seconds 5
$appProcess = Get-Process ContextSensor.App -ErrorAction SilentlyContinue
Test-Result "Application Process Running" ($null -ne $appProcess) $true

# Test 6: Files exist
Write-Host "`nTest 6: Files installed" -ForegroundColor Yellow
Test-Result "Program Files Directory" (Test-Path "C:\Program Files\ContextSensor") $true
Test-Result "Main App Executable" (Test-Path "C:\Program Files\ContextSensor\ContextSensor.App.exe") $true
Test-Result "Monitor Executable" (Test-Path "C:\Program Files\ContextSensor\ContextSensor.Monitor.exe") $true
Test-Result "Updater Executable" (Test-Path "C:\Program Files\ContextSensor\ContextSensor.Updater.exe") $true

# Test 7: Data directory exists
Write-Host "`nTest 7: Data directories" -ForegroundColor Yellow
Test-Result "ProgramData Directory" (Test-Path "C:\ProgramData\ContextSensor") $true
Test-Result "Data Directory" (Test-Path "C:\ProgramData\ContextSensor\data") $true
Test-Result "Logs Directory" (Test-Path "C:\ProgramData\ContextSensor\logs") $true

# Test 8: Registry keys
Write-Host "`nTest 8: Registry configuration" -ForegroundColor Yellow
$regPath = "HKLM:\Software\ContextSensor"
Test-Result "Registry Key Exists" (Test-Path $regPath) $true
$regProps = Get-ItemProperty -Path $regPath
Test-Result "InstallPath Set" ($null -ne $regProps.InstallPath) $true
Test-Result "Version Set" ($null -ne $regProps.Version) $true

# Test 9: Start Menu shortcuts
Write-Host "`nTest 9: Start Menu shortcuts" -ForegroundColor Yellow
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\ContextSensor"
Test-Result "Start Menu Folder" (Test-Path $startMenuPath) $true

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
$passed = ($testResults | Where-Object { $_.Result -eq "PASS" }).Count
$failed = ($testResults | Where-Object { $_.Result -eq "FAIL" }).Count
$total = $testResults.Count

Write-Host "Total: $total | Passed: $passed | Failed: $failed"
Write-Host ""

if ($failed -eq 0) {
    Write-Host "All tests PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some tests FAILED!" -ForegroundColor Red
    $testResults | Where-Object { $_.Result -eq "FAIL" } | Format-Table -AutoSize
    exit 1
}
```

**Usage**:
```powershell
.\Test-Installation.ps1 -MsiPath ".\ContextSensor.msi"
```

---

### Service Monitoring Test Script

Save as `Test-Monitoring.ps1`:

```powershell
<#
.SYNOPSIS
    Test ContextSensor monitoring service
.DESCRIPTION
    Tests automatic restart functionality
#>

Write-Host "=== Monitor Service Test ===" -ForegroundColor Cyan

# Check initial state
Write-Host "`n1. Checking initial state..." -ForegroundColor Yellow
$initialProcess = Get-Process ContextSensor.App -ErrorAction SilentlyContinue
if ($initialProcess) {
    Write-Host "   App running with PID: $($initialProcess.Id)" -ForegroundColor Green
    $initialPid = $initialProcess.Id
} else {
    Write-Host "   App not running!" -ForegroundColor Red
    exit 1
}

# Kill process
Write-Host "`n2. Simulating crash (killing process)..." -ForegroundColor Yellow
Stop-Process -Id $initialPid -Force
Write-Host "   Process killed" -ForegroundColor Green

# Wait for restart
Write-Host "`n3. Waiting for automatic restart (45 seconds)..." -ForegroundColor Yellow
$timeout = 60
$elapsed = 0
$restarted = $false

while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 5
    $elapsed += 5
    
    $newProcess = Get-Process ContextSensor.App -ErrorAction SilentlyContinue
    if ($newProcess -and $newProcess.Id -ne $initialPid) {
        Write-Host "   App restarted with new PID: $($newProcess.Id)" -ForegroundColor Green
        $restarted = $true
        break
    }
    
    Write-Host "   Waiting... ($elapsed seconds)" -ForegroundColor Gray
}

if (-not $restarted) {
    Write-Host "   FAILED: App did not restart within $timeout seconds" -ForegroundColor Red
    exit 1
}

# Check logs
Write-Host "`n4. Checking monitor logs..." -ForegroundColor Yellow
$logPath = "C:\ProgramData\ContextSensor\logs\monitor.log"
$recentLogs = Get-Content $logPath -Tail 20
$restartLogs = $recentLogs | Select-String "restart"
if ($restartLogs) {
    Write-Host "   Found restart entries in logs:" -ForegroundColor Green
    $restartLogs | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
} else {
    Write-Host "   No restart entries found (may be expected)" -ForegroundColor Yellow
}

Write-Host "`n=== Test PASSED ===" -ForegroundColor Green
Write-Host "Monitor service successfully restarted the application`n"
```

**Usage**:
```powershell
.\Test-Monitoring.ps1
```

---

## Test Scenarios

### Scenario Matrix

| Scenario | OS | .NET | Result | Priority |
|----------|----|----- |--------|----------|
| Fresh install | Win10 | Yes | Should succeed | High |
| Fresh install | Win11 | Yes | Should succeed | High |
| Fresh install | Win10 | No | Should fail with message | High |
| Silent install | Win10 | Yes | Should succeed | High |
| Upgrade | Win10 | Yes | Should preserve data | High |
| Reinstall | Win10 | Yes | Should work | Medium |
| Uninstall | Win10 | N/A | Should clean up | High |
| Crash recovery | Win10 | Yes | Should restart app | High |
| Update install | Win10 | Yes | Should auto-update | High |
| Reboot test | Win10 | Yes | Should auto-start | Medium |
| Multi-crash | Win10 | Yes | Should apply backoff | Medium |
| Custom path | Win10 | Yes | Should install to custom | Low |
| Network install | Win10 | Yes | Should install from UNC | Low |

---

## Verification Procedures

### Post-Installation Verification

```powershell
# Complete verification script
function Test-Installation {
    $results = @{
        Services = $false
        Application = $false
        Files = $false
        Registry = $false
        Shortcuts = $false
        Logs = $false
    }
    
    # Check services
    try {
        $monitor = Get-Service ContextSensor.Monitor
        $updater = Get-Service ContextSensor.Updater
        $results.Services = ($monitor.Status -eq 'Running' -and $updater.Status -eq 'Running')
    } catch {
        $results.Services = $false
    }
    
    # Check application
    $results.Application = (Get-Process ContextSensor.App -ErrorAction SilentlyContinue) -ne $null
    
    # Check files
    $files = @(
        "C:\Program Files\ContextSensor\ContextSensor.App.exe",
        "C:\Program Files\ContextSensor\ContextSensor.Monitor.exe",
        "C:\Program Files\ContextSensor\ContextSensor.Updater.exe",
        "C:\Program Files\ContextSensor\appsettings.json"
    )
    $results.Files = ($files | ForEach-Object { Test-Path $_ } | Where-Object { -not $_ }).Count -eq 0
    
    # Check registry
    $results.Registry = Test-Path "HKLM:\Software\ContextSensor"
    
    # Check shortcuts
    $results.Shortcuts = Test-Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\ContextSensor"
    
    # Check logs
    $results.Logs = Test-Path "C:\ProgramData\ContextSensor\logs"
    
    # Display results
    Write-Host "`n=== Installation Verification ===" -ForegroundColor Cyan
    $results.GetEnumerator() | ForEach-Object {
        $status = if ($_.Value) { "OK" } else { "FAIL" }
        $color = if ($_.Value) { "Green" } else { "Red" }
        Write-Host ("{0,-20} : {1}" -f $_.Key, $status) -ForegroundColor $color
    }
    
    $allPassed = ($results.Values | Where-Object { -not $_ }).Count -eq 0
    return $allPassed
}

# Run verification
if (Test-Installation) {
    Write-Host "`nInstallation verification PASSED" -ForegroundColor Green
} else {
    Write-Host "`nInstallation verification FAILED" -ForegroundColor Red
}
```

---

## Common Test Issues

### Issue 1: Services Not Starting

**Symptoms**: Services installed but status is "Stopped"

**Diagnosis**:
```powershell
# Check service configuration
sc.exe query ContextSensor.Monitor
sc.exe query ContextSensor.Updater

# Check event logs
Get-EventLog -LogName Application -Source ContextSensor.* -Newest 10

# Check service logs
Get-Content "C:\ProgramData\ContextSensor\logs\monitor.log" -Tail 50
```

**Common Causes**:
- Missing .NET Runtime
- Configuration file errors
- Permission issues
- Port conflicts

**Solutions**:
- Install .NET 8.0 Runtime
- Validate JSON configuration
- Run as Administrator
- Check firewall/antivirus

---

### Issue 2: Application Not Starting

**Symptoms**: Monitor service running but app doesn't start

**Diagnosis**:
```powershell
# Check ProcessPath in config
Get-Content "C:\Program Files\ContextSensor\monitor.config.json"

# Try manual start
& "C:\Program Files\ContextSensor\ContextSensor.App.exe"

# Check app logs
Get-Content "C:\ProgramData\ContextSensor\logs\contextsensor.log" -Tail 50
```

**Solutions**:
- Verify ProcessPath is correct
- Check app dependencies
- Review app configuration

---

### Issue 3: Update Not Installing

**Symptoms**: Updater detects update but doesn't install

**Diagnosis**:
```powershell
# Check updater logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 100

# Verify manifest accessible
Invoke-RestMethod -Uri "https://your-server.com/manifest.json"

# Check temp directory
Get-ChildItem "C:\ProgramData\ContextSensor\temp\updates"
```

**Common Causes**:
- Network issues
- Checksum mismatch
- Insufficient disk space
- Permission issues

---

## Test Reporting

### Test Report Template

```markdown
# Test Report: ContextSensor Installer

**Date**: 2025-10-31
**Tester**: [Name]
**Version**: 1.0.0
**Environment**: Windows 10 21H2

## Test Results

| Test ID | Test Name | Result | Notes |
|---------|-----------|--------|-------|
| T001 | Fresh Install Win10 | PASS | Completed in 4 minutes |
| T002 | Fresh Install Win11 | PASS | No issues |
| T003 | Silent Install | PASS | Exit code 0 |
| T004 | No .NET Runtime | PASS | Correct error message |
| T005 | Upgrade Test | PASS | Data preserved |
| T006 | Reboot Test | PASS | Services auto-started |
| T007 | Crash Recovery | PASS | Restarted in 35 seconds |
| T008 | Multi-Crash | PASS | Backoff working |
| T009 | Update Test | PASS | Auto-updated successfully |
| T010 | Uninstall | PASS | Clean removal |

## Summary

- **Total Tests**: 10
- **Passed**: 10
- **Failed**: 0
- **Pass Rate**: 100%

## Issues Found

None

## Recommendations

Ready for production deployment.
```

---

## Continuous Testing

### Daily Smoke Test

```powershell
# Quick smoke test - run daily
& ".\Test-Installation.ps1" -MsiPath ".\ContextSensor.msi"
if ($LASTEXITCODE -eq 0) {
    Write-Host "Daily smoke test PASSED" -ForegroundColor Green
} else {
    Write-Host "Daily smoke test FAILED - investigate!" -ForegroundColor Red
    # Send alert email
}
```

### Regression Test Suite

Before each release, run complete test suite:

1. Fresh installation tests (Win10, Win11)
2. Upgrade tests
3. Service functionality tests
4. Update mechanism tests
5. Uninstallation tests
6. Performance tests
7. Security tests

---

## Conclusion

This testing guide provides comprehensive coverage of the ContextSensor installer package. Follow these procedures before each release to ensure quality and reliability.

**Key Testing Principles**:
- Test on clean VMs
- Use snapshots for quick rollback
- Automate where possible
- Document all findings
- Test edge cases
- Verify on both Windows 10 and 11

For issues or questions, refer to:
- [Installer Guide](../installer/GUIDE.md) - Complete installation and deployment guide
- [Update Manifest Reference](../installer/UPDATE_MANIFEST_REFERENCE.md) - Update manifest format
- Service logs in `C:\ProgramData\ContextSensor\logs\`


## Running All Tests

### Complete Test Suite

Run all tests across all projects:

```powershell
# From repository root
dotnet test -c Release

# With test results output
dotnet test -c Release --logger "trx;LogFileName=test-results.trx"

# With coverage
dotnet test -c Release /p:CollectCoverage=true
```

### Project-Specific Test Runs

```powershell
# Performance tests only
dotnet test test/ContextSensor.PerformanceTests -c Release

# Updater tests only
dotnet test test/ContextSensor.UpdaterTests -c Release

# Run benchmarks
cd test/ContextSensor.PerformanceTests
dotnet run -c Release --benchmark
```

### Continuous Integration

Example Azure DevOps pipeline:

```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'windows-latest'

steps:
  # Build
  - task: DotNetCoreCLI@2
    displayName: 'Build Solution'
    inputs:
      command: 'build'
      projects: '**/*.csproj'
      arguments: '-c Release'

  # Performance Tests
  - task: DotNetCoreCLI@2
    displayName: 'Run Performance Tests'
    inputs:
      command: 'test'
      projects: 'test/ContextSensor.PerformanceTests/*.csproj'
      arguments: '-c Release --logger trx --no-build'

  # Updater Tests
  - task: DotNetCoreCLI@2
    displayName: 'Run Updater Tests'
    inputs:
      command: 'test'
      projects: 'test/ContextSensor.UpdaterTests/*.csproj'
      arguments: '-c Release --logger trx --no-build'

  # Publish Results
  - task: PublishTestResults@2
    displayName: 'Publish Test Results'
    inputs:
      testResultsFormat: 'VSTest'
      testResultsFiles: '**/*.trx'
      mergeTestResults: true
```

## Test Quality Metrics

### Coverage Targets

| Project | Target Coverage | Notes |
|---------|----------------|-------|
| ContextSensor.Core | > 80% | Critical business logic |
| ContextSensor.App | > 70% | Main application |
| ContextSensor.Updater | > 85% | Update service |
| ContextSensor.Monitor | > 75% | Monitoring service |

### Performance Benchmarks

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| Event Processing | > 1000/s | < 800/s | < 500/s |
| Event Latency (avg) | < 1ms | > 2ms | > 5ms |
| Memory Usage | < 100MB | > 200MB | > 500MB |
| CPU Usage (idle) | < 2% | > 5% | > 10% |

## Documentation Index

### Quick Reference

| Topic | Document | Description |
|-------|----------|-------------|
| Performance Testing | [PERFORMANCE_TESTING.md](PERFORMANCE_TESTING.md) | Benchmarks, resource usage, optimization |
| Updater Testing | [UPDATER_TESTING.md](UPDATER_TESTING.md) | Auto-update, version management, manifest handling |
| Installer Testing | This guide | MSI installation, upgrade, service lifecycle |
| Installer Creation | [../installer/GUIDE.md](../installer/GUIDE.md) | Building and deploying installers |
| Update Manifests | [../installer/UPDATE_MANIFEST_REFERENCE.md](../installer/UPDATE_MANIFEST_REFERENCE.md) | Manifest format and schema |

### Complete Testing Workflow

Before each release, ensure all test categories pass:

1. **Unit Tests** - Run all automated tests
2. **Performance Tests** - Verify benchmarks meet targets
3. **Resource Tests** - Check for leaks and resource usage
4. **Updater Tests** - Validate update mechanism
5. **Installer Tests** - Test installation scenarios
6. **Integration Tests** - End-to-end workflow validation

## Enhanced Conclusion

This comprehensive testing guide ensures quality and reliability across all aspects of ContextSensor.

### Pre-Release Checklist

Before each release:

- [ ] All unit tests pass (`dotnet test`)
- [ ] Performance benchmarks meet targets
- [ ] No resource leaks detected
- [ ] Memory usage within limits
- [ ] Update mechanism tested
- [ ] Installer tested on clean VMs (Win10 & Win11)
- [ ] Upgrade paths verified
- [ ] Service lifecycle validated
- [ ] Uninstall cleanup confirmed
- [ ] Documentation updated

### Key Testing Principles

1. **Test on Clean VMs** - Avoid environment contamination
2. **Use Snapshots** - Enable quick rollback
3. **Automate Everything** - Reduce manual effort
4. **Monitor Resources** - Track memory, CPU, handles
5. **Document Findings** - Maintain test evidence
6. **Test Edge Cases** - Cover failure scenarios
7. **Verify Multiple OS** - Windows 10 and 11
8. **Measure Performance** - Establish baselines
9. **Test Updates** - Validate upgrade paths
10. **Continuous Testing** - Integrate with CI/CD

### Support Resources

For issues or questions, refer to:

- **Performance Issues**: [Performance Testing Guide](PERFORMANCE_TESTING.md)
- **Update Issues**: [Updater Testing Guide](UPDATER_TESTING.md)
- **Installer Issues**: [Installer Guide](../installer/GUIDE.md)
- **Manifest Format**: [Update Manifest Reference](../installer/UPDATE_MANIFEST_REFERENCE.md)
- **Application Logs**: `C:\ProgramData\ContextSensor\logs\`
- **Test Results**: Check CI/CD pipeline outputs

### Getting Help

If tests fail or you encounter issues:

1. **Check Logs** - Review application and test logs
2. **Review Documentation** - Consult specific testing guides
3. **Isolate Issue** - Run specific test categories
4. **Compare Baselines** - Check against known good results
5. **Clean Environment** - Test on fresh VM
6. **Report Issues** - Document failures with logs and repro steps

---

**Last Updated**: 2025-11-01  
**Version**: 1.0.0