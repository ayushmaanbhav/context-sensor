# Building the Installer

Quick reference for building the ContextSensor MSI installer package.

## Prerequisites

```powershell
# Install .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# Install WiX Toolset v4
dotnet tool install --global wix --version 4.0.4
```

## Build Commands

### Quick Build
```powershell
cd installer
.\build-installer.ps1
```

### Build Options
```powershell
# Clean build
.\build-installer.ps1 -Clean

# Sign installer
.\build-installer.ps1 -Sign -CertificatePath "cert.pfx" -CertificatePassword "pass"

# Debug build
.\build-installer.ps1 -Configuration Debug
```

## Output

```
installer\bin\Release\x64\ContextSensor-x64.msi    # 64-bit Windows
installer\bin\Release\x86\ContextSensor-x86.msi    # 32-bit Windows
```

## Build Process

The script automatically:
1. Restores NuGet packages
2. Builds solution in Release mode
3. Publishes components as self-contained executables (x86 + x64)
4. Creates MSI packages for both platforms
5. Calculates SHA256 checksums

## Testing Installation

```powershell
# Silent install with logging
msiexec /i bin\Release\x64\ContextSensor-x64.msi /quiet /norestart /l*v install.log

# Verify services
Get-Service ContextSensor.* | Format-Table Name, Status, StartType

# Check logs
Get-Content "C:\ProgramData\ContextSensor\logs\monitor.log" -Tail 20
```

## Troubleshooting

**"Cannot find WiX Toolset"**
```powershell
dotnet tool install --global wix --version 4.0.4
```

**"Source file not found"**
```powershell
dotnet build ../ContextSensor.sln -c Release
```

## For More Information

See the complete [Installer Guide](GUIDE.md) for:
- Installation procedures
- Configuration options
- Deployment scenarios
- Troubleshooting
- Update manifests