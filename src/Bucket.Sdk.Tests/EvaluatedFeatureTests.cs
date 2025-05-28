namespace Bucket.Sdk.Tests;

public class EvaluatedFeatureTests
{
    private const string _key = "test-feature";
    private const bool _enabled = true;
    private const bool _override = true;
    private static readonly (string key, JsonElement payload) _config = ("config1", "test-value".AsJsonElement());

    [Fact]
    public void Constructor_WithKeyAndEnabled_InitializesProperties()
    {
        // Arrange & Act
        var feature = new EvaluatedFeature(_key, _enabled);

        // Assert
        Assert.Equal(_key, feature.Key);
        Assert.Equal(_enabled, feature.Enabled);
        Assert.False(feature.Override);
        Assert.Null(feature.Config);
    }

    [Fact]
    public void Constructor_WithOverride_InitializesProperties()
    {
        // Arrange & Act
        var feature = new EvaluatedFeature(_key, _enabled, _override);

        // Assert
        Assert.Equal(_key, feature.Key);
        Assert.Equal(_enabled, feature.Enabled);
        Assert.Equal(_override, feature.Override);
        Assert.Null(feature.Config);
    }

    [Fact]
    public void Constructor_WithConfig_InitializesProperties()
    {
        // Arrange & Act
        var feature = new EvaluatedFeature(_key, _enabled, _config);

        // Assert
        Assert.Equal(_key, feature.Key);
        Assert.Equal(_enabled, feature.Enabled);
        Assert.False(feature.Override);
        Assert.Equal(_config, feature.Config);
    }

    [Fact]
    public void Constructor_WithConfigAndOverride_InitializesProperties()
    {
        // Arrange & Act
        var feature = new EvaluatedFeature(_key, _enabled, _config, _override);

        // Assert
        Assert.Equal(_key, feature.Key);
        Assert.Equal(_enabled, feature.Enabled);
        Assert.Equal(_override, feature.Override);
        Assert.Equal(_config, feature.Config);
    }

