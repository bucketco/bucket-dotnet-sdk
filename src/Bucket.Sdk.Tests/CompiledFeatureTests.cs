namespace Bucket.Sdk.Tests;

public sealed class CompiledFeatureTests
{
    private static readonly IReadOnlyDictionary<string, object?> _empty = new Dictionary<string, object?>();

    private const string _featureKey = "test-feature";

    [Fact]
    public void Evaluate_WithConfig_ReturnsProperDataWithConfig()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules =
                [
                    new FeatureTargetingRule { Filter = new ConstantFilter { Value = true } },
                ],
            },
            Config = new FeatureConfigDefinition
            {
                Version = 3,
                Variants =
                [
                    new FeatureConfigVariantDefinition
                    {
                        Key = "variant-1",
                        Payload = new { value = "test" }.AsJsonElement(),
                        Filter = new ConstantFilter { Value = true },
                    },
                ],
            },
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.Evaluate(_empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_featureKey, result.Key);
        Assert.True(result.Enabled);
        Assert.False(result.Override);

        // Check config
        _ = Assert.NotNull(result.Config);
        Assert.Equal("variant-1", result.Config.Value.Key);
        _ = Assert.NotNull(result.Config?.Payload);

        // Check debug data
        Assert.NotNull(result.FlagEvaluationDebugData);
        Assert.Equal(2, result.FlagEvaluationDebugData!.Version);
        _ = Assert.Single(result.FlagEvaluationDebugData.EvaluatedRules);
        Assert.True(result.FlagEvaluationDebugData.EvaluatedRules[0]);
        Assert.Empty(result.FlagEvaluationDebugData.EvaluationIssues);

        // Check config debug data
        Assert.NotNull(result.ConfigEvaluationDebugData);
        Assert.Equal(3, result.ConfigEvaluationDebugData!.Version);
        _ = Assert.Single(result.ConfigEvaluationDebugData.EvaluatedRules);
        Assert.True(result.ConfigEvaluationDebugData.EvaluatedRules[0]);
        Assert.Empty(result.ConfigEvaluationDebugData.EvaluationIssues);

        // Check context
        Assert.NotNull(result.EvaluationContext);
        Assert.Equal(_empty, result.EvaluationContext);
    }

    [Fact]
    public void Evaluate_WithConfigButNoMatchingVariant_ReturnsNoConfig()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules =
                [
                    new FeatureTargetingRule { Filter = new ConstantFilter { Value = true } },
                ],
            },
            Config = new FeatureConfigDefinition
            {
                Version = 3,
                Variants =
                [
                    new FeatureConfigVariantDefinition
                    {
                        Key = "variant-1",
                        Payload = new { value = "test" }.AsJsonElement(),
                        Filter = new ConstantFilter { Value = false }, // No match
                    },
                ],
            },
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.Evaluate(_empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_featureKey, result.Key);
        Assert.True(result.Enabled);

        // Check config
        Assert.False(result.Config.HasValue);

        // Check debug data
        Assert.NotNull(result.FlagEvaluationDebugData);
        Assert.Equal(2, result.FlagEvaluationDebugData!.Version);
        _ = Assert.Single(result.FlagEvaluationDebugData.EvaluatedRules);
        Assert.True(result.FlagEvaluationDebugData.EvaluatedRules[0]);

        // Check config debug data
        Assert.NotNull(result.ConfigEvaluationDebugData);
        Assert.Equal(3, result.ConfigEvaluationDebugData!.Version);
        _ = Assert.Single(result.ConfigEvaluationDebugData.EvaluatedRules);
        Assert.False(result.ConfigEvaluationDebugData.EvaluatedRules[0]);
    }

    [Fact]
    public void Evaluate_WithEmptyConfig_ReturnsNoConfig()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules =
                [
                    new FeatureTargetingRule { Filter = new ConstantFilter { Value = true } },
                ],
            },
            Config = new FeatureConfigDefinition
            {
                Version = 3,
                Variants = [], // Empty variants
            },
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.Evaluate(_empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_featureKey, result.Key);
        Assert.True(result.Enabled);

        // Check config
        Assert.False(result.Config.HasValue);

        // Check debug data
        Assert.NotNull(result.FlagEvaluationDebugData);
        Assert.Equal(2, result.FlagEvaluationDebugData!.Version);

        // Check config debug data
        Assert.NotNull(result.ConfigEvaluationDebugData);
        Assert.Equal(3, result.ConfigEvaluationDebugData!.Version);
        Assert.Empty(result.ConfigEvaluationDebugData.EvaluatedRules);
    }

    [Fact]
    public void Evaluate_WithNoConfig_ReturnsOnlyFlagDebugData()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules =
                [
                    new FeatureTargetingRule { Filter = new ConstantFilter { Value = true } },
                ],
            },
            Config = null, // No config
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.Evaluate(_empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_featureKey, result.Key);
        Assert.True(result.Enabled);

        // Check config
        Assert.False(result.Config.HasValue);

        // Check debug data
        Assert.NotNull(result.FlagEvaluationDebugData);
        Assert.Equal(2, result.FlagEvaluationDebugData!.Version);
        _ = Assert.Single(result.FlagEvaluationDebugData.EvaluatedRules);
        Assert.True(result.FlagEvaluationDebugData.EvaluatedRules[0]);

        // Check no config debug data
        Assert.Null(result.ConfigEvaluationDebugData);
    }

    [Fact]
    public void Evaluate_WithFlagRule_False_ReturnsDisabledFeature()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules =
                [
                    new FeatureTargetingRule
                    {
                        Filter = new ConstantFilter { Value = false }, // Feature is disabled
                    },
                ],
            },
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.Evaluate(_empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_featureKey, result.Key);
        Assert.False(result.Enabled);
        Assert.False(result.Config.HasValue);
    }

    [Fact]
    public void Evaluate_WithMultipleRules_FirstMatchingRuleWins()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules =
                [
                    new FeatureTargetingRule
                    {
                        Filter = new ConstantFilter { Value = false }, // First rule is false
                    },
                    new FeatureTargetingRule
                    {
                        Filter = new ConstantFilter { Value = true }, // Second rule is true
                    },
                ],
            },
            Config = new FeatureConfigDefinition
            {
                Version = 3,
                Variants =
                [
                    new FeatureConfigVariantDefinition
                    {
                        Key = "variant-1",
                        Payload = new { value = "test1" }.AsJsonElement(),
                        Filter = new ConstantFilter { Value = false }, // First variant no match
                    },
                    new FeatureConfigVariantDefinition
                    {
                        Key = "variant-2",
                        Payload = new { value = "test2" }.AsJsonElement(),
                        Filter = new ConstantFilter { Value = true }, // Second variant matches
                    },
                ],
            },
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.Evaluate(_empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_featureKey, result.Key);
        Assert.True(result.Enabled); // At least one rule matched, so enabled

        // Check config - second variant should be selected
        Assert.True(result.Config.HasValue);
        Assert.Equal("variant-2", result.Config.Value.Key);

        // Check debug data
        Assert.NotNull(result.FlagEvaluationDebugData);
        Assert.Equal(2, result.FlagEvaluationDebugData!.Version);
        Assert.Equal(2, result.FlagEvaluationDebugData.EvaluatedRules.Count);
        Assert.False(result.FlagEvaluationDebugData.EvaluatedRules[0]);
        Assert.True(result.FlagEvaluationDebugData.EvaluatedRules[1]);

        // Check config debug data
        Assert.NotNull(result.ConfigEvaluationDebugData);
        Assert.Equal(3, result.ConfigEvaluationDebugData!.Version);
        Assert.Equal(2, result.ConfigEvaluationDebugData.EvaluatedRules.Count);
        Assert.False(result.ConfigEvaluationDebugData.EvaluatedRules[0]);
        Assert.True(result.ConfigEvaluationDebugData.EvaluatedRules[1]);
    }

    [Fact]
    public void Evaluate_WithEvaluationIssues_RecordsIssuesCorrectly()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules =
                [
                    new FeatureTargetingRule
                    {
                        Filter = new ContextFilter
                        {
                            Field = "missing_field", // This field will be missing
                            Operator = ContextOperatorType.Set,
                            Values = [],
                        },
                    },
                ],
            },
            Config = new FeatureConfigDefinition
            {
                Version = 3,
                Variants =
                [
                    new FeatureConfigVariantDefinition
                    {
                        Key = "variant-1",
                        Payload = new { value = "test" }.AsJsonElement(),
                        Filter = new ContextFilter
                        {
                            Field = "another_missing_field", // This field will be missing
                            Operator = ContextOperatorType.Set,
                            Values = [],
                        },
                    },
                ],
            },
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.Evaluate(_empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_featureKey, result.Key);
        Assert.False(result.Enabled);         // No rules matched
        Assert.False(result.Config.HasValue); // No config matched

        // Check flag evaluation issues
        Assert.NotNull(result.FlagEvaluationDebugData);
        _ = Assert.Single(result.FlagEvaluationDebugData.EvaluationIssues);
        Assert.Equal(EvaluationIssueType.MissingField, result.FlagEvaluationDebugData.EvaluationIssues[0].type);
        Assert.Equal("missing_field", result.FlagEvaluationDebugData.EvaluationIssues[0].name);

        // Check config evaluation issues
        Assert.NotNull(result.ConfigEvaluationDebugData);
        _ = Assert.Single(result.ConfigEvaluationDebugData.EvaluationIssues);
        Assert.Equal(EvaluationIssueType.MissingField, result.ConfigEvaluationDebugData.EvaluationIssues[0].type);
        Assert.Equal("another_missing_field", result.ConfigEvaluationDebugData.EvaluationIssues[0].name);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 1,
                Rules = []
            }
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act
        var result = compiledFeature.ToString();

        // Assert
        Assert.Contains("FeatureDefinition =", result);
        Assert.Contains(_featureKey, result);
    }

    [Fact]
    public void Equals_WithSameFeatureDefinition_ReturnsTrue()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 1,
                Rules = []
            }
        };

        var compiledFeature1 = new CompiledFeature(featureDefinition);
        var compiledFeature2 = new CompiledFeature(featureDefinition);

        // Act & Assert
        Assert.Equal(compiledFeature1, compiledFeature2);
        Assert.True(compiledFeature1 == compiledFeature2);
        Assert.False(compiledFeature1 != compiledFeature2);
    }

    [Fact]
    public void Equals_WithDifferentFeatureDefinitions_ReturnsFalse()
    {
        // Arrange
        var featureDefinition1 = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 1,
                Rules = []
            }
        };

        var featureDefinition2 = new FeatureDefinition
        {
            Key = _featureKey + "-different",
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules = []
            }
        };

        var compiledFeature1 = new CompiledFeature(featureDefinition1);
        var compiledFeature2 = new CompiledFeature(featureDefinition2);

        // Act & Assert
        Assert.NotEqual(compiledFeature1, compiledFeature2);
        Assert.False(compiledFeature1 == compiledFeature2);
        Assert.True(compiledFeature1 != compiledFeature2);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 1,
                Rules = []
            }
        };

        var compiledFeature = new CompiledFeature(featureDefinition);

        // Act & Assert
        Assert.False(compiledFeature.Equals(null));
        Assert.False(compiledFeature == null);
        Assert.False(null == compiledFeature);
        Assert.True(compiledFeature != null);
        Assert.True(null != compiledFeature);
    }

    [Fact]
    public void GetHashCode_WithSameFeatureDefinition_ReturnsSameValue()
    {
        // Arrange
        var featureDefinition = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 1,
                Rules = []
            }
        };

        var compiledFeature1 = new CompiledFeature(featureDefinition);
        var compiledFeature2 = new CompiledFeature(featureDefinition);

        // Act
        var hashCode1 = compiledFeature1.GetHashCode();
        var hashCode2 = compiledFeature2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentFeatureDefinitions_ReturnsDifferentValues()
    {
        // Arrange
        var featureDefinition1 = new FeatureDefinition
        {
            Key = _featureKey,
            Targeting = new FeatureTargetingDefinition
            {
                Version = 1,
                Rules = []
            }
        };

        var featureDefinition2 = new FeatureDefinition
        {
            Key = _featureKey + "-different",
            Targeting = new FeatureTargetingDefinition
            {
                Version = 2,
                Rules = []
            }
        };

        var compiledFeature1 = new CompiledFeature(featureDefinition1);
        var compiledFeature2 = new CompiledFeature(featureDefinition2);

        // Act
        var hashCode1 = compiledFeature1.GetHashCode();
        var hashCode2 = compiledFeature2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
