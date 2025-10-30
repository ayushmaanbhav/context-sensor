using System.Text.Json;
using ContextSensor.Updater;
using FluentAssertions;
using Xunit;

namespace ContextSensor.UpdaterTests;

/// <summary>
/// Tests for update manifest parsing and validation
/// </summary>
public class UpdateManifestTests
{
    [Fact]
    public void Should_Parse_Valid_Manifest()
    {
        // Arrange
        var json = @"{
            ""version"": ""1.0.1"",
            ""releaseDate"": ""2025-11-01T00:00:00Z"",
            ""downloadUrl"": ""https://example.com/ContextSensor-1.0.1.msi"",
            ""checksum"": ""sha256:abc123def456"",
            ""releaseNotes"": ""Bug fixes and improvements"",
            ""minimumVersion"": ""1.0.0"",
            ""isMandatory"": false
        }";

        // Act
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        manifest.Should().NotBeNull();
        manifest!.Version.Should().Be("1.0.1");
        manifest.DownloadUrl.Should().Be("https://example.com/ContextSensor-1.0.1.msi");
        manifest.Checksum.Should().Be("sha256:abc123def456");
        manifest.ReleaseNotes.Should().Be("Bug fixes and improvements");
    }

    [Fact]
    public void Should_Handle_Manifest_With_Missing_Optional_Fields()
    {
        // Arrange
        var json = @"{
            ""version"": ""1.0.1"",
            ""releaseDate"": ""2025-11-01T00:00:00Z"",
            ""downloadUrl"": ""https://example.com/ContextSensor-1.0.1.msi""
        }";

        // Act
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        manifest.Should().NotBeNull();
        manifest!.Version.Should().Be("1.0.1");
        manifest.DownloadUrl.Should().Be("https://example.com/ContextSensor-1.0.1.msi");
    }

    [Fact]
    public void Should_Parse_Release_Date_Correctly()
    {
        // Arrange
        var json = @"{
            ""version"": ""1.0.1"",
            ""releaseDate"": ""2025-11-01T10:30:00Z"",
            ""downloadUrl"": ""https://example.com/ContextSensor-1.0.1.msi""
        }";

        // Act
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        manifest.Should().NotBeNull();
        manifest!.ReleaseDate.Year.Should().Be(2025);
        manifest.ReleaseDate.Month.Should().Be(11);
        manifest.ReleaseDate.Day.Should().Be(1);
    }

    [Fact]
    public void Should_Support_Different_Checksum_Formats()
    {
        // Arrange - Test with checksum prefix
        var json1 = @"{
            ""version"": ""1.0.1"",
            ""releaseDate"": ""2025-11-01T00:00:00Z"",
            ""downloadUrl"": ""https://example.com/ContextSensor-1.0.1.msi"",
            ""checksum"": ""sha256:abc123""
        }";

        // Arrange - Test without prefix
        var json2 = @"{
            ""version"": ""1.0.1"",
            ""releaseDate"": ""2025-11-01T00:00:00Z"",
            ""downloadUrl"": ""https://example.com/ContextSensor-1.0.1.msi"",
            ""checksum"": ""abc123""
        }";

        // Act
        var manifest1 = JsonSerializer.Deserialize<UpdateManifest>(json1, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var manifest2 = JsonSerializer.Deserialize<UpdateManifest>(json2, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        manifest1.Should().NotBeNull();
        manifest1!.Checksum.Should().StartWith("sha256:");
        
        manifest2.Should().NotBeNull();
        manifest2!.Checksum.Should().NotStartWith("sha256:");
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]  // Patch upgrade
    [InlineData("1.0.0", "1.1.0", true)]  // Minor upgrade
    [InlineData("1.0.0", "2.0.0", true)]  // Major upgrade
    [InlineData("1.0.1", "1.0.0", false)] // Downgrade
    [InlineData("1.0.0", "1.0.0", false)] // Same version
    public void Should_Compare_Versions_Correctly(string currentVersion, string availableVersion, bool shouldUpdate)
    {
        // Arrange
        var current = new Version(currentVersion);
        var available = new Version(availableVersion);

        // Act
        var isNewer = available > current;

        // Assert
        isNewer.Should().Be(shouldUpdate);
    }

    [Fact]
    public void Should_Serialize_Manifest_Correctly()
    {
        // Arrange
        var manifest = new UpdateManifest
        {
            Version = "1.0.1",
            ReleaseDate = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            DownloadUrl = "https://example.com/ContextSensor-1.0.1.msi",
            Checksum = "sha256:abc123",
            ReleaseNotes = "Test release"
        };

        // Act
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // Assert
        json.Should().Contain("\"version\": \"1.0.1\"");
        json.Should().Contain("\"downloadUrl\":");
        json.Should().Contain("\"checksum\":");
    }
}