namespace ContextSensor.Monitor;

public class MonitorConfiguration
{
    public MonitoringSettings Monitoring { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public class MonitoringSettings
{
    public string ProcessName { get; set; } = "ContextSensor.App";
    public string ProcessPath { get; set; } = string.Empty;
    public int CheckIntervalSeconds { get; set; } = 30;
    public int StartupDelaySeconds { get; set; } = 10;
    public int MaxRestartAttempts { get; set; } = 3;
    public int RestartDelaySeconds { get; set; } = 10;
    public double RestartBackoffMultiplier { get; set; } = 2.0;
    public int MaxRestartAttemptsPerHour { get; set; } = 10;
    public bool EnableAutoStart { get; set; } = true;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public string LogPath { get; set; } = string.Empty;
}
