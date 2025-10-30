# ContextSensor Installer Build Script
# Builds the MSI installer package

param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Sign,
    [string]$CertificatePath = "",
    [string]$CertificatePassword = ""
)

$ErrorActionPreference = "Stop"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "ContextSensor Installer Builder" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Get paths
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionDir = Split-Path -Parent $scriptPath
$installerDir = Join-Path $solutionDir "installer"
$outputDir = Join-Path $installerDir "bin\$Configuration"

Write-Host "Solution Directory: $solutionDir" -ForegroundColor Gray
Write-Host "Installer Directory: $installerDir" -ForegroundColor Gray
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $outputDir) {
        Remove-Item -Path $outputDir -Recurse -Force
    }
    Write-Host "Clean complete." -ForegroundColor Green
    Write-Host ""
}

# Step 1: Restore NuGet packages
Write-Host "Step 1: Restoring NuGet packages..." -ForegroundColor Yellow
Push-Location $solutionDir
try {
    dotnet restore ContextSensor.sln
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed"
    }
    Write-Host "NuGet restore complete." -ForegroundColor Green
}
finally {
    Pop-Location
}
Write-Host ""

# Step 2: Build all projects
Write-Host "Step 2: Building solution..." -ForegroundColor Yellow
Push-Location $solutionDir
try {
    dotnet build ContextSensor.sln -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed"
    }
    Write-Host "Solution build complete." -ForegroundColor Green
}
finally {
    Pop-Location
}
Write-Host ""

# Step 2.5: Publish App and Windows Services as self-contained single-file (x86 & x64)
Write-Host "Step 2.5: Publishing app and services (self-contained single-file)..." -ForegroundColor Yellow
$platforms = @('x86','x64')
foreach ($plat in $platforms) {
    $rid = "win-$plat"

    Write-Host (" - Publishing App ({0})" -f $rid) -ForegroundColor Yellow
    dotnet publish "src/ContextSensor.App/ContextSensor.App.csproj" -c $Configuration -r $rid --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for App ($rid)" }

    Write-Host (" - Publishing Monitor ({0})" -f $rid) -ForegroundColor Yellow
    dotnet publish "src/ContextSensor.Monitor/ContextSensor.Monitor.csproj" -c $Configuration -r $rid --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for Monitor ($rid)" }

    Write-Host (" - Publishing Updater ({0})" -f $rid) -ForegroundColor Yellow
    dotnet publish "src/ContextSensor.Updater/ContextSensor.Updater.csproj" -c $Configuration -r $rid --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for Updater ($rid)" }
}
Write-Host "App and service publish complete." -ForegroundColor Green
Write-Host ""

# Step 3: Build installer
Write-Host "Step 3: Building MSI installer (x86 and x64)..." -ForegroundColor Yellow
Push-Location $installerDir
try {
    $platforms = @('x86', 'x64')
    foreach ($plat in $platforms) {
        Write-Host (" - Building platform: {0}" -f $plat) -ForegroundColor Yellow
        dotnet build ContextSensor.Installer.wixproj -c $Configuration -p:Platform=$plat
        if ($LASTEXITCODE -ne 0) {
            throw "Installer build failed for platform $plat"
        }
    }
    Write-Host "Installer build complete." -ForegroundColor Green
}
finally {
    Pop-Location
}
Write-Host ""

# Step 4: Find MSI files (x86 and x64)
$msiFiles = Get-ChildItem -Path (Join-Path $installerDir "bin") -Filter "*.msi" -Recurse | Sort-Object LastWriteTime -Descending
if (-not $msiFiles) {
    throw "MSI files not found in output directory"
}

# Pick latest per platform by folder name or filename suffix
$latestX64 = $msiFiles | Where-Object { $_.FullName -match '\\x64\\' -or $_.BaseName -match '-x64$' } | Select-Object -First 1
$latestX86 = $msiFiles | Where-Object { $_.FullName -match '\\x86\\' -or $_.BaseName -match '-x86$' } | Select-Object -First 1

if (-not $latestX64 -and -not $latestX86) {
    # Fallback: choose the most recent single MSI
    $latest = $msiFiles | Select-Object -First 1
    Write-Host "MSI Package: $($latest.FullName)" -ForegroundColor Cyan
    Write-Host "Size: $([math]::Round($latest.Length / 1MB, 2)) MB" -ForegroundColor Cyan
} else {
    if ($latestX64) {
        Write-Host "MSI Package (x64): $($latestX64.FullName)" -ForegroundColor Cyan
        Write-Host "Size (x64): $([math]::Round($latestX64.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    }
    if ($latestX86) {
        Write-Host "MSI Package (x86): $($latestX86.FullName)" -ForegroundColor Cyan
        Write-Host "Size (x86): $([math]::Round($latestX86.Length / 1MB, 2)) MB" -ForegroundColor Cyan
    }
}

# Preserve existing variables for subsequent steps by selecting a default MSI
$msiFile = $latestX64
if (-not $msiFile) { $msiFile = $latestX86 }
if (-not $msiFile) { $msiFile = $msiFiles | Select-Object -First 1 }

Write-Host ""

# Step 5: Sign if requested
if ($Sign) {
    if (-not $CertificatePath) {
        Write-Host "Warning: Sign requested but no certificate path provided. Skipping signing." -ForegroundColor Yellow
    }
    elseif (-not (Test-Path $CertificatePath)) {
        Write-Host "Warning: Certificate file not found: $CertificatePath. Skipping signing." -ForegroundColor Yellow
    }
    else {
        Write-Host "Step 4: Signing MSI package..." -ForegroundColor Yellow
        
        $signArgs = @(
            "sign",
            "/f", $CertificatePath,
            "/t", "http://timestamp.digicert.com"
        )
        
        if ($CertificatePassword) {
            $signArgs += @("/p", $CertificatePassword)
        }
        
        $signArgs += $msiFile.FullName
        
        & signtool.exe $signArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "MSI package signed successfully." -ForegroundColor Green
        }
        else {
            Write-Host "Warning: MSI signing failed." -ForegroundColor Yellow
        }
        Write-Host ""
    }
}

# Step 6: Calculate checksum
Write-Host "Calculating SHA256 checksum..." -ForegroundColor Yellow
$hash = Get-FileHash -Path $msiFile.FullName -Algorithm SHA256
Write-Host "SHA256: $($hash.Hash.ToLower())" -ForegroundColor Cyan
Write-Host ""

# Summary
Write-Host "================================" -ForegroundColor Green
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output: $($msiFile.FullName)" -ForegroundColor White
Write-Host ""
Write-Host "To install:" -ForegroundColor Gray
Write-Host "  msiexec /i `"$($msiFile.FullName)`" /l*v install.log" -ForegroundColor Gray
Write-Host ""
Write-Host "To install silently:" -ForegroundColor Gray
Write-Host "  msiexec /i `"$($msiFile.FullName)`" /quiet /norestart" -ForegroundColor Gray
Write-Host ""
