# Update Manifest Reference

Quick reference for creating and hosting ContextSensor update manifests for automatic updates.

## Manifest Format

```json
{
  "version": "1.0.2",
  "releaseDate": "2025-11-01T00:00:00Z",
  "downloadUrl": "https://updates.example.com/ContextSensor-1.0.2-x64.msi",
  "checksum": "sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "releaseNotes": "Bug fixes and performance improvements"
}
```

## Field Reference

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `version` | ✅ Yes | String | Semantic version (e.g., "1.0.2") |
| `releaseDate` | ✅ Yes | ISO 8601 String | Release timestamp in UTC |
| `downloadUrl` | ✅ Yes | URL | Direct HTTPS link to MSI installer |
| `checksum` | ✅ Yes | String | SHA256 hash prefixed with `sha256:` |
| `releaseNotes` | ❌ No | String | Brief description of changes |
| `minimumVersion` | ❌ No | String | Minimum version required for upgrade |
| `mandatoryUpdate` | ❌ No | Boolean | Reserved for future use (default: false) |

## Generating Checksums

### PowerShell
```powershell
$hash = Get-FileHash -Path "ContextSensor-1.0.2-x64.msi" -Algorithm SHA256
$checksum = "sha256:" + $hash.Hash.ToLower()
Write-Host $checksum
```

### Using Build Script
The [`build-installer.ps1`](../../installer/build-installer.ps1) automatically displays the checksum:
```powershell
cd installer
.\build-installer.ps1
# Look for "SHA256: ..." in the output
```

### Command Line
```powershell
# Windows
certutil -hashfile ContextSensor-1.0.2-x64.msi SHA256

# Linux/macOS
sha256sum ContextSensor-1.0.2-x64.msi
```

## Hosting Options

### Option 1: Static Web Server

```
https://updates.example.com/
└── contextsensor/
    ├── manifest.json              ← Latest version info
    ├── ContextSensor-1.0.0-x64.msi
    ├── ContextSensor-1.0.1-x64.msi
    └── ContextSensor-1.0.2-x64.msi
```

**Nginx Configuration:**
```nginx
location /contextsensor/ {
    add_header Content-Type application/json;
    add_header Cache-Control "public, max-age=300";
    add_header Access-Control-Allow-Origin "*";
}
```

### Option 2: GitHub Releases

```json
{
  "version": "1.0.2",
  "releaseDate": "2025-11-01T00:00:00Z",
  "downloadUrl": "https://github.com/yourorg/contextsensor/releases/download/v1.0.2/ContextSensor-1.0.2-x64.msi",
  "checksum": "sha256:...",
  "releaseNotes": "See: https://github.com/yourorg/contextsensor/releases/tag/v1.0.2"
}
```

### Option 3: Azure Blob Storage

```powershell
# Upload MSI
az storage blob upload \
  --account-name youraccount \
  --container-name updates \
  --name ContextSensor-1.0.2-x64.msi \
  --file ContextSensor-1.0.2-x64.msi

# Upload manifest
az storage blob upload \
  --account-name youraccount \
  --container-name updates \
  --name manifest.json \
  --file manifest.json \
  --content-type application/json
```

### Option 4: AWS S3

```bash
# Upload MSI
aws s3 cp ContextSensor-1.0.2-x64.msi \
  s3://your-bucket/contextsensor/ \
  --acl public-read

# Upload manifest
aws s3 cp manifest.json \
  s3://your-bucket/contextsensor/ \
  --content-type application/json \
  --acl public-read
```

## Release Workflow

### 1. Build New Version

```powershell
# Update version in Product.wxs (line 8)
# <Package Name="ContextSensor" Version="1.0.2" ...>

cd installer
.\build-installer.ps1

# Note the output checksum
# SHA256: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
```

### 2. Sign MSI (Recommended)

```powershell
signtool sign /f certificate.pfx `
  /p password `
  /t http://timestamp.digicert.com `
  .\ContextSensor-1.0.2-x64.msi
```

### 3. Create Manifest

Create `manifest.json` with new version info:

```json
{
  "version": "1.0.2",
  "releaseDate": "2025-11-01T10:00:00Z",
  "downloadUrl": "https://your-cdn.com/ContextSensor-1.0.2-x64.msi",
  "checksum": "sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
  "releaseNotes": "• Fixed issue with high-DPI displays\n• Improved event processing performance\n• Updated dependencies"
}
```

### 4. Upload Files

```powershell
# Upload MSI to CDN/web server
# Upload manifest.json to update endpoint
```

### 5. Verify

```powershell
# Test manifest is accessible
Invoke-RestMethod -Uri "https://your-server.com/updates/manifest.json"

# Monitor update logs on client machines
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 50 -Wait
```

## Configuration

### Set Update URL During Installation

```powershell
msiexec /i ContextSensor-x64.msi `
  UPDATEURL="https://your-server.com/contextsensor/manifest.json" `
  /quiet
```

### Configure After Installation

**Option 1: Edit Configuration File**
```powershell
notepad "C:\Program Files\ContextSensor\updater.config.json"
# Change "UpdateCheckUrl" value
Restart-Service ContextSensor.Updater
```

**Option 2: Via Registry**
```powershell
Set-ItemProperty -Path "HKLM:\Software\ContextSensor" `
  -Name "UpdateUrl" `
  -Value "https://your-server.com/contextsensor/manifest.json"
