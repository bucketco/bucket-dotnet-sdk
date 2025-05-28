namespace Bucket.Sdk.Tests;

public sealed class FeatureTests
{
    private const string _key = "test-feature";
    private const bool _enabled = true;
    private static readonly (string, string) _config = ("config1", "test-value");
    private int _checkConfigCalled;

    private int _checkEnabledCalled;
    private int _trackCalled;

    private void CheckEnabledStateCallback() => _checkEnabledCalled++;
    private void CheckConfigCallback() => _checkConfigCalled++;
    private void TrackCallback() => _trackCalled++;

    [Fact]
    public void Feature_WorksAsExpected()
    {
        var feature = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.Equal(_key, feature.Key);
        Assert.Equal(0, _checkEnabledCalled);
        Assert.Equal(0, _trackCalled);

        var result = feature.Enabled;

        Assert.Equal(_enabled, result);
        Assert.Equal(1, _checkEnabledCalled);
        Assert.Equal(0, _trackCalled);

        feature.Track();

        Assert.Equal(1, _checkEnabledCalled);
        Assert.Equal(1, _trackCalled);

        _ = feature.Enabled;
        feature.Track();

        Assert.Equal(2, _checkEnabledCalled);
        Assert.Equal(2, _trackCalled);
    }

    [Fact]
    public void Feature_WorksAsExpected_WithFalseEnabled()
    {
        var feature = new Feature(
            _key,
            false,
            CheckEnabledStateCallback,
            TrackCallback
        );

        Assert.False(feature.Enabled);
        Assert.Equal(1, _checkEnabledCalled);
    }

    [Fact]
    public void Feature_LetsExceptionsPassThrough()
    {
        var feature = new Feature(
            _key,
            false,
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException()
        );

        _ = Assert.Throws<InvalidOperationException>(() => feature.Enabled);
        _ = Assert.Throws<InvalidOperationException>(feature.Track);
    }

    [Fact]
    public void FeatureGeneric_WorksAsExpected()
    {
        var feature = new Feature<string>(
            _key,
            _enabled,
            CheckEnabledStateCallback,
            CheckConfigCallback,
            TrackCallback,
            _config
        );

        Assert.Equal(_key, feature.Key);
        Assert.Equal(0, _checkEnabledCalled);
        Assert.Equal(0, _trackCalled);

        Assert.Equal(_enabled, feature.Enabled);
        Assert.Equal(1, _checkEnabledCalled);
        Assert.Equal(0, _checkConfigCalled);
        Assert.Equal(0, _trackCalled);

        Assert.Equal(_config, feature.Config);
        Assert.Equal(1, _checkEnabledCalled);
        Assert.Equal(1, _checkConfigCalled);
        Assert.Equal(0, _trackCalled);

        feature.Track();
        Assert.Equal(1, _checkEnabledCalled);
        Assert.Equal(1, _checkConfigCalled);
        Assert.Equal(1, _trackCalled);

        _ = feature.Enabled;
        _ = feature.Config;
        feature.Track();

        Assert.Equal(2, _checkEnabledCalled);
        Assert.Equal(2, _checkConfigCalled);
        Assert.Equal(2, _trackCalled);
    }

    [Fact]
    public void FeatureGeneric_WorksAsExpected_WithFalseEnabled()
    {
        var feature = new Feature<string>(
            _key,
            false,
            CheckEnabledStateCallback,
            CheckConfigCallback,
            TrackCallback,
            _config
        );

        Assert.False(feature.Enabled);
        Assert.Equal(1, _checkEnabledCalled);
    }

    [Fact]
    public void FeatureGeneric_WorksAsExpected_WithNullPayload()
    {
        var nullConfig = ("config1", (string?) null);
        var feature = new Feature<string>(
            _key,
            _enabled,
            CheckEnabledStateCallback,
            CheckConfigCallback,
            TrackCallback,
            nullConfig
        );

        Assert.Equal(nullConfig, feature.Config);
        Assert.Equal(1, _checkConfigCalled);
    }

    [Fact]
    public void FeatureGeneric_LetsExceptionsPassThrough()
    {
        var feature = new Feature<string>(
            _key,
            false,
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            () => throw new InvalidOperationException(),
            _config
        );

        _ = Assert.Throws<InvalidOperationException>(() => feature.Enabled);
        _ = Assert.Throws<InvalidOperationException>(() => feature.Config);
        _ = Assert.Throws<InvalidOperationException>(feature.Track);
    }

    [Fact]
    public void Feature_ToString_ReturnsExpectedFormat()
    {
        var feature = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);

