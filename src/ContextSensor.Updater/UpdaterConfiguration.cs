namespace ContextSensor.Updater;

public class UpdaterConfiguration
{
    public UpdateSettings Update { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public class UpdateSettings
{
    public string UpdateCheckUrl { get; set; } = string.Empty;
    public int CheckIntervalHours { get; set; } = 4;
    public bool AutoInstall { get; set; } = true;
    public bool BackupBeforeUpdate { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public bool VerifyChecksum { get; set; } = true;
    public bool AllowDowngrade { get; set; } = false;
    public int StartupDelayMinutes { get; set; } = 2;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public string LogPath { get; set; } = string.Empty;
}