Restart-Service ContextSensor.Updater
```

## Update Process

The Updater service automatically:

1. **Checks** manifest URL every 4 hours (configurable)
2. **Compares** manifest version with installed version
3. **Downloads** MSI if newer version available
4. **Verifies** SHA256 checksum
5. **Stops** Monitor service (which stops the app)
6. **Installs** update silently: `msiexec /i package.msi /quiet /norestart`
7. **Restarts** services automatically
8. **Preserves** all data in `C:\ProgramData\ContextSensor\`

## Examples

### Patch Release

```json
{
  "version": "1.0.1",
  "releaseDate": "2025-11-05T00:00:00Z",
  "downloadUrl": "https://updates.example.com/ContextSensor-1.0.1-x64.msi",
  "checksum": "sha256:abc123...",
  "releaseNotes": "Bug fixes"
}
```

### Minor Release

```json
{
  "version": "1.1.0",
  "releaseDate": "2025-12-01T00:00:00Z",
  "downloadUrl": "https://updates.example.com/ContextSensor-1.1.0-x64.msi",
  "checksum": "sha256:def456...",
  "releaseNotes": "New features:\n• Keyboard event tracking\n• Enhanced logging\n• Performance improvements"
}
```

### Major Release with Minimum Version

```json
{
  "version": "2.0.0",
  "releaseDate": "2026-01-15T00:00:00Z",
  "downloadUrl": "https://updates.example.com/ContextSensor-2.0.0-x64.msi",
  "checksum": "sha256:ghi789...",
  "releaseNotes": "Major update - requires version 1.5.0 or higher",
  "minimumVersion": "1.5.0"
}
```

### Security Update

```json
{
  "version": "1.0.3",
  "releaseDate": "2025-11-20T00:00:00Z",
  "downloadUrl": "https://updates.example.com/ContextSensor-1.0.3-x64.msi",
  "checksum": "sha256:jkl012...",
  "releaseNotes": "SECURITY UPDATE: Critical vulnerability fixed - update immediately",
  "mandatoryUpdate": true
}
```

## Testing Updates Locally

### 1. Start Local HTTP Server

```powershell
# Using Python
python -m http.server 8000

# Or using .NET
dotnet tool install -g dotnet-serve
dotnet serve -p 8000
```

### 2. Create Test Manifest

Place in server directory:
```json
{
  "version": "1.0.2",
  "releaseDate": "2025-11-01T00:00:00Z",
  "downloadUrl": "http://localhost:8000/ContextSensor-1.0.2-x64.msi",
  "checksum": "sha256:...",
  "releaseNotes": "Test update"
}
```

### 3. Configure Updater

```powershell
$config = @{
  Update = @{
    UpdateCheckUrl = "http://localhost:8000/manifest.json"
    CheckIntervalHours = 0.1  # Check every 6 minutes
    AutoInstall = $true
    BackupBeforeUpdate = $true
    VerifyChecksum = $true
  }
} | ConvertTo-Json
$config | Set-Content "C:\Program Files\ContextSensor\updater.config.json"
Restart-Service ContextSensor.Updater
```

### 4. Monitor Logs

```powershell
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 50 -Wait
```

## Troubleshooting

### Update Not Detected

```powershell
# Verify manifest is accessible
Invoke-RestMethod -Uri "https://your-server.com/manifest.json"

# Check current version
Get-ItemProperty -Path "HKLM:\Software\ContextSensor" | Select-Object Version

# Review updater logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 100

# Force check by restarting service
Restart-Service ContextSensor.Updater
```

### Download Fails

```powershell
# Test download URL
Invoke-WebRequest -Uri "https://your-server.com/package.msi" -OutFile "test.msi"

# Check disk space
Get-PSDrive C | Format-Table Name, Used, Free

# Check temp directory
Test-Path "C:\ProgramData\ContextSensor\temp\updates"
```

### Checksum Mismatch

```powershell
# Verify checksum locally
$hash = Get-FileHash -Path "ContextSensor-1.0.2-x64.msi" -Algorithm SHA256
Write-Host ("sha256:" + $hash.Hash.ToLower())

# Compare with manifest
Invoke-RestMethod -Uri "https://your-server.com/manifest.json" | Select-Object checksum
```

## Security Best Practices

| Practice | Why | Implementation |
|----------|-----|----------------|
| **Use HTTPS** | Prevent MITM attacks | All URLs must use HTTPS protocol |
| **Verify Checksums** | Ensure file integrity | SHA256 automatically verified |
| **Code Sign MSI** | Verify publisher authenticity | Use valid code signing certificate |
| **Access Control** | Prevent unauthorized updates | Restrict write access to manifest |
| **Audit Logging** | Track update activities | Monitor updater.log files |
| **Rate Limiting** | Prevent abuse | Implement on server side |

## Quick Reference

```powershell
# Generate checksum
$hash = Get-FileHash "package.msi" -Algorithm SHA256
"sha256:" + $hash.Hash.ToLower()

# Test manifest
Invoke-RestMethod "https://your-server.com/manifest.json"

# Check installed version
(Get-ItemProperty "HKLM:\Software\ContextSensor").Version

# View update logs
Get-Content "C:\ProgramData\ContextSensor\logs\updater.log" -Tail 50

# Force update check
Restart-Service ContextSensor.Updater
```

---

**Related Documentation:**
- [Installation Guide](GUIDE.md) - Complete installation and configuration guide
- [Build Instructions](../../installer/README.md) - Building the MSI installer
- [Example Manifest](../../installer/manifest-example.json) - Sample manifest file