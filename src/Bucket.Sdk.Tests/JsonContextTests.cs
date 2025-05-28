namespace Bucket.Sdk.Tests;

/// <summary>
/// Tests for JsonContext.List and JsonContext.Attributes
/// </summary>
public sealed class JsonContextTests
{
    [Fact]
    public void Deserialize_ErrorResponse_WithMessage()
    {
        JsonAssert.EquivalentToFixture("response.error.full",
            new ResponseBase
            {
                Success = false,
                Error = new ErrorDetails { Message = "Invalid API key", Code = "INVALID_API_KEY" },
            });
    }

    [Fact]
    public void Deserialize_ErrorResponse_WithoutMessage()
    {
        JsonAssert.EquivalentToFixture("response.error.no-message",
            new ResponseBase { Success = false, Error = new ErrorDetails { Code = "INVALID_API_KEY" } });
    }

    [Fact]
    public void Deserialize_SuccessResponse() => JsonAssert.EquivalentToFixture(
        "response.success", new ResponseBase { Success = true });

    [Fact]
    public void Deserialize_FeaturesEvaluated()
    {
        JsonAssert.EquivalentToFixture("response.features-evaluated.full", new FeaturesEvaluateResponse
        {
            Success = true,
            UsedRemoteContext = true,
            Features =
            [
                new FeatureEvaluated
                {
                    Key = "feature-1",
                    Enabled = false,
                    TargetingVersion = 19,
                    Config = new FeatureConfigEvaluated
                    {
                        Key = "gpt-4.5-mini",
                        Version = 2,
                        Payload = new { maxTokens = 2000 }.AsJsonElement(),
                        MissingFields = ["company.tier"],
                        EvaluatedRules = [true],
                    },
                    MissingFields = ["user.name", "other.level"],
                    EvaluatedRules = [true, false, true],
                },
                new FeatureEvaluated { Key = "feature-2", Enabled = true, TargetingVersion = 11 },
                new FeatureEvaluated
                {
                    Key = "feature-3",
                    Enabled = true,
                    TargetingVersion = 1,
                    Config = new FeatureConfigEvaluated
                    {
                        Key = "variant-b", Version = 2, MissingFields = [], EvaluatedRules = [],
                    },
                    MissingFields = [],
                    EvaluatedRules = [],
                },
            ],
        });
    }

