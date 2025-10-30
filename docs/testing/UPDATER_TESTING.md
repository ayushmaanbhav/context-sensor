# ContextSensor Updater Service Testing Guide

This guide covers testing procedures specific to the auto-update functionality of ContextSensor, including update manifest handling, version comparison, and upgrade workflows.

## Table of Contents

1. [Overview](#overview)
2. [Test Project](#test-project)
3. [Test Categories](#test-categories)
4. [Running Tests](#running-tests)
5. [Manual Testing](#manual-testing)
6. [Test Scenarios](#test-scenarios)
7. [Troubleshooting](#troubleshooting)

## Overview

The updater service is responsible for:
- Checking for available updates periodically
- Downloading update packages
- Verifying package integrity
- Installing updates silently
- Managing backup and rollback

Testing ensures:
- ✅ Updates are detected correctly
- ✅ Version comparison works reliably
- ✅ Downloads are verified for integrity
- ✅ Installation process is robust
- ✅ Error handling is comprehensive

## Test Project

### ContextSensor.UpdaterTests

Location: [`test/ContextSensor.UpdaterTests/`](../../test/ContextSensor.UpdaterTests/)

**Purpose**: Unit and integration testing for updater service

**Dependencies**:
- xUnit - Test framework
- FluentAssertions - Readable assertions
- Moq - Mocking framework

**Structure**:
```
test/ContextSensor.UpdaterTests/
├── UpdateManifestTests.cs       # Manifest parsing tests
├── UpdaterServiceTests.cs       # Service logic tests
├── UpdaterIntegrationTests.cs   # End-to-end workflows
└── ContextSensor.UpdaterTests.csproj
```

## Test Categories

### 1. Manifest Tests

Located in [`UpdateManifestTests.cs`](../../test/ContextSensor.UpdaterTests/UpdateManifestTests.cs)

#### Valid Manifest Parsing
```csharp
Should_Parse_Valid_Manifest()
```
**Purpose**: Verify correct parsing of well-formed update manifests

**Test Manifest**:
```json
{
  "version": "1.0.1",
  "releaseDate": "2025-11-01T00:00:00Z",
  "downloadUrl": "https://example.com/ContextSensor-1.0.1.msi",
  "checksum": "sha256:abc123def456",
  "releaseNotes": "Bug fixes and improvements",
  "minimumVersion": "1.0.0",
  "isMandatory": false
}
```

#### Optional Fields Handling
```csharp
Should_Handle_Manifest_With_Missing_Optional_Fields()
```
**Purpose**: Ensure graceful handling of minimal manifests

**Minimal Manifest**:
```json
{
  "version": "1.0.1",
  "releaseDate": "2025-11-01T00:00:00Z",
  "downloadUrl": "https://example.com/ContextSensor-1.0.1.msi"
}
```

#### Date Parsing
```csharp
Should_Parse_Release_Date_Correctly()
```
**Purpose**: Verify ISO 8601 date parsing

#### Checksum Format Support
```csharp
Should_Support_Different_Checksum_Formats()
```
**Purpose**: Handle both prefixed and non-prefixed checksums
- `sha256:abc123` ✅
- `abc123` ✅

#### Version Comparison
```csharp
Should_Compare_Versions_Correctly()
```
**Test Cases**:
| Current | Available | Should Update |
|---------|-----------|---------------|
| 1.0.0 | 1.0.1 | ✅ Yes |
| 1.0.0 | 1.1.0 | ✅ Yes |
| 1.0.0 | 2.0.0 | ✅ Yes |
| 1.0.1 | 1.0.0 | ❌ No |
| 1.0.0 | 1.0.0 | ❌ No |

### 2. Service Tests

Located in [`UpdaterServiceTests.cs`](../../test/ContextSensor.UpdaterTests/UpdaterServiceTests.cs)

#### Configuration Initialization
```csharp
Should_Initialize_With_Valid_Configuration()
```
**Purpose**: Verify service starts with valid config

**Configuration**:
```json
{
  "Update": {
    "UpdateCheckUrl": "https://example.com/manifest.json",
    "CheckIntervalHours": 24,
    "AutoInstall": false,
    "VerifyChecksum": true,
    "BackupBeforeUpdate": true,
    "StartupDelayMinutes": 5
  }
}
```

#### Network Error Handling
```csharp
Should_Handle_Network_Errors_Gracefully()
```
**Purpose**: Ensure service continues operating despite network failures

**Error Scenarios**:
- Network timeout
- DNS resolution failure
- HTTP 404/500 errors
- Invalid SSL certificate

#### Manifest Response Parsing
```csharp
Should_Parse_Valid_Manifest_Response()
```
**Purpose**: Verify HTTP manifest download and parsing

#### Update Detection
```csharp
Should_Detect_When_Update_Is_Available()
```
**Purpose**: Confirm correct version comparison logic

#### Configuration Respect
```csharp
Should_Respect_AutoInstall_Setting()
Should_Respect_ChecksumVerification_Setting()
```
**Purpose**: Verify configuration settings are honored

#### Edge Cases
```csharp
Should_Handle_Empty_Update_URL()
Should_Use_Configured_Check_Interval()
Should_Support_Startup_Delay()
Should_Support_Backup_Before_Update()
```

### 3. Integration Tests

Located in [`UpdaterIntegrationTests.cs`](../../test/ContextSensor.UpdaterTests/UpdaterIntegrationTests.cs)

#### Manifest File Creation
```csharp
Should_Create_Valid_Manifest_File()
```
**Purpose**: Test manifest generation for distribution

#### Checksum Calculation
```csharp
Should_Calculate_File_Checksum_Correctly()
```
**Purpose**: Verify SHA256 checksum calculation
- Same file → same checksum ✅
- Different files → different checksums ✅

#### Checksum Verification
```csharp
Should_Detect_Checksum_Mismatch()
```
**Purpose**: Ensure corrupted downloads are rejected

#### Directory Permissions
```csharp
Should_Verify_Download_Directory_Permissions()
```
**Purpose**: Check write/read/delete permissions in temp directory

#### Corrupted Manifest Handling
```csharp
Should_Handle_Corrupted_Manifest()
```
**Purpose**: Gracefully handle invalid JSON

#### Multiple Download Management
```csharp
Should_Support_Multiple_Download_Attempts()
Should_Clean_Up_Old_Downloads()
```
**Purpose**: Manage download directory lifecycle

#### Concurrent Operations
```csharp
Should_Handle_Concurrent_Update_Checks()
```
**Purpose**: Verify thread-safety of update checks

#### Schema Validation
```csharp
Should_Validate_Manifest_Schema()
```
**Purpose**: Ensure required fields are present

## Running Tests

### Run All Updater Tests

```powershell
cd test/ContextSensor.UpdaterTests
dotnet test -c Release
```

### Run Specific Test Class

```powershell
# Manifest tests only
dotnet test --filter "FullyQualifiedName~UpdateManifestTests"

# Service tests only
dotnet test --filter "FullyQualifiedName~UpdaterServiceTests"

# Integration tests only
dotnet test --filter "FullyQualifiedName~UpdaterIntegrationTests"
```

### Run Specific Test

```powershell
dotnet test --filter "Name~Should_Parse_Valid_Manifest"
```

### Run with Detailed Output

```powershell
dotnet test -c Release --logger "console;verbosity=detailed"
```

### Generate Test Coverage

```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Manual Testing

### Test Setup

#### 1. Create Test Update Server

**Option A: Python HTTP Server**
```powershell
# Create test directory
mkdir C:\TestUpdateServer
cd C:\TestUpdateServer

# Create test manifest
$manifest = @{
    version = "1.0.1"
    releaseDate = (Get-Date).ToUniversalTime().ToString("o")
    downloadUrl = "http://localhost:8000/ContextSensor-1.0.1.msi"
    checksum = "sha256:CALCULATE_ACTUAL_CHECKSUM"
    releaseNotes = "Test update"
} | ConvertTo-Json

Set-Content -Path "manifest.json" -Value $manifest

# Start server
python -m http.server 8000
```

**Option B: .NET Simple Server**
```powershell
dotnet serve -p 8000 -d C:\TestUpdateServer
```

#### 2. Calculate MSI Checksum

```powershell
$msiPath = "C:\TestUpdateServer\ContextSensor-1.0.1.msi"
$hash = Get-FileHash -Path $msiPath -Algorithm SHA256
$checksum = "sha256:" + $hash.Hash.ToLower()
Write-Host "Checksum: $checksum"

# Update manifest with actual checksum
$manifest = Get-Content "manifest.json" | ConvertFrom-Json
$manifest.checksum = $checksum
$manifest | ConvertTo-Json | Set-Content "manifest.json"
```

#### 3. Configure Updater Service

Edit [`updater.config.json`](../../src/ContextSensor.Updater/updater.config.json):

```json
{
  "Update": {
    "UpdateCheckUrl": "http://localhost:8000/manifest.json",
    "CheckIntervalHours": 0.05,
    "AutoInstall": true,
    "VerifyChecksum": true,
    "BackupBeforeUpdate": false,
    "AllowDowngrade": false,
    "StartupDelayMinutes": 0.1
  }
}
```

### Manual Test Scenarios

#### Scenario 1: Fresh Update Check

**Steps**:
1. Start test update server
2. Install ContextSensor v1.0.0
3. Configure updater to point to test server
4. Restart updater service
5. Monitor logs

**Expected**:
```
[INF] Checking for updates...
[INF] Latest version available: 1.0.1
[INF] New version available: 1.0.1 (current: 1.0.0)
[INF] Auto-install is enabled. Starting update process...
[INF] Downloading update from http://localhost:8000/ContextSensor-1.0.1.msi
[INF] Download completed
[INF] Verifying checksum...
[INF] Checksum verification passed
[INF] Stopping ContextSensor services...
[INF] Installing update...
[INF] Update installed successfully
```

**Verification**:
```powershell
# Check new version
Get-ItemProperty -Path "HKLM:\Software\ContextSensor" | Select-Object Version

# Verify services running
Get-Service ContextSensor.* | Format-Table Name, Status

# Check logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 50
```

#### Scenario 2: Checksum Mismatch

**Steps**:
1. Create manifest with incorrect checksum
2. Trigger update check

**Expected**:
```
[ERR] Checksum verification failed. Aborting update.
```

**Verification**:
- Update NOT installed
- Original version still running
- Error logged

#### Scenario 3: Network Failure

**Steps**:
1. Stop test update server
2. Wait for update check

**Expected**:
```
[ERR] Error downloading update manifest
[WRN] Failed to download update manifest
```

**Verification**:
- Service continues running
- No crash
- Error logged

#### Scenario 4: Auto-Install Disabled

**Steps**:
1. Set `AutoInstall: false`
2. Restart service
3. Wait for update check

**Expected**:
```
[INF] New version available: 1.0.1 (current: 1.0.0)
[INF] Auto-install is disabled. Update available but not installing.
```

**Verification**:
- Update detected but not installed
- User notification (if configured)

#### Scenario 5: Backup Before Update

**Steps**:
1. Set `BackupBeforeUpdate: true`
2. Trigger update

**Expected**:
```
[INF] Creating backup before update...
[INF] Backup created at: C:\ProgramData\ContextSensor\temp\updates\backup-20251101120000
```

**Verification**:
```powershell
Get-ChildItem "C:\ProgramData\ContextSensor\temp\updates\backup-*"
```

## Test Scenarios

### Scenario Matrix

| Scenario | Current Version | Available Version | Auto-Install | Checksum Valid | Expected Result |
|----------|----------------|-------------------|--------------|----------------|-----------------|
| Normal update | 1.0.0 | 1.0.1 | Yes | Yes | Install |
| Manual update | 1.0.0 | 1.0.1 | No | Yes | Detect only |
| Invalid checksum | 1.0.0 | 1.0.1 | Yes | No | Abort |
| Same version | 1.0.0 | 1.0.0 | Yes | Yes | Skip |
| Downgrade | 1.0.1 | 1.0.0 | Yes | Yes | Skip |
| Network error | 1.0.0 | N/A | Yes | N/A | Log error |
| Corrupted manifest | 1.0.0 | N/A | Yes | N/A | Log error |

### Advanced Scenarios

#### Scenario A: Update During Active Use

**Setup**: Application actively processing events

**Steps**:
1. Start event processing
2. Trigger update

**Expected**:
- Monitor service stops app gracefully
- Update installs
- App restarts automatically
- No data loss

#### Scenario B: Update Failure Recovery

**Setup**: Simulate update failure

**Steps**:
1. Make MSI uninstallable (invalid package)
2. Trigger update

**Expected**:
- Update fails
- Services restarted
- System returns to previous state
- Error logged

#### Scenario C: Multiple Rapid Update Checks

**Setup**: Very short check interval (0.01 hours)

**Steps**:
1. Configure rapid checks
2. Monitor for 5 minutes

**Expected**:
- No crashes
- Proper interval spacing
- No duplicate downloads

## Troubleshooting

### Common Issues

#### Issue 1: Updates Not Detected

**Symptoms**: Service running but not detecting updates

**Diagnosis**:
```powershell
# Check service status
Get-Service ContextSensor.Updater

# Check configuration
Get-Content "C:\Program Files\ContextSensor\updater.config.json"

# Check logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 50

# Test manifest URL manually
Invoke-RestMethod -Uri "YOUR_UPDATE_URL/manifest.json"
```

**Common Causes**:
- Empty or invalid update URL
- Network connectivity issues
- Firewall blocking requests
- Invalid manifest JSON
- Service not running

#### Issue 2: Checksum Verification Failing

**Symptoms**: Downloads fail checksum validation

**Diagnosis**:
```powershell
# Calculate actual checksum
Get-FileHash -Path "downloaded-file.msi" -Algorithm SHA256

# Compare with manifest
$manifest = Invoke-RestMethod -Uri "YOUR_UPDATE_URL/manifest.json"
Write-Host "Manifest checksum: $($manifest.checksum)"
```

**Solutions**:
- Regenerate checksum
- Update manifest with correct checksum
- Ensure file not corrupted during upload

#### Issue 3: Installation Fails

**Symptoms**: Download succeeds but installation fails

**Diagnosis**:
```powershell
# Check MSI log
Get-Content "C:\ProgramData\ContextSensor\temp\updates\install.log"

# Try manual install
Start-Process msiexec -ArgumentList "/i `"path-to-msi`" /l*v `"test-install.log`"" -Wait

# Check exit code
$LASTEXITCODE
```

**Common Exit Codes**:
- `0`: Success
- `1603`: Fatal error during installation
- `1618`: Another installation in progress
- `1619`: Package could not be opened
- `3010`: Success, reboot required

#### Issue 4: Service Won't Start After Update

**Symptoms**: Updater service stops after installing update

**Diagnosis**:
```powershell
# Check Windows Event Log
Get-EventLog -LogName Application -Source ContextSensor.Updater -Newest 10

# Check service configuration
sc.exe query ContextSensor.Updater
sc.exe qc ContextSensor.Updater

# Try manual start
Start-Service ContextSensor.Updater
```

### Debug Mode

Enable verbose logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ContextSensor.Updater": "Trace"
    }
  }
}
```

## Best Practices

### Creating Update Manifests

1. **Always include checksum**: Prevents corrupted downloads
2. **Use ISO 8601 dates**: Ensures proper parsing
3. **Include release notes**: Helps with auditing
4. **Test manifest JSON**: Validate before publishing
5. **Version manifest URL**: Consider versioning scheme

### Testing Updates

1. **Test on clean VM**: Avoid environment issues
2. **Test upgrade paths**: Verify each version transition
3. **Test failure scenarios**: Ensure graceful error handling
4. **Monitor resource usage**: Check for leaks during update
5. **Verify rollback**: Test backup/restore functionality

### Production Deployments

1. **Stage updates**: Test in staging environment first
2. **Gradual rollout**: Deploy to subset of users initially
3. **Monitor metrics**: Track success/failure rates
4. **Have rollback plan**: Keep previous version accessible
5. **Document changes**: Maintain comprehensive release notes

## Integration with CI/CD

### Automated Manifest Generation

```powershell
# Example build script
$version = "1.0.1"
$msiPath = ".\ContextSensor.msi"
$downloadUrl = "https://releases.example.com/ContextSensor-$version.msi"

# Calculate checksum
$hash = Get-FileHash -Path $msiPath -Algorithm SHA256
$checksum = "sha256:" + $hash.Hash.ToLower()

# Generate manifest
$manifest = @{
    version = $version
    releaseDate = (Get-Date).ToUniversalTime().ToString("o")
    downloadUrl = $downloadUrl
    checksum = $checksum
    releaseNotes = "See CHANGELOG.md"
    minimumVersion = "1.0.0"
} | ConvertTo-Json -Depth 10

# Save manifest
Set-Content -Path "manifest.json" -Value $manifest

# Upload to distribution server
# ... (your upload logic)
```

### Automated Testing in Pipeline

```yaml
# Azure DevOps example
- task: DotNetCoreCLI@2
  displayName: 'Run Updater Tests'
  inputs:
    command: 'test'
    projects: 'test/ContextSensor.UpdaterTests/*.csproj'
    arguments: '-c Release --logger trx'

- task: PublishTestResults@2
  displayName: 'Publish Test Results'
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
```

## See Also

- [Main Testing Guide](GUIDE.md) - Overall testing procedures
- [Performance Testing Guide](PERFORMANCE_TESTING.md) - Performance testing
- [Update Manifest Reference](../installer/UPDATE_MANIFEST_REFERENCE.md) - Manifest format details
- [Installer Guide](../installer/GUIDE.md) - Installer creation and deployment