        var expected = $"Feature {{ Key = {_key}, Enabled = {_enabled} }}";
        Assert.Equal(expected, feature.ToString());
    }

    [Fact]
    public void Feature_Equals_ReturnsTrueForSameValues()
    {
        var feature1 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);
        var feature2 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.True(feature1.Equals(feature2));
        Assert.True(feature1 == feature2);
        Assert.False(feature1 != feature2);
    }

    [Fact]
    public void Feature_Equals_ReturnsFalseForDifferentKey()
    {
        var feature1 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);
        var feature2 = new Feature("different-key", _enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.False(feature1.Equals(feature2));
        Assert.False(feature1 == feature2);
        Assert.True(feature1 != feature2);
    }

    [Fact]
    public void Feature_Equals_ReturnsFalseForDifferentEnabledValue()
    {
        var feature1 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);
        var feature2 = new Feature(_key, !_enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.False(feature1.Equals(feature2));
        Assert.False(feature1 == feature2);
        Assert.True(feature1 != feature2);
    }

    [Fact]
    public void Feature_Equals_ReturnsFalseForNull()
    {
        var feature1 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.False(feature1.Equals(null));
    }

    [Fact]
    public void Feature_GetHashCode_ReturnsSameValueForEqualObjects()
    {
        var feature1 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);
        var feature2 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.Equal(feature1.GetHashCode(), feature2.GetHashCode());
    }

    [Fact]
    public void Feature_GetHashCode_ReturnsDifferentValueForDifferentKey()
    {
        var feature1 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);
        var feature2 = new Feature("different-key", _enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.NotEqual(feature1.GetHashCode(), feature2.GetHashCode());
    }

    [Fact]
    public void Feature_GetHashCode_ReturnsDifferentValueForDifferentEnabledValue()
    {
        var feature1 = new Feature(_key, _enabled, CheckEnabledStateCallback, TrackCallback);
        var feature2 = new Feature(_key, !_enabled, CheckEnabledStateCallback, TrackCallback);

        Assert.NotEqual(feature1.GetHashCode(), feature2.GetHashCode());
    }

    [Fact]
    public void FeatureGeneric_ToString_ReturnsExpectedFormat()
    {
        var feature = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback, TrackCallback,
            _config);

        var expected = $"Feature {{ Key = {_key}, Enabled = {_enabled}, Config = {_config} }}";
        Assert.Equal(expected, feature.ToString());
    }

    [Fact]
    public void FeatureGeneric_Equals_ReturnsTrueForSameValues()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);
        var feature2 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);

        Assert.True(feature1.Equals(feature2));
        Assert.True(feature1 == feature2);
        Assert.False(feature1 != feature2);
    }

    [Fact]
    public void FeatureGeneric_Equals_ReturnsFalseForDifferentKey()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);
        var feature2 = new Feature<string>("different-key", _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);

        Assert.False(feature1.Equals(feature2));
        Assert.False(feature1 == feature2);
        Assert.True(feature1 != feature2);
    }

    [Fact]
    public void FeatureGeneric_Equals_ReturnsFalseForDifferentEnabledValue()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);
        var feature2 = new Feature<string>(_key, !_enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);

        Assert.False(feature1.Equals(feature2));
        Assert.False(feature1 == feature2);
        Assert.True(feature1 != feature2);
    }

    [Fact]
    public void FeatureGeneric_Equals_ReturnsFalseForDifferentConfig()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);
        var feature2 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, ("different-key", "different-value"));

        Assert.False(feature1.Equals(feature2));
        Assert.False(feature1 == feature2);
        Assert.True(feature1 != feature2);
    }

    [Fact]
    public void FeatureGeneric_Equals_ReturnsFalseForNull()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);

        Assert.False(feature1.Equals(null));
    }

    [Fact]
    public void FeatureGeneric_GetHashCode_ReturnsSameValueForEqualObjects()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);
        var feature2 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);

        Assert.Equal(feature1.GetHashCode(), feature2.GetHashCode());
    }

    [Fact]
    public void FeatureGeneric_GetHashCode_ReturnsDifferentValueForDifferentKey()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);
        var feature2 = new Feature<string>("different-key", _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);

        Assert.NotEqual(feature1.GetHashCode(), feature2.GetHashCode());
    }

    [Fact]
    public void FeatureGeneric_GetHashCode_ReturnsDifferentValueForDifferentConfig()
    {
        var feature1 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, _config);
        var feature2 = new Feature<string>(_key, _enabled, CheckEnabledStateCallback, CheckConfigCallback,
            TrackCallback, ("different-key", "different-value"));

        Assert.NotEqual(feature1.GetHashCode(), feature2.GetHashCode());
    }
}