    [Fact]
    public void Deserialize_FeaturesDefinitions()
    {
        JsonAssert.EquivalentToFixture("response.features-definitions.full", new FeaturesDefinitionsResponse
        {
            Success = true,
            Features =
            [
                new FeatureDefinition
                {
                    Key = "feature-1",
                    Targeting = new FeatureTargetingDefinition
                    {
                        Version = 2,
                        Rules =
                        [
                            new FeatureTargetingRule
                            {
                                Filter = new GroupFilter
                                {
                                    Operator = GroupFilterOperatorType.And,
                                    Filters =
                                    [
                                        new NegationFilter { Filter = new ConstantFilter { Value = false } },
                                        new ContextFilter
                                        {
                                            Operator = ContextOperatorType.StringAnyOf,
                                            Field = "company.name",
                                            Values = ["acme", "bucket"],
                                        },
                                        new PartialRolloutFilter
                                        {
                                            Key = "feature-1",
                                            PartialRolloutAttribute = "company.id",
                                            PartialRolloutThreshold = 99999,
                                        },
                                    ],
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
                                Payload = new { some = "value" }.AsJsonElement(),
                                Filter = new ContextFilter
                                {
                                    Operator = ContextOperatorType.StringAnyOf,
                                    Field = "user.name",
                                    Values = ["alex", "ron"],
                                },
                            },
                        ],
                    },
                },
                new FeatureDefinition
                {
                    Key = "feature-2", Targeting = new FeatureTargetingDefinition { Version = 3, Rules = [] },
                },
            ],
        });
    }

    [Fact]
    public void Serialize_CompanyMessage_Full()
    {
        JsonAssert.EquivalentToFixture("message.company.full", new CompanyMessage
        {
            CompanyId = "company-123",
            UserId = "user-abc",
            Attributes = new Dictionary<string, object?>
            {
                ["name"] = "Acme Inc.",
                ["industry"] = "Technology",
                ["size"] = 250,
                ["isPremium"] = true,
                ["foundedYear"] = 2010,
                ["nullValue"] = null,
            },
            Metadata = new TrackingMetadata { Active = true },
        });
    }

    [Fact]
    public void Serialize_CompanyMessage_Minimal() =>
        JsonAssert.EquivalentToFixture("message.company.minimal", new CompanyMessage { CompanyId = "company-xyz" });

    [Fact]
    public void Serialize_UserMessage_Full()
    {
        JsonAssert.EquivalentToFixture("message.user.full", new UserMessage
        {
            UserId = "user-456",
            Attributes = new Dictionary<string, object?>
            {
                ["email"] = "john.doe@example.com",
                ["age"] = 32,
                ["isPremium"] = false,
                ["lastLogin"] = "2023-04-15T10:30:00Z",
                ["preferences.theme"] = "dark",
                ["preferences.notifications"] = true,
            },
            Metadata = new TrackingMetadata { Active = true },
        });
    }

    [Fact]
    public void Serialize_UserMessage_Minimal() =>
        JsonAssert.EquivalentToFixture("message.user.minimal", new UserMessage { UserId = "user-789" });

    [Fact]
    public void Serialize_TrackEventMessage_Full()
    {
        JsonAssert.EquivalentToFixture("message.event.full", new TrackEventMessage
        {
            Name = "purchase_completed",
            UserId = "user-123",
            CompanyId = "company-456",
            Attributes = new Dictionary<string, object?>
            {
                ["eventName"] = "purchase_completed",
                ["amount"] = 99.99,
                ["currency"] = "USD",
                ["items.0.id"] = "product-001",
                ["items.0.name"] = "Premium Subscription",
                ["items.0.quantity"] = 1,
                ["timestamp"] = "2023-05-20T15:45:30Z",
            },
            Metadata = new TrackingMetadata { Active = true },
        });
    }

    [Fact]
    public void Serialize_EventMessage_Minimal() =>
        JsonAssert.EquivalentToFixture("message.event.minimal",
            new TrackEventMessage { Name = "purchase_completed", UserId = "user-abc" });

    [Fact]
    public void Serialize_FeatureEventMessage_CheckFlag()
    {
        JsonAssert.EquivalentToFixture("message.feature.check-flag", new FeatureEventMessage
        {
            FeatureKey = "premium-dashboard",
            SubType = FeatureEventType.CheckFlag,
            EvaluationResult = true.AsJsonElement(),
            TargetingVersion = 5,
            Context = new Dictionary<string, object?> { ["user.id"] = "user-123", ["company.plan"] = "enterprise" },
            EvaluatedRules = [true, false, true],
        });
    }

    [Fact]
    public void Serialize_FeatureEventMessage_CheckConfig()
    {
        JsonAssert.EquivalentToFixture("message.feature.check-config", new FeatureEventMessage
        {
            FeatureKey = "api-rate-limits",
            SubType = FeatureEventType.CheckConfig,
            EvaluationResult = new { limit = 1000, period = "hour", throttling = true }.AsJsonElement(),
            TargetingVersion = 3,
            Context = new Dictionary<string, object?> { ["company.tier"] = "premium", ["user.role"] = "admin" },
            MissingFields = ["user.usage"],
        });
    }

    [Fact]
    public void Serialize_FeatureEventMessage_EvaluateFlag()
    {
        JsonAssert.EquivalentToFixture("message.feature.evaluate-flag", new FeatureEventMessage
        {
            FeatureKey = "beta-feature",
            SubType = FeatureEventType.EvaluateFlag,
            EvaluationResult = false.AsJsonElement(),
            TargetingVersion = 2,
            Context = new Dictionary<string, object?>
            {
                ["user.email"] = "test@example.com",
                ["company.id"] = "company-xyz",
            },
        });
    }

    [Fact]
    public void Serialize_FeatureEventMessage_EvaluateConfig()
    {
        JsonAssert.EquivalentToFixture("message.feature.evaluate-config", new FeatureEventMessage
        {
            FeatureKey = "ui-settings",
            SubType = FeatureEventType.EvaluateConfig,
            EvaluationResult = new { theme = "dark", showBanner = true, maxItems = 50 }.AsJsonElement(),
            TargetingVersion = 7,
            Context = new Dictionary<string, object?> { ["user.preferences.darkMode"] = true },
            EvaluatedRules = [true, true],
            MissingFields = [],
        });
    }

    // Tests for JsonContext.List<T>
    [Fact]
    public void List_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var source = new[] { "one", "two", "three" };

        // Act
        var list = new JsonContext.List<string>(source);

        // Assert
        Assert.Equal(3, list.Count);
        Assert.Equal("one", list[0]);
        Assert.Equal("two", list[1]);
        Assert.Equal("three", list[2]);
    }

