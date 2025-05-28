namespace Bucket.Sdk.Tests.OpenFeature;

using System;
using System.Collections.Generic;

using global::OpenFeature.Model;

public sealed class DefaultEvaluationContextTranslatorTests
{
    [Fact]
    public void Translate_WithNullContext_ReturnsEmptyContext()
    {
        // Act
        var result = DefaultEvaluationContextTranslator.Translate(null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.ToFields());
    }

    [Fact]
    public void Translate_ConvertsStandardFieldsToUserProperties()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Builder()
            .SetTargetingKey("user-123")
            .Set("name", "John Doe")
            .Set("email", "john@example.com")
            .Set("avatar", "https://example.com/avatar.jpg")
            .Set("companyId", "company-456")
            .Set("companyName", "Acme Inc")
            .Set("companyAvatar", "https://example.com/company.jpg")
            .Build();

        // Act
        var result = DefaultEvaluationContextTranslator.Translate(evaluationContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            new Dictionary<string, object?>
            {
                { "user.id", "user-123" },
                { "user.name", "John Doe" },
                { "user.email", "john@example.com" },
                { "user.avatar", "https://example.com/avatar.jpg" },
                { "company.id", "company-456" },
                { "company.name", "Acme Inc" },
                { "company.avatar", "https://example.com/company.jpg" },
            },
            result.ToFields()
        );
    }

    [Fact]
    public void Translate_FallsBackToUserId_WhenTargetingKeyIsMissing()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Builder()
            .Set("userId", "user-123")
            .Build();

        // Act
        var result = DefaultEvaluationContextTranslator.Translate(evaluationContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            new Dictionary<string, object?> { { "user.id", "user-123" }, },
            result.ToFields()
        );
    }

    [Fact]
    public void Translate_WithInvalidAvatarUris_HandlesGracefully()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Builder()
            .Set("avatar", "not-a-valid-uri")
            .Set("companyAvatar", "also-not-valid")
            .Build();

        // Act
        var result = DefaultEvaluationContextTranslator.Translate(evaluationContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.ToFields());
    }

    [Fact]
    public void Translate_IgnoresStandardFields_ThatAreNotStrings()
    {
        // Arrange
        var evaluationContext = EvaluationContext.Builder()
            .Set("userId", 1)
            .Set("name", 2)
            .Set("email", 3)
            .Set("avatar", 4)
            .Set("companyId", 5)
            .Set("companyName", 6)
            .Set("companyAvatar", 7)
            .Build();

        // Act
        var result = DefaultEvaluationContextTranslator.Translate(evaluationContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.ToFields());
    }

    [Fact]
    public void Translate_IncludesCustomFields()
    {
        // Arrange
        var innerStructure = new Structure(
            new Dictionary<string, Value> { { "value", new Value(false) } }
        );
        var outerStructure = new Structure(
            new Dictionary<string, Value> { { "key", new Value("value") }, { "inner", new Value(innerStructure) } });

        var evaluationContext = EvaluationContext.Builder()
            .Set("custom_int", 1)
            .Set("custom_string", "test")
            .Set("custom_bool", true)
            .Set("custom_double", 1.23)
            .Set("custom_datetime", new DateTime(2021, 1, 1, 12, 30, 0))
            .Set("custom_object", new Value(outerStructure))
            .Build();

        // Act
        var result = DefaultEvaluationContextTranslator.Translate(evaluationContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            new Dictionary<string, object?>
            {
                { "other.custom_int", 1.0 },
                { "other.custom_string", "test" },
                { "other.custom_bool", true },
                { "other.custom_double", 1.23 },
                { "other.custom_datetime", new DateTime(2021, 1, 1, 12, 30, 0) },
                { "other.custom_object.key", "value" },
                { "other.custom_object.inner.value", false },
            },
            result.ToFields()
        );
    }
}
