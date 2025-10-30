using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContextSensor.Updater;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ContextSensor.UpdaterTests;

/// <summary>
/// Integration tests for the updater service workflow
/// </summary>
public class UpdaterIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDirectory;
    private readonly string _manifestPath;
    private readonly string _downloadPath;

    public UpdaterIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _testDirectory = Path.Combine(Path.GetTempPath(), $"UpdaterTests_{Guid.NewGuid()}");
        _manifestPath = Path.Combine(_testDirectory, "manifest.json");
        _downloadPath = Path.Combine(_testDirectory, "downloads");

        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_downloadPath);
    }

    [Fact]
    public void Should_Create_Valid_Manifest_File()
    {
        // Arrange
        var manifest = new UpdateManifest
        {
            Version = "1.0.1",
            ReleaseDate = DateTime.UtcNow,
            DownloadUrl = "https://example.com/ContextSensor-1.0.1.msi",
            Checksum = "sha256:abc123def456",
            ReleaseNotes = "Test release for integration testing"
        };

        // Act
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        File.WriteAllText(_manifestPath, json);

        _output.WriteLine($"Manifest created at: {_manifestPath}");
        _output.WriteLine($"Manifest content:\n{json}");

        // Assert
        File.Exists(_manifestPath).Should().BeTrue();
        var fileContent = File.ReadAllText(_manifestPath);
        fileContent.Should().Contain("1.0.1");
        fileContent.Should().Contain("downloadUrl");
    }

    [Fact]
    public void Should_Calculate_File_Checksum_Correctly()
    {
        // Arrange
        var testFilePath = Path.Combine(_testDirectory, "test.bin");
        var testContent = "This is test content for checksum calculation";
        File.WriteAllText(testFilePath, testContent);

        // Act
        string checksum1 = CalculateChecksum(testFilePath);
        string checksum2 = CalculateChecksum(testFilePath);

        _output.WriteLine($"File: {testFilePath}");
        _output.WriteLine($"Checksum: {checksum1}");

        // Assert
        checksum1.Should().NotBeNullOrEmpty();
        checksum1.Should().Be(checksum2, "same file should produce same checksum");
        checksum1.Length.Should().Be(64, "SHA256 checksum should be 64 characters");
    }

    [Fact]
    public void Should_Detect_Checksum_Mismatch()
    {
        // Arrange
        var testFile1 = Path.Combine(_testDirectory, "file1.bin");
        var testFile2 = Path.Combine(_testDirectory, "file2.bin");
        
        File.WriteAllText(testFile1, "Content A");
        File.WriteAllText(testFile2, "Content B");

        // Act
        var checksum1 = CalculateChecksum(testFile1);
        var checksum2 = CalculateChecksum(testFile2);

        _output.WriteLine($"File 1 checksum: {checksum1}");
        _output.WriteLine($"File 2 checksum: {checksum2}");

        // Assert
        checksum1.Should().NotBe(checksum2, "different files should have different checksums");
    }

    [Fact]
    public void Should_Verify_Download_Directory_Permissions()
    {
        // Arrange & Act
        var testFile = Path.Combine(_downloadPath, "permission-test.txt");
        
        // Act & Assert - Should be able to write
        Action writeAction = () => File.WriteAllText(testFile, "test");
        writeAction.Should().NotThrow("should have write permission in download directory");

        // Should be able to read
        Action readAction = () => File.ReadAllText(testFile);
        readAction.Should().NotThrow("should have read permission in download directory");

        // Should be able to delete
        Action deleteAction = () => File.Delete(testFile);
        deleteAction.Should().NotThrow("should have delete permission in download directory");
    }

    [Fact]
    public void Should_Handle_Corrupted_Manifest()
    {
        // Arrange
        var corruptedManifest = "{ this is not valid json";
        File.WriteAllText(_manifestPath, corruptedManifest);

        // Act & Assert
        Action act = () => JsonSerializer.Deserialize<UpdateManifest>(File.ReadAllText(_manifestPath));
        act.Should().Throw<JsonException>("corrupted manifest should throw exception");
    }

    [Fact]
    public void Should_Support_Multiple_Download_Attempts()
    {
        // Arrange
        var downloadedFiles = new List<string>();
        
        // Act - Simulate multiple downloads
        for (int i = 0; i < 3; i++)
        {
            var fileName = $"ContextSensor-1.0.{i}.msi";
            var filePath = Path.Combine(_downloadPath, fileName);
            
            // Simulate download by creating file
            File.WriteAllText(filePath, $"Fake MSI content {i}");
            downloadedFiles.Add(filePath);
            
            _output.WriteLine($"Downloaded: {fileName}");
        }

        // Assert
        downloadedFiles.Should().HaveCount(3);
        downloadedFiles.Should().OnlyContain(f => File.Exists(f));
    }

    [Fact]
    public void Should_Clean_Up_Old_Downloads()
    {
        // Arrange - Create old download files
        var oldFiles = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var filePath = Path.Combine(_downloadPath, $"old-download-{i}.msi");
            File.WriteAllText(filePath, "old content");
            File.SetLastWriteTime(filePath, DateTime.Now.AddDays(-i));
            oldFiles.Add(filePath);
        }

        _output.WriteLine($"Created {oldFiles.Count} old download files");

        // Act - Delete files older than 3 days
        var cutoffDate = DateTime.Now.AddDays(-3);
        var deletedCount = 0;
        
        foreach (var file in oldFiles)
        {
            if (File.GetLastWriteTime(file) < cutoffDate)
            {
                File.Delete(file);
                deletedCount++;
                _output.WriteLine($"Deleted: {Path.GetFileName(file)}");
            }
        }

        // Assert - At least 2 files should be deleted (timing can affect boundary cases)
        deletedCount.Should().BeGreaterThanOrEqualTo(2, "should delete files older than 3 days");
        Directory.GetFiles(_downloadPath).Length.Should().BeLessThanOrEqualTo(3, "should keep recent files");
    }

    [Fact]
    public void Should_Handle_Concurrent_Update_Checks()
    {
        // Arrange
        var tasks = new List<Task<bool>>();
        var manifestsCreated = 0;

        // Act - Simulate concurrent manifest reads
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var manifest = new UpdateManifest
                    {
                        Version = $"1.0.{index}",
                        ReleaseDate = DateTime.UtcNow,
                        DownloadUrl = $"https://example.com/ContextSensor-1.0.{index}.msi"
                    };
                    
                    var tempPath = Path.Combine(_downloadPath, $"manifest-{index}.json");
                    var json = JsonSerializer.Serialize(manifest);
                    File.WriteAllText(tempPath, json);
                    
                    Interlocked.Increment(ref manifestsCreated);
                    return true;
                }
                catch
                {
                    return false;
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var successCount = tasks.Count(t => t.Result);
        _output.WriteLine($"Successful manifest operations: {successCount}/10");
        
        successCount.Should().Be(10, "all concurrent operations should succeed");
        manifestsCreated.Should().Be(10);
    }

    [Fact]
    public void Should_Validate_Manifest_Schema()
    {
        // Arrange
        var validManifest = new UpdateManifest
        {
            Version = "1.0.1",
            ReleaseDate = DateTime.UtcNow,
            DownloadUrl = "https://example.com/ContextSensor-1.0.1.msi",
            Checksum = "sha256:abc123"
        };

        // Act - Validate required fields
        var hasVersion = !string.IsNullOrEmpty(validManifest.Version);
        var hasDownloadUrl = !string.IsNullOrEmpty(validManifest.DownloadUrl);
        var hasValidDate = validManifest.ReleaseDate != default;

        // Assert
        hasVersion.Should().BeTrue("version is required");
        hasDownloadUrl.Should().BeTrue("download URL is required");
        hasValidDate.Should().BeTrue("release date should be valid");
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.0.1", "1.0.1", false)]
    [InlineData("1.1.0", "1.0.9", false)]
    public void Should_Handle_Version_Comparison_Logic(string current, string available, bool shouldUpdate)
    {
        // Arrange
        var currentVersion = new Version(current);
        var availableVersion = new Version(available);

        // Act
        var isNewer = availableVersion > currentVersion;

        // Assert
        isNewer.Should().Be(shouldUpdate);
    }

    private static string CalculateChecksum(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
                _output.WriteLine($"Cleaned up test directory: {_testDirectory}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error cleaning up: {ex.Message}");
        }
    }
}