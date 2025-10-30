using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace ContextSensor.Updater;

public class UpdaterService : BackgroundService
{
    private readonly ILogger<UpdaterService> _logger;
    private readonly UpdateSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;
    private readonly string _tempDirectory;

    public UpdaterService(
        ILogger<UpdaterService> logger,
        IOptions<UpdaterConfiguration> configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = configuration.Value.Update;
        _httpClient = httpClientFactory.CreateClient();
        _currentVersion = GetCurrentVersion();
        _tempDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ContextSensor", "temp", "updates");
        
        Directory.CreateDirectory(_tempDirectory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Update Service starting...");
        _logger.LogInformation("Current version: {Version}", _currentVersion);
        _logger.LogInformation("Update check URL: {Url}", _settings.UpdateCheckUrl);
        _logger.LogInformation("Check interval: {Interval} hours", _settings.CheckIntervalHours);

        // Wait for initial startup delay
        if (_settings.StartupDelayMinutes > 0)
        {
            _logger.LogInformation("Waiting {Delay} minutes before first update check...", 
                _settings.StartupDelayMinutes);
            await Task.Delay(TimeSpan.FromMinutes(_settings.StartupDelayMinutes), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForUpdatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
            }

            await Task.Delay(TimeSpan.FromHours(_settings.CheckIntervalHours), stoppingToken);
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.UpdateCheckUrl))
        {
            _logger.LogWarning("Update check URL is not configured");
            return;
        }

        _logger.LogInformation("Checking for updates...");

        try
        {
            // Download update manifest
            var manifest = await DownloadManifestAsync(cancellationToken);
            if (manifest == null)
            {
                _logger.LogWarning("Failed to download update manifest");
                return;
            }

            _logger.LogInformation("Latest version available: {Version}", manifest.Version);

            // Compare versions
            if (!IsNewerVersion(manifest.Version, _currentVersion))
            {
                _logger.LogInformation("No updates available. Current version {Current} is up to date.", 
                    _currentVersion);
                return;
            }

            _logger.LogInformation("New version available: {NewVersion} (current: {CurrentVersion})", 
                manifest.Version, _currentVersion);
            _logger.LogInformation("Release notes: {ReleaseNotes}", manifest.ReleaseNotes);

            if (_settings.AutoInstall)
            {
                _logger.LogInformation("Auto-install is enabled. Starting update process...");
                await InstallUpdateAsync(manifest, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Auto-install is disabled. Update available but not installing.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update check");
        }
    }

    private async Task<UpdateManifest?> DownloadManifestAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(_settings.UpdateCheckUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading update manifest");
            return null;
        }
    }

    private async Task InstallUpdateAsync(UpdateManifest manifest, CancellationToken cancellationToken)
    {
        var downloadPath = Path.Combine(_tempDirectory, $"ContextSensor-{manifest.Version}.msi");

        try
        {
            // Download MSI package
            _logger.LogInformation("Downloading update from {Url}...", manifest.DownloadUrl);
            await DownloadFileAsync(manifest.DownloadUrl, downloadPath, cancellationToken);
            _logger.LogInformation("Download completed: {Path}", downloadPath);

            // Verify checksum
            if (_settings.VerifyChecksum && !string.IsNullOrWhiteSpace(manifest.Checksum))
            {
                _logger.LogInformation("Verifying checksum...");
                if (!VerifyChecksum(downloadPath, manifest.Checksum))
                {
                    _logger.LogError("Checksum verification failed. Aborting update.");
                    File.Delete(downloadPath);
                    return;
                }
                _logger.LogInformation("Checksum verification passed");
            }

            // Backup if configured
            if (_settings.BackupBeforeUpdate)
            {
                _logger.LogInformation("Creating backup before update...");
                await CreateBackupAsync(cancellationToken);
            }

            // Stop services
            _logger.LogInformation("Stopping ContextSensor services...");
            StopService("ContextSensor.Monitor");
            await Task.Delay(2000, cancellationToken); // Wait for monitor to stop app

            // Install update
            _logger.LogInformation("Installing update...");
            var success = await InstallMsiAsync(downloadPath, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Update installed successfully");
                
                // Services will be restarted automatically by Windows
                // since they're configured as Automatic start type
                
                // Clean up
                try
                {
                    File.Delete(downloadPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary update file");
                }
            }
            else
            {
                _logger.LogError("Update installation failed");
                
                // Restart services since update failed
                StartService("ContextSensor.Monitor");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update installation");
            
            // Attempt to restart services
            try
            {
                StartService("ContextSensor.Monitor");
            }
            catch (Exception restartEx)
            {
                _logger.LogError(restartEx, "Failed to restart services after failed update");
            }
        }
    }

    private async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cancellationToken);
    }

    private bool VerifyChecksum(string filePath, string expectedChecksum)
    {
        try
        {
            // Expected format: "sha256:checksum" or just "checksum"
            var checksumValue = expectedChecksum.Contains(':') 
                ? expectedChecksum.Split(':')[1] 
                : expectedChecksum;

            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            var actualChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return actualChecksum.Equals(checksumValue, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying checksum");
            return false;
        }
    }

    private async Task CreateBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var installPath = GetInstallPath();
            if (string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath))
            {
                _logger.LogWarning("Install path not found. Skipping backup.");
                return;
            }

            var backupPath = Path.Combine(_tempDirectory, $"backup-{DateTime.UtcNow:yyyyMMddHHmmss}");
            Directory.CreateDirectory(backupPath);

            // Simple directory copy (in production, consider using more robust backup)
            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(installPath))
                {
                    var fileName = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(backupPath, fileName), true);
                }
            }, cancellationToken);

            _logger.LogInformation("Backup created at: {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup");
        }
    }

    private async Task<bool> InstallMsiAsync(string msiPath, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{msiPath}\" /quiet /norestart /l*v \"{Path.Combine(_tempDirectory, "install.log")}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start msiexec process");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            
            // Exit code 0 or 3010 (reboot required) are success
            var success = process.ExitCode == 0 || process.ExitCode == 3010;
            
            if (!success)
            {
                _logger.LogError("MSI installation failed with exit code: {ExitCode}", process.ExitCode);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MSI installer");
            return false;
        }
    }

    private void StopService(string serviceName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"stop \"{serviceName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            
            _logger.LogInformation("Service {ServiceName} stop command executed", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping service {ServiceName}", serviceName);
        }
    }

    private void StartService(string serviceName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"start \"{serviceName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            
            _logger.LogInformation("Service {ServiceName} start command executed", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting service {ServiceName}", serviceName);
        }
    }

    private string GetCurrentVersion()
    {
        try
        {
            // Try to get version from registry
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\ContextSensor");
            var version = key?.GetValue("Version") as string;
            return version ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    private string GetInstallPath()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\ContextSensor");
            return key?.GetValue("InstallPath") as string ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private bool IsNewerVersion(string availableVersion, string currentVersion)
    {
        try
        {
            // Simple version comparison (e.g., "1.0.1" vs "1.0.0")
            var available = new Version(availableVersion);
            var current = new Version(currentVersion);
            
            if (!_settings.AllowDowngrade && available < current)
            {
                return false;
            }
            
            return available > current;
        }
        catch
        {
            _logger.LogWarning("Failed to parse versions. Available: {Available}, Current: {Current}", 
                availableVersion, currentVersion);
            return false;
        }
    }
}