    [Fact]
    public void List_Enumeration_WorksCorrectly()
    {
        // Arrange
        var source = new[] { "one", "two", "three" };
        var list = new JsonContext.List<string>(source);

        // Act & Assert
        var enumeratedValues = list.ToList();
        Assert.Equal(3, enumeratedValues.Count);
        Assert.Equal("one", enumeratedValues[0]);
        Assert.Equal("two", enumeratedValues[1]);
        Assert.Equal("three", enumeratedValues[2]);
    }

    [Fact]
    public void List_ToString_ReturnsCorrectFormattedString()
    {
        // Arrange
        var source = new[] { "one", "two", "three" };
        var list = new JsonContext.List<string>(source);

        // Act
        var result = list.ToString();

        // Assert
        Assert.Equal("List { one, two, three }", result);
    }

    [Fact]
    public void List_ToString_WithEmptyList_ReturnsCorrectFormat()
    {
        // Arrange
        var source = Array.Empty<string>();
        var list = new JsonContext.List<string>(source);

        // Act
        var result = list.ToString();

        // Assert
        Assert.Equal("List { }", result);
    }

    [Fact]
    public void List_Equals_ReturnsTrueForListsWithSameContents()
    {
        // Arrange
        var source1 = new[] { "one", "two", "three" };
        var source2 = new[] { "one", "two", "three" };
        var list1 = new JsonContext.List<string>(source1);
        var list2 = new JsonContext.List<string>(source2);

        // Act & Assert
        Assert.Equal(list1, list2);
    }

    [Fact]
    public void List_Equals_ReturnsFalseForListsWithDifferentContents()
    {
        // Arrange
        var source1 = new[] { "one", "two", "three" };
        var source2 = new[] { "one", "two", "four" };
        var list1 = new JsonContext.List<string>(source1);
        var list2 = new JsonContext.List<string>(source2);

        // Act & Assert
        Assert.NotEqual(list1, list2);
    }

    [Fact]
    public void List_Equals_ReturnsFalseForListsWithDifferentLengths()
    {
        // Arrange
        var source1 = new[] { "one", "two", "three" };
        var source2 = new[] { "one", "two" };
        var list1 = new JsonContext.List<string>(source1);
        var list2 = new JsonContext.List<string>(source2);

        // Act & Assert
        Assert.NotEqual(list1, list2);
    }

    [Fact]
    public void List_GetHashCode_ReturnsSameValueForEqualLists()
    {
        // Arrange
        var source1 = new[] { "one", "two", "three" };
        var source2 = new[] { "one", "two", "three" };
        var list1 = new JsonContext.List<string>(source1);
        var list2 = new JsonContext.List<string>(source2);

        // Act & Assert
        Assert.Equal(list1.GetHashCode(), list2.GetHashCode());
    }

    [Fact]
    public void List_GetHashCode_ReturnsDifferentValueForDifferentLists()
    {
        // Arrange
        var source1 = new[] { "one", "two", "three" };
        var source2 = new[] { "one", "two", "four" };
        var list1 = new JsonContext.List<string>(source1);
        var list2 = new JsonContext.List<string>(source2);

        // Act & Assert
        Assert.NotEqual(list1.GetHashCode(), list2.GetHashCode());
    }

