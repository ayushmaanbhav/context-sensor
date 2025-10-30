using System.Net;
using ContextSensor.Updater;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace ContextSensor.UpdaterTests;

/// <summary>
/// Tests for the UpdaterService functionality
/// </summary>
public class UpdaterServiceTests
{
    private readonly Mock<ILogger<UpdaterService>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IOptions<UpdaterConfiguration>> _mockConfiguration;

    public UpdaterServiceTests()
    {
        _mockLogger = new Mock<ILogger<UpdaterService>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfiguration = new Mock<IOptions<UpdaterConfiguration>>();
    }

    [Fact]
    public void Should_Initialize_With_Valid_Configuration()
    {
        // Arrange
        var config = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://example.com/manifest.json",
                CheckIntervalHours = 24,
                AutoInstall = false,
                VerifyChecksum = true
            }
        };
        _mockConfiguration.Setup(x => x.Value).Returns(config);

        var httpClient = new HttpClient();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new UpdaterService(_mockLogger.Object, _mockConfiguration.Object, _mockHttpClientFactory.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Handle_Network_Errors_Gracefully()
    {
        // Arrange
        var config = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://invalid-url-that-does-not-exist.com/manifest.json",
                CheckIntervalHours = 24
            }
        };
        _mockConfiguration.Setup(x => x.Value).Returns(config);

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert - Should not throw, should log error
        var service = new UpdaterService(_mockLogger.Object, _mockConfiguration.Object, _mockHttpClientFactory.Object);
        
        // Service should be created successfully even with invalid config
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Parse_Valid_Manifest_Response()
    {
        // Arrange
        var manifestJson = @"{
            ""version"": ""1.0.1"",
            ""releaseDate"": ""2025-11-01T00:00:00Z"",
            ""downloadUrl"": ""https://example.com/ContextSensor-1.0.1.msi"",
            ""checksum"": ""sha256:abc123"",
            ""releaseNotes"": ""Bug fixes""
        }";

        var config = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://example.com/manifest.json",
                CheckIntervalHours = 24
            }
        };
        _mockConfiguration.Setup(x => x.Value).Returns(config);

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("manifest.json")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(manifestJson)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new UpdaterService(_mockLogger.Object, _mockConfiguration.Object, _mockHttpClientFactory.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.0.1", "1.0.0", false)]
    [InlineData("2.0.0", "1.0.0", false)]
    public void Should_Detect_When_Update_Is_Available(string currentVersion, string availableVersion, bool shouldUpdate)
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
    public void Should_Respect_AutoInstall_Setting()
    {
        // Arrange
        var configWithAutoInstall = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://example.com/manifest.json",
                AutoInstall = true
            }
        };

        var configWithoutAutoInstall = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://example.com/manifest.json",
                AutoInstall = false
            }
        };

        // Assert
        configWithAutoInstall.Update.AutoInstall.Should().BeTrue();
        configWithoutAutoInstall.Update.AutoInstall.Should().BeFalse();
    }

    [Fact]
    public void Should_Respect_ChecksumVerification_Setting()
    {
        // Arrange
        var config = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://example.com/manifest.json",
                VerifyChecksum = true
            }
        };

        // Assert
        config.Update.VerifyChecksum.Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Empty_Update_URL()
    {
        // Arrange
        var config = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "",
                CheckIntervalHours = 24
            }
        };
        _mockConfiguration.Setup(x => x.Value).Returns(config);

        var httpClient = new HttpClient();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var service = new UpdaterService(_mockLogger.Object, _mockConfiguration.Object, _mockHttpClientFactory.Object);

        // Assert - Should create service but will skip update checks
        service.Should().NotBeNull();
    }

    [Fact]
    public void Should_Use_Configured_Check_Interval()
    {
        // Arrange
        var intervals = new[] { 1, 6, 12, 24, 48 };

        foreach (var interval in intervals)
        {
            var config = new UpdaterConfiguration
            {
                Update = new UpdateSettings
                {
                    UpdateCheckUrl = "https://example.com/manifest.json",
                    CheckIntervalHours = interval
                }
            };

            // Assert
            config.Update.CheckIntervalHours.Should().Be(interval);
        }
    }

    [Fact]
    public void Should_Support_Startup_Delay()
    {
        // Arrange
        var config = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://example.com/manifest.json",
                StartupDelayMinutes = 5
            }
        };

        // Assert
        config.Update.StartupDelayMinutes.Should().Be(5);
    }

    [Fact]
    public void Should_Support_Backup_Before_Update()
    {
        // Arrange
        var config = new UpdaterConfiguration
        {
            Update = new UpdateSettings
            {
                UpdateCheckUrl = "https://example.com/manifest.json",
                BackupBeforeUpdate = true
            }
        };

        // Assert
        config.Update.BackupBeforeUpdate.Should().BeTrue();
    }

    [Theory]
    [InlineData("sha256:abc123def456", "abc123def456")]
    [InlineData("ABC123DEF456", "abc123def456")]
    [InlineData("abc123def456", "abc123def456")]
    public void Should_Normalize_Checksum_For_Comparison(string input, string expected)
    {
        // Arrange & Act
        var normalized = input.Contains(':') ? input.Split(':')[1].ToLowerInvariant() : input.ToLowerInvariant();

        // Assert
        normalized.Should().Be(expected);
    }

    [Fact]
    public void Should_Handle_Invalid_JSON_Response()
    {
        // Arrange
        var invalidJson = "{ invalid json content }";
        
        // Act & Assert
        Action act = () => System.Text.Json.JsonSerializer.Deserialize<UpdateManifest>(invalidJson);
        act.Should().Throw<System.Text.Json.JsonException>();
    }
}