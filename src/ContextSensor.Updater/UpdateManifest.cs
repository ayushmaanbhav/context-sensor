using System.Text.Json.Serialization;

namespace ContextSensor.Updater;

public class UpdateManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("releaseDate")]
    public DateTime ReleaseDate { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; set; } = string.Empty;

    [JsonPropertyName("minimumVersion")]
    public string MinimumVersion { get; set; } = string.Empty;

    [JsonPropertyName("mandatoryUpdate")]
    public bool MandatoryUpdate { get; set; }
}
