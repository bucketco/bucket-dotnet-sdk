namespace Bucket.Sdk.Tests;

public sealed class ConfigurationTests
{
    private readonly IConfigurationRoot _systemConfiguration = new ConfigurationBuilder().Build();

    [Fact]
    public void Constructor_DefaultValues()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key" };

        // Assert
        Assert.Equal("test-key", config.SecretKey);
        Assert.Equal(new Uri("https://front.bucket.co"), config.ApiBaseUri);
        Assert.Equal(OperationMode.LocalEvaluation, config.Mode);
        Assert.Equal(100, config.Output.MaxMessages);
        Assert.Equal(TimeSpan.FromSeconds(10), config.Output.FlushInterval);
        Assert.Equal(TimeSpan.FromSeconds(60), config.Output.RollingWindow);
        Assert.Equal(TimeSpan.FromSeconds(60), config.Features.RefreshInterval);
        Assert.Equal(TimeSpan.FromMinutes(10), config.Features.StaleAge);
    }

    [Fact]
    public void SecretKey_ThrowsWhenInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentException>(() => new Configuration { SecretKey = "" });
        _ = Assert.Throws<ArgumentException>(() => new Configuration { SecretKey = "   " });
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Configuration { SecretKey = "test-key", ApiBaseUri = null! });
    }

    [Fact]
    public void ApiBaseUri_Valid_SetsValue()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key" };
        var newUri = new Uri("https://api.example.com");

        // Act
        config.ApiBaseUri = newUri;

        // Assert
        Assert.Equal(newUri, config.ApiBaseUri);
    }

    [Fact]
    public void Mode_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key" };

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => config.Mode = (OperationMode) 999);
    }

    [Fact]
    public void Mode_ValidValues_SetsValue()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key", Mode = OperationMode.Offline };

        // Assert
        Assert.Equal(OperationMode.Offline, config.Mode);

        // Act
        config.Mode = OperationMode.LocalEvaluation;

        // Assert
        Assert.Equal(OperationMode.LocalEvaluation, config.Mode);

        // Act
        config.Mode = OperationMode.RemoteEvaluation;

        // Assert
        Assert.Equal(OperationMode.RemoteEvaluation, config.Mode);
    }

    [Fact]
    public void Output_MaxMessages_LessThanOne_ThrowsArgumentOutOfRangeException() =>
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Configuration { SecretKey = "test-key", Output = { MaxMessages = 0 } });

    [Fact]
    public void Output_MaxMessages_ValidValue_SetsValue()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key", Output = { MaxMessages = 500 } };

        // Assert
        Assert.Equal(500, config.Output.MaxMessages);
    }

    [Fact]
    public void Output_FlushInterval_ZeroOrNegative_ThrowsArgumentOutOfRangeException() =>
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Configuration { SecretKey = "test-key", Output = { FlushInterval = TimeSpan.Zero } });

    [Fact]
    public void Output_FlushInterval_ValidValue_SetsValue()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key" };

        // Act
        var newInterval = TimeSpan.FromMinutes(5);
        config.Output.FlushInterval = newInterval;

        // Assert
        Assert.Equal(newInterval, config.Output.FlushInterval);
    }

    [Fact]
    public void Output_RollingWindow_ZeroOrNegative_ThrowsArgumentOutOfRangeException() =>
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Configuration { SecretKey = "test-key", Output = { RollingWindow = TimeSpan.Zero } });

    [Fact]
    public void Output_RollingWindow_ValidValue_SetsValue()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key" };

        // Act
        var newWindow = TimeSpan.FromMinutes(3);
        config.Output.RollingWindow = newWindow;

        // Assert
        Assert.Equal(newWindow, config.Output.RollingWindow);
    }

    [Fact]
    public void Features_RefreshInterval_ZeroOrNegative_ThrowsArgumentOutOfRangeException() =>
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Configuration { SecretKey = "test-key", Features = { RefreshInterval = TimeSpan.Zero } });


    [Fact]
    public void Features_RefreshInterval_ValidValue_SetsValue()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key" };

        // Act
        var newInterval = TimeSpan.FromMinutes(2);
        config.Features.RefreshInterval = newInterval;

        // Assert
        Assert.Equal(newInterval, config.Features.RefreshInterval);
    }

    [Fact]
    public void Features_StaleAge_ZeroOrNegative_ThrowsArgumentOutOfRangeException() =>
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Configuration { SecretKey = "test-key", Features = { StaleAge = TimeSpan.Zero } });


    [Fact]
    public void Features_StaleAge_ValidValue_SetsValue()
    {
        // Arrange
        var config = new Configuration { SecretKey = "test-key" };

        // Act
        var newAge = TimeSpan.FromHours(1);
        config.Features.StaleAge = newAge;

        // Assert
        Assert.Equal(newAge, config.Features.StaleAge);
    }

    [Fact]
    public void FromConfiguration_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => Configuration.FromConfiguration(null!));
        _ = Assert.Throws<ArgumentNullException>(() => Configuration.FromConfiguration(_systemConfiguration, null!));
        _ = Assert.Throws<ArgumentException>(() => Configuration.FromConfiguration(_systemConfiguration, ""));
        _ = Assert.Throws<ArgumentException>(() =>
            Configuration.FromConfiguration(_systemConfiguration, "NonExistentSection"));
    }

    [Fact]
    public void FromConfiguration_ValidSection_ReturnsConfiguration()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "Bucket:SecretKey", "config-test-key" },
            { "Bucket:ApiBaseUri", "https://api.test.com" },
            { "Bucket:Mode", "Offline" },
            { "Bucket:Output:MaxMessages", "200" },
            { "Bucket:Output:FlushInterval", "00:05:00" },
            { "Bucket:Output:RollingWindow", "00:02:00" },
            { "Bucket:Features:RefreshInterval", "00:03:00" },
            { "Bucket:Features:StaleAge", "01:00:00" },
        };

        var systemConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData).Build();

        // Act
        var bucketConfig = Configuration.FromConfiguration(systemConfiguration);

        // Assert
        Assert.Equal("config-test-key", bucketConfig.SecretKey);
        Assert.Equal(new Uri("https://api.test.com"), bucketConfig.ApiBaseUri);
        Assert.Equal(OperationMode.Offline, bucketConfig.Mode);
        Assert.Equal(200, bucketConfig.Output.MaxMessages);
        Assert.Equal(TimeSpan.FromMinutes(5), bucketConfig.Output.FlushInterval);
        Assert.Equal(TimeSpan.FromMinutes(2), bucketConfig.Output.RollingWindow);
        Assert.Equal(TimeSpan.FromMinutes(3), bucketConfig.Features.RefreshInterval);
        Assert.Equal(TimeSpan.FromHours(1), bucketConfig.Features.StaleAge);
    }

    [Fact]
    public void FromConfiguration_CustomSectionName_ReturnsConfiguration()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "CustomSection:SecretKey", "custom-section-key" },
            { "CustomSection:ApiBaseUri", "https://custom.example.com" },
            { "CustomSection:Mode", "RemoteEvaluation" },
        };

        // Act
        var systemConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData).Build();

        var bucketConfig = Configuration.FromConfiguration(systemConfiguration, "CustomSection");

        // Assert
        Assert.Equal("custom-section-key", bucketConfig.SecretKey);
        Assert.Equal(new Uri("https://custom.example.com"), bucketConfig.ApiBaseUri);
        Assert.Equal(OperationMode.RemoteEvaluation, bucketConfig.Mode);
    }
}
