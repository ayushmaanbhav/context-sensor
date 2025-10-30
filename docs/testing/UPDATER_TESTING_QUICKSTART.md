# Updater Testing Quick Start Guide

This guide shows you how to test the ContextSensor updater service using two approaches: automated unit tests and end-to-end integration testing.

## Two Types of Updater Testing

### 1. Automated Unit Tests ✅ (Already Running)

These tests validate the updater logic **without** requiring a real server or installed application.

**What they test**:
- ✅ Manifest parsing and validation
- ✅ Version comparison logic
- ✅ Checksum calculation
- ✅ Configuration handling
- ✅ Error scenarios

**How to run**:
```powershell
cd test/ContextSensor.UpdaterTests
dotnet test -c Release
```

**Results**: ✅ 42/42 tests passed

---

### 2. End-to-End Integration Testing 🔄 (Real-World Testing)

This tests the **actual updater service** with a real test server and simulated update.

**What it tests**:
- 🔄 Real HTTP manifest download
- 🔄 Actual update detection
- 🔄 Service configuration changes
- 🔄 Log monitoring
- 🔄 Complete update workflow

**This is what you should run to fully validate the updater!**

---

## End-to-End Testing Guide

### Prerequisites

1. **ContextSensor installed** (via MSI installer)
2. **Administrator privileges** (to modify service config)
3. **Test update server** (included in project)

### Quick Test (Automated Script)

**Option A: Using PowerShell script** (Recommended)

```powershell
cd test

# Run end-to-end test (safe mode - won't actually install)
.\Test-UpdaterEndToEnd.ps1

# Run with custom version
.\Test-UpdaterEndToEnd.ps1 -TestVersion "1.0.2"

# Run on different port
.\Test-UpdaterEndToEnd.ps1 -Port 9000
```

**What this script does**:
1. ✅ Checks if ContextSensor is installed
2. ✅ Creates a test MSI file
3. ✅ Calculates checksum
4. ✅ Generates manifest.json
5. ✅ Starts test server (if UpdateServer project exists)
6. ✅ Configures updater to use localhost
7. ✅ Monitors logs for update detection
8. ✅ Provides cleanup instructions

**Expected Output**:
```
=== ContextSensor Updater End-to-End Test ===

Step 1: Checking ContextSensor installation...
  ✓ Updater service found
  Current version: 1.0.0

Step 2: Setting up test update server...
  ✓ Test server directory ready

Step 3: Creating test MSI file...
  ✓ Test MSI created

Step 4: Calculating checksum...
  ✓ Checksum: sha256:abc123...

Step 5: Creating update manifest...
  ✓ Manifest created

Step 6: Starting test update server...
  ✓ Test server started

Step 7: Testing manifest accessibility...
  ✓ Manifest accessible
  Available version: 1.0.1

Step 8: Backing up updater configuration...
  ✓ Configuration backed up

Step 9: Configuring updater for test...
  ✓ Updater configured

Step 10: Restarting updater service...
  ✓ Updater service restarted

Step 11: Monitoring updater logs...
  Watching for update detection...
  .....
  ✓ UPDATE DETECTED!
  
  Recent log entries:
    [INF] Checking for updates...
    [INF] Latest version available: 1.0.1
    [INF] New version available: 1.0.1 (current: 1.0.0)

✓ Updater service successfully detected the update!
```

---

### Manual Step-by-Step Testing

If you prefer to test manually:

#### Step 1: Start Test Update Server

**Option A: Using the UpdateServer project**
```powershell
cd test/UpdateServer
dotnet run --urls "http://localhost:8000"
```

**Option B: Using Python**
```powershell
cd test/UpdateServer
python -m http.server 8000
```

**Option C: Using dotnet-serve**
```powershell
dotnet tool install --global dotnet-serve
cd test/UpdateServer
dotnet serve -p 8000
```

#### Step 2: Create Test Manifest

Create `test/UpdateServer/manifest.json`:

```json
{
  "version": "1.0.1",
  "releaseDate": "2025-11-01T00:00:00Z",
  "downloadUrl": "http://localhost:8000/ContextSensor-1.0.1.msi",
  "checksum": "sha256:YOUR_CHECKSUM_HERE",
  "releaseNotes": "Test update - DO NOT USE IN PRODUCTION"
}
```

#### Step 3: Create Fake MSI

```powershell
# Create a fake MSI file for testing
$testContent = "Fake MSI for testing"
Set-Content -Path "test/UpdateServer/ContextSensor-1.0.1.msi" -Value $testContent

# Calculate checksum
$hash = Get-FileHash -Path "test/UpdateServer/ContextSensor-1.0.1.msi" -Algorithm SHA256
$checksum = "sha256:" + $hash.Hash.ToLower()
Write-Host "Use this checksum in manifest: $checksum"
```

Update the manifest with the actual checksum.

#### Step 4: Configure Updater

Edit `C:\Program Files\ContextSensor\updater.config.json`:

```json
{
  "Update": {
    "UpdateCheckUrl": "http://localhost:8000/manifest.json",
    "CheckIntervalHours": 0.05,
    "AutoInstall": false,
    "VerifyChecksum": true,
    "BackupBeforeUpdate": true,
    "StartupDelayMinutes": 0.1
  }
}
```

**IMPORTANT**: Set `AutoInstall: false` for safety during testing!

#### Step 5: Restart Updater Service

```powershell
Restart-Service ContextSensor.Updater
```

#### Step 6: Monitor Logs

```powershell
# Watch logs in real-time
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Wait -Tail 20
```

**Watch for these log entries**:
```
[INF] Checking for updates...
[INF] Latest version available: 1.0.1
[INF] New version available: 1.0.1 (current: 1.0.0)
[INF] Auto-install is disabled. Update available but not installing.
```

#### Step 7: Verify Detection

```powershell
# Check recent logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 50 | Select-String "version"

# Expected to see:
# - "Latest version available: 1.0.1"
# - "New version available"
```

---

## Testing Different Scenarios

### Scenario 1: Update Detection (AutoInstall OFF)

**Config**: `AutoInstall: false`

**Expected**:
```
[INF] New version available: 1.0.1 (current: 1.0.0)
[INF] Auto-install is disabled. Update available but not installing.
```

✅ **Result**: Update detected but NOT installed (safe)

---

### Scenario 2: Checksum Mismatch

**Setup**: Use wrong checksum in manifest

**Expected**:
```
[INF] Downloading update...
[INF] Download completed
[INF] Verifying checksum...
[ERR] Checksum verification failed. Aborting update.
```

✅ **Result**: Update rejected (security working)

---

### Scenario 3: Network Error

**Setup**: Stop test server

**Expected**:
```
[ERR] Error downloading update manifest
[WRN] Failed to download update manifest
```

✅ **Result**: Service continues running, error logged

---

### Scenario 4: Same Version

**Setup**: Manifest version = current version

**Expected**:
```
[INF] Latest version available: 1.0.0
[INF] No updates available. Current version 1.0.0 is up to date.
```

✅ **Result**: No update attempted

---

## Cleanup After Testing

### 1. Stop Test Server

```powershell
# If started as job
Get-Job | Stop-Job
Get-Job | Remove-Job

# If started manually, press Ctrl+C
```

### 2. Restore Original Configuration

```powershell
# Restore backup
Copy-Item "C:\Program Files\ContextSensor\updater.config.json.backup" `
          "C:\Program Files\ContextSensor\updater.config.json" -Force

# Restart service
Restart-Service ContextSensor.Updater
```

### 3. Clean Up Test Files

```powershell
Remove-Item "test/UpdateServer/ContextSensor-*.msi"
Remove-Item "test/UpdateServer/manifest.json"
```

---

## Troubleshooting

### Issue: Can't access manifest

**Symptom**: 
```
Invoke-RestMethod : Unable to connect to the remote server
```

**Solution**:
1. Check server is running: `netstat -an | findstr :8000`
2. Test in browser: `http://localhost:8000/manifest.json`
3. Check firewall settings

### Issue: Update not detected

**Symptom**: Logs show no update check

**Solution**:
1. Check service is running: `Get-Service ContextSensor.Updater`
2. Verify config file: `Get-Content "C:\Program Files\ContextSensor\updater.config.json"`
3. Check startup delay has passed
4. Review logs: `Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 100`

### Issue: Checksum verification fails

**Symptom**: 
```
[ERR] Checksum verification failed
```

**Solution**:
1. Recalculate checksum:
   ```powershell
   Get-FileHash -Path "test/UpdateServer/ContextSensor-1.0.1.msi" -Algorithm SHA256
   ```
2. Update manifest with correct checksum
3. Restart test

---

## Quick Reference Commands

```powershell
# Check current version
Get-ItemProperty -Path "HKLM:\Software\ContextSensor" | Select-Object Version

# Check updater service
Get-Service ContextSensor.Updater | Format-List *

# View updater config
Get-Content "C:\Program Files\ContextSensor\updater.config.json" | ConvertFrom-Json

# Watch updater logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Wait -Tail 20

# Restart updater
Restart-Service ContextSensor.Updater

# Check for update checks in logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" | Select-String "Checking for updates"
```

---

## Best Practices

1. **Always use AutoInstall: false** during testing
2. **Backup configuration** before making changes
3. **Monitor logs** to understand behavior
4. **Clean up test files** after testing
5. **Restore original config** when done
6. **Test on non-production systems** first

---

## See Also

- [Updater Testing Guide](../docs/testing/UPDATER_TESTING.md) - Comprehensive testing procedures
- [Update Manifest Reference](../docs/installer/UPDATE_MANIFEST_REFERENCE.md) - Manifest format
- [Main Testing Guide](../docs/testing/GUIDE.md) - Overall testing procedures