    // Tests for JsonContext.Attributes
    [Fact]
    public void Attributes_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };

        // Act
        var attributes = new JsonContext.Attributes(source);

        // Assert
        Assert.Equal(3, attributes.Count);
        Assert.Equal("value1", attributes["key1"]);
        Assert.Equal(42, attributes["key2"]);
        Assert.Equal(true, attributes["key3"]);
    }

    [Fact]
    public void Attributes_Enumeration_WorksCorrectly()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var attributes = new JsonContext.Attributes(source);

        // Act & Assert
        var items = attributes.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, kv => kv.Key == "key1" && (string) kv.Value! == "value1");
        Assert.Contains(items, kv => kv.Key == "key2" && (int) kv.Value! == 42);
        Assert.Contains(items, kv => kv.Key == "key3" && (bool) kv.Value!);
    }

    [Fact]
    public void Attributes_ToString_ReturnsCorrectFormattedString()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var attributes = new JsonContext.Attributes(source);

        // Act
        var result = attributes.ToString();

        // Assert
        // The output should be ordered by key
        Assert.Equal("Attributes { \"key1\" = value1, \"key2\" = 42, \"key3\" = True }", result);
    }

    [Fact]
    public void Attributes_ToString_WithEmptyDictionary_ReturnsCorrectFormat()
    {
        // Arrange
        var attributes = new JsonContext.Attributes(
            new Dictionary<string, object?>()
        );

        // Act
        var result = attributes.ToString();

        // Assert
        Assert.Equal("Attributes { }", result);
    }

    [Fact]
    public void Attributes_Equals_ReturnsTrueForDictionariesWithSameContents()
    {
        // Arrange
        var source1 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var source2 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var attributes1 = new JsonContext.Attributes(source1);
        var attributes2 = new JsonContext.Attributes(source2);

        // Act & Assert
        Assert.Equal(attributes1, attributes2);
    }

    [Fact]
    public void Attributes_Equals_ReturnsFalseForDictionariesWithDifferentContents()
    {
        // Arrange
        var source1 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var source2 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = false
        };
        var attributes1 = new JsonContext.Attributes(source1);
        var attributes2 = new JsonContext.Attributes(source2);

        // Act & Assert
        Assert.NotEqual(attributes1, attributes2);
    }

    [Fact]
    public void Attributes_Equals_ReturnsFalseForDictionariesWithDifferentKeys()
    {
        // Arrange
        var source1 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var source2 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key4"] = true
        };
        var attributes1 = new JsonContext.Attributes(source1);
        var attributes2 = new JsonContext.Attributes(source2);

        // Act & Assert
        Assert.NotEqual(attributes1, attributes2);
    }

    [Fact]
    public void Attributes_GetHashCode_ReturnsSameValueForEqualDictionaries()
    {
        // Arrange
        var source1 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var source2 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var attributes1 = new JsonContext.Attributes(source1);
        var attributes2 = new JsonContext.Attributes(source2);

        // Act & Assert
        Assert.Equal(attributes1.GetHashCode(), attributes2.GetHashCode());
    }

    [Fact]
    public void Attributes_GetHashCode_ReturnsDifferentValueForDifferentDictionaries()
    {
        // Arrange
        var source1 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var source2 = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = false
        };
        var attributes1 = new JsonContext.Attributes(source1);
        var attributes2 = new JsonContext.Attributes(source2);

        // Act & Assert
        Assert.NotEqual(attributes1.GetHashCode(), attributes2.GetHashCode());
    }

    [Fact]
    public void Attributes_TryGetValue_ReturnsTrueAndValueForExistingKey()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        var attributes = new JsonContext.Attributes(source);

        // Act
        var success = attributes.TryGetValue("key1", out var value);

        // Assert
        Assert.True(success);
        Assert.Equal("value1", value);
    }

    [Fact]
    public void Attributes_TryGetValue_ReturnsFalseForNonExistingKey()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        var attributes = new JsonContext.Attributes(source);

        // Act
        var success = attributes.TryGetValue("key3", out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void Attributes_ContainsKey_ReturnsTrueForExistingKey()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        var attributes = new JsonContext.Attributes(source);

        // Act & Assert
        Assert.True(attributes.ContainsKey("key1"));
    }

    [Fact]
    public void Attributes_ContainsKey_ReturnsFalseForNonExistingKey()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        var attributes = new JsonContext.Attributes(source);

        // Act & Assert
        Assert.False(attributes.ContainsKey("key3"));
    }

    [Fact]
    public void Attributes_Keys_ReturnsCorrectKeys()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var attributes = new JsonContext.Attributes(source);

        // Act
        var keys = attributes.Keys.ToList();

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public void Attributes_Values_ReturnsCorrectValues()
    {
        // Arrange
        var source = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var attributes = new JsonContext.Attributes(source);

        // Act
        var values = attributes.Values.ToList();

        // Assert
        Assert.Equal(3, values.Count);
        Assert.Contains("value1", values);
        Assert.Contains(42, values);
        Assert.Contains(true, values);
    }
}