    [Fact]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange & Act
        _ = Assert.Throws<ArgumentNullException>(() => _ = new EvaluatedFeature(null!, _enabled));
        _ = Assert.Throws<ArgumentException>(() => _ = new EvaluatedFeature(string.Empty, _enabled));
        _ = Assert.Throws<ArgumentNullException>(() =>
            _ = new EvaluatedFeature(_key, _enabled, (null!, "some-value".AsJsonElement())));
        _ = Assert.Throws<ArgumentException>(() =>
            _ = new EvaluatedFeature(_key, _enabled, (string.Empty, "some-value".AsJsonElement())));
    }

    [Fact]
    public void Constructor_WithNullConfig_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var feature = new EvaluatedFeature(_key, _enabled, null);

        // Assert
        Assert.Equal(_key, feature.Key);
        Assert.Equal(_enabled, feature.Enabled);
        Assert.False(feature.Override);
        Assert.Null(feature.Config);
    }

    [Fact]
    public void Constructor_WithNullPayload_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var config = ("config-key", ((object?) null).AsJsonElement());
        var feature = new EvaluatedFeature(_key, _enabled, config);

        // Assert
        Assert.Equal(_key, feature.Key);
        Assert.Equal(_enabled, feature.Enabled);
        Assert.False(feature.Override);
        Assert.Equal(config, feature.Config);
    }

    [Fact]
    public void SetEnabled_ChangesEnabledValue()
    {
        // Arrange
        var feature = new EvaluatedFeature(_key, false);

        // Act
        feature = feature with
        {
            Enabled = true
        };

        // Assert
        Assert.True(feature.Enabled);
    }

    [Fact]
    public void WithConfig_ChangesConfigValue()
    {
        // Arrange
        var feature = new EvaluatedFeature(_key, _enabled);
        var newConfig = ("new-config", "new-value".AsJsonElement());

        // Act
        feature = feature with
        {
            Config = newConfig
        };

        // Assert
        Assert.Equal(newConfig, feature.Config);
    }

    [Fact]
    public void EvaluatedFeature_RecordEquality_WorksAsExpected()
    {
        // Arrange
        var feature1 = new EvaluatedFeature(_key, _enabled, _config);
        var feature2 = new EvaluatedFeature(_key, _enabled, _config);
        var feature3 = new EvaluatedFeature(_key, !_enabled, _config);

        // Assert
        Assert.Equal(feature1, feature2);
        Assert.NotEqual(feature1, feature3);
        Assert.True(feature1 == feature2);
        Assert.False(feature1 == feature3);
    }

    [Fact]
    public void ToString_WithBasicProperties_ReturnsCorrectString()
    {
        // Arrange
        var feature = new EvaluatedFeature(_key, _enabled);

        // Act
        var result = feature.ToString();

        // Assert
        Assert.Equal("EvaluatedFeature { Key = test-feature, Enabled = True, Override = False, Config = , EvaluationContext = , FlagEvaluationDebugData = , ConfigEvaluationDebugData =  }", result);
    }

    [Fact]
    public void ToString_WithAllProperties_ReturnsCorrectString()
    {
        // Arrange
        var feature = new EvaluatedFeature(_key, _enabled, _config, _override);

        // Act
        var result = feature.ToString();

        // Assert
        Assert.Equal(
            "EvaluatedFeature { Key = test-feature, Enabled = True, Override = True, Config = (config1, test-value), EvaluationContext = , FlagEvaluationDebugData = , ConfigEvaluationDebugData =  }",
            result);
    }

    [Fact]
    public void ToString_WithInternalProperties_HandlesThemCorrectly()
    {
        // Arrange
        var evaluationContext = new Dictionary<string, object?> { { "userId", "test-user" }, { "country", "US" } };

        var flagDebugData = new EvaluationDebugData
        {
            Version = 5,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.MissingField, "userId")],
        };

        var configDebugData = new EvaluationDebugData { Version = 3, EvaluatedRules = [true], EvaluationIssues = [] };

        var feature = new EvaluatedFeature(_key, _enabled, _config)
        {
            FlagEvaluationDebugData = flagDebugData,
            ConfigEvaluationDebugData = configDebugData,
            EvaluationContext = evaluationContext
        };

        // Act
        var result = feature.ToString();

        // Assert
        Assert.Equal(
            "EvaluatedFeature { Key = test-feature, Enabled = True, Override = False, Config = (config1, test-value), EvaluationContext = { \"country\" = US, \"userId\" = test-user }, FlagEvaluationDebugData = EvaluationDebugData { Version = 5, EvaluatedRules = [ False, True ], EvaluationIssues = [ (MissingField, userId) ] }, ConfigEvaluationDebugData = EvaluationDebugData { Version = 3, EvaluatedRules = [ True ], EvaluationIssues = [ ] } }",
            result);
    }

    [Fact]
    public void Equals_WithIdenticalFeatures_ReturnsTrue()
    {
        // Arrange
        var evaluationContext = new Dictionary<string, object?> { { "userId", "test-user" }, { "country", "US" } };

        var flagDebugData = new EvaluationDebugData
        {
            Version = 5,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.MissingField, "userId")],
        };

        var configDebugData = new EvaluationDebugData
        {
            Version = 3,
            EvaluatedRules = [true],
            EvaluationIssues = []
        };

        var feature1 = new EvaluatedFeature(_key, _enabled, _config, _override)
        {
            FlagEvaluationDebugData = flagDebugData,
            ConfigEvaluationDebugData = configDebugData,
            EvaluationContext = evaluationContext
        };

        var feature2 = new EvaluatedFeature(_key, _enabled, _config, _override)
        {
            FlagEvaluationDebugData = flagDebugData,
            ConfigEvaluationDebugData = configDebugData,
            EvaluationContext = evaluationContext
        };

        // Act & Assert
        Assert.Equal(feature1, feature2);
        Assert.True(feature1 == feature2);
        Assert.False(feature1 != feature2);
    }

    [Fact]
    public void Equals_WithDifferentKey_ReturnsFalse()
    {
        // Arrange
        var feature1 = new EvaluatedFeature("feature1", _enabled);
        var feature2 = new EvaluatedFeature("feature2", _enabled);

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
        Assert.False(feature1 == feature2);
        Assert.True(feature1 != feature2);
    }

    [Fact]
    public void Equals_WithDifferentEnabled_ReturnsFalse()
    {
        // Arrange
        var feature1 = new EvaluatedFeature(_key, true);
        var feature2 = new EvaluatedFeature(_key, false);

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
    }

    [Fact]
    public void Equals_WithDifferentOverride_ReturnsFalse()
    {
        // Arrange
        var feature1 = new EvaluatedFeature(_key, _enabled, true);
        var feature2 = new EvaluatedFeature(_key, _enabled);

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
    }

    [Fact]
    public void Equals_WithDifferentConfigKey_ReturnsFalse()
    {
        // Arrange
        var config1 = ("config1", "value".AsJsonElement());
        var config2 = ("config2", "value".AsJsonElement());
        var feature1 = new EvaluatedFeature(_key, _enabled, config1);
        var feature2 = new EvaluatedFeature(_key, _enabled, config2);

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
    }

    [Fact]
    public void Equals_WithDifferentConfigPayload_ReturnsFalse()
    {
        // Arrange
        var config1 = ("config", "value1".AsJsonElement());
        var config2 = ("config", "value2".AsJsonElement());
        var feature1 = new EvaluatedFeature(_key, _enabled, config1);
        var feature2 = new EvaluatedFeature(_key, _enabled, config2);

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
    }

    [Fact]
    public void Equals_WithDifferentEvaluationContext_ReturnsFalse()
    {
        // Arrange
        var context1 = new Dictionary<string, object?> { { "userId", "user1" } };
        var context2 = new Dictionary<string, object?> { { "userId", "user2" } };

        var feature1 = new EvaluatedFeature(_key, _enabled) with
        {
            EvaluationContext = context1
        };
        var feature2 = new EvaluatedFeature(_key, _enabled) with
        {
            EvaluationContext = context2
        };

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
    }

    [Fact]
    public void Equals_WithDifferentFlagEvaluationDebugData_ReturnsFalse()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 1,
            EvaluatedRules = [true],
            EvaluationIssues = []
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 2,
            EvaluatedRules = [false],
            EvaluationIssues = []
        };

        var feature1 = new EvaluatedFeature(_key, _enabled) with
        {
            FlagEvaluationDebugData = debugData1
        };
        var feature2 = new EvaluatedFeature(_key, _enabled) with
        {
            FlagEvaluationDebugData = debugData2
        };

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
    }

    [Fact]
    public void Equals_WithDifferentConfigEvaluationDebugData_ReturnsFalse()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 1,
            EvaluatedRules = [true],
            EvaluationIssues = []
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 2,
            EvaluatedRules = [false],
            EvaluationIssues = []
        };

        var feature1 = new EvaluatedFeature(_key, _enabled) with
        {
            ConfigEvaluationDebugData = debugData1
        };
        var feature2 = new EvaluatedFeature(_key, _enabled) with
        {
            ConfigEvaluationDebugData = debugData2
        };

        // Act & Assert
        Assert.NotEqual(feature1, feature2);
    }

    [Fact]
    public void GetHashCode_WithIdenticalFeatures_ReturnsSameValue()
    {
        // Arrange
        var evaluationContext = new Dictionary<string, object?> { { "userId", "test-user" } };
        var debugData = new EvaluationDebugData
        {
            Version = 1,
            EvaluatedRules = [true],
            EvaluationIssues = []
        };

        var feature1 = new EvaluatedFeature(_key, _enabled, _config) with
        {
            FlagEvaluationDebugData = debugData,
            EvaluationContext = evaluationContext
        };

        var feature2 = new EvaluatedFeature(_key, _enabled, _config) with
        {
            FlagEvaluationDebugData = debugData,
            EvaluationContext = evaluationContext
        };

        // Act
        var hashCode1 = feature1.GetHashCode();
        var hashCode2 = feature2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentFeatures_ReturnsDifferentValues()
    {
        // Arrange
        var feature1 = new EvaluatedFeature("feature1", true);
        var feature2 = new EvaluatedFeature("feature2", false);

        // Act
        var hashCode1 = feature1.GetHashCode();
        var hashCode2 = feature2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
