namespace Bucket.Sdk.Tests;

public class CompiledFilterTests
{
    private static readonly IReadOnlyDictionary<string, object?> _empty = new Dictionary<string, object?>();

    #region Missing Field Tests

    [Fact]
    public void ContextFilter_MissingField_ReturnsFalse()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.Is, Field = "country", Values = ["US"] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.False(result);
        _ = Assert.Single(issues);
        Assert.Equal(EvaluationIssueType.MissingField, issues[0].issue);
        Assert.Equal("country", issues[0].name);
    }

    #endregion

    private sealed record MockUnsupportedFilter: Filter;

    #region GroupFilter Tests

    [Fact]
    public void GroupFilter_And_AllTrue_ReturnsTrue()
    {
        // Arrange
        var filter = new GroupFilter
        {
            Operator = GroupFilterOperatorType.And,
            Filters =
            [
                new ConstantFilter { Value = true },
                new ConstantFilter { Value = true },
            ],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.True(result);
        Assert.Empty(issues);
    }

    [Fact]
    public void GroupFilter_And_OneFalse_ReturnsFalse()
    {
        // Arrange
        var filter = new GroupFilter
        {
            Operator = GroupFilterOperatorType.And,
            Filters =
            [
                new ConstantFilter { Value = true },
                new ConstantFilter { Value = false },
            ],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.False(result);
        Assert.Empty(issues);
    }

    [Fact]
    public void GroupFilter_Or_OneTrue_ReturnsTrue()
    {
        // Arrange
        var filter = new GroupFilter
        {
            Operator = GroupFilterOperatorType.Or,
            Filters =
            [
                new ConstantFilter { Value = false },
                new ConstantFilter { Value = true },
            ],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.True(result);
        Assert.Empty(issues);
    }

    [Fact]
    public void GroupFilter_Or_AllFalse_ReturnsFalse()
    {
        // Arrange
        var filter = new GroupFilter
        {
            Operator = GroupFilterOperatorType.Or,
            Filters =
            [
                new ConstantFilter { Value = false },
                new ConstantFilter { Value = false },
            ],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.False(result);
        Assert.Empty(issues);
    }

    [Fact]
    public void GroupFilter_Nested_WorksCorrectly()
    {
        // Arrange
        var filter = new GroupFilter
        {
            Operator = GroupFilterOperatorType.And,
            Filters =
            [
                new GroupFilter
                {
                    Operator = GroupFilterOperatorType.Or,
                    Filters =
                    [
                        new ConstantFilter { Value = true },
                        new ConstantFilter { Value = false },
                    ],
                },
                new ConstantFilter { Value = true },
            ],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.True(result);
        Assert.Empty(issues);
    }

    #endregion

    #region NegationFilter Tests

    [Fact]
    public void NegationFilter_TrueFilter_ReturnsFalse()
    {
        // Arrange
        var filter = new NegationFilter { Filter = new ConstantFilter { Value = true } };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.False(result);
        Assert.Empty(issues);
    }

    [Fact]
    public void NegationFilter_FalseFilter_ReturnsTrue()
    {
        // Arrange
        var filter = new NegationFilter { Filter = new ConstantFilter { Value = false } };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.True(result);
        Assert.Empty(issues);
    }

    #endregion

    #region ConstantFilter Tests

    [Fact]
    public void ConstantFilter_True_ReturnsTrue()
    {
        // Arrange
        var filter = new ConstantFilter { Value = true };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.True(result);
        Assert.Empty(issues);
    }

    [Fact]
    public void ConstantFilter_False_ReturnsFalse()
    {
        // Arrange
        var filter = new ConstantFilter { Value = false };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.False(result);
        Assert.Empty(issues);
    }

    #endregion

    #region PartialRolloutFilter Tests

    [Fact]
    public void PartialRolloutFilter_MissingField_ReturnsFalse()
    {
        // Arrange
        var filter = new PartialRolloutFilter
        {
            Key = "test-key",
            PartialRolloutAttribute = "userId",
            PartialRolloutThreshold = 50000, // 50%
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.False(result);
        _ = Assert.Single(issues);
        Assert.Equal(EvaluationIssueType.MissingField, issues[0].issue);
        Assert.Equal("userId", issues[0].name);
    }

    [Fact]
    public void PartialRolloutFilter_WithField_ReturnsDeterministicResult()
    {
        // Arrange - Create multiple users to verify some pass and some don't
        var filter = new PartialRolloutFilter
        {
            Key = "test-key",
            PartialRolloutAttribute = "userId",
            PartialRolloutThreshold = 50000, // 50%
        };
        var compiled = new CompiledFilter(filter);

        var results = new Dictionary<string, bool>();

        // Test with 20 different user IDs to see if we get a mix of true/false
        for (var i = 1; i <= 20; i++)
        {
            var userId = $"user-{i}";
            var contextFields = new Dictionary<string, object?> { ["userId"] = userId };
            var issues = new List<(EvaluationIssueType issue, string name)>();

            // Act
            var result = compiled.Predicate(contextFields, issues);
            results[userId] = result;

            // Assert - No issues should be reported
            Assert.Empty(issues);
        }

        // We should have a mix of true and false results
        Assert.Contains(results.Values, v => v);
        Assert.Contains(results.Values, v => !v);

        // Same user should always get the same result (deterministic)
        var checkUserId = "consistent-user-check";
        var contextFields1 = new Dictionary<string, object?> { ["userId"] = checkUserId };
        var contextFields2 = new Dictionary<string, object?> { ["userId"] = checkUserId };
        var issues1 = new List<(EvaluationIssueType issue, string name)>();
        var issues2 = new List<(EvaluationIssueType issue, string name)>();

        var result1 = compiled.Predicate(contextFields1, issues1);
        var result2 = compiled.Predicate(contextFields2, issues2);

        // Results should be the same for the same user
        Assert.Equal(result1, result2);
    }

    #endregion

    #region ContextFilter Tests - String Operations

    [Fact]
    public void ContextFilter_StringContains_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.StringContains,
            Field = "email",
            Values = ["example.com"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act - Should match
        var contextFields1 = new Dictionary<string, object?> { ["email"] = "user@example.com" };
        var result1 = compiled.Predicate(contextFields1, issues);

        // Act - Should not match
        var contextFields2 = new Dictionary<string, object?> { ["email"] = "user@other.com" };
        var result2 = compiled.Predicate(contextFields2, issues);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_StringNotContains_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.StringNotContains,
            Field = "email",
            Values = ["example.com"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act - Should not match the "not contains" condition
        var contextFields1 = new Dictionary<string, object?> { ["email"] = "user@example.com" };
        var result1 = compiled.Predicate(contextFields1, issues);

        // Act - Should match the "not contains" condition
        var contextFields2 = new Dictionary<string, object?> { ["email"] = "user@other.com" };
        var result2 = compiled.Predicate(contextFields2, issues);

        // Assert
        Assert.False(result1);
        Assert.True(result2);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_Is_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.Is, Field = "country", Values = ["US"] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act - Should match
        var contextFields1 = new Dictionary<string, object?> { ["country"] = "US" };
        var result1 = compiled.Predicate(contextFields1, issues);

        // Act - Should not match
        var contextFields2 = new Dictionary<string, object?> { ["country"] = "UK" };
        var result2 = compiled.Predicate(contextFields2, issues);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_IsNot_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.IsNot, Field = "country", Values = ["US"] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act - Should not match the "is not" condition
        var contextFields1 = new Dictionary<string, object?> { ["country"] = "US" };
        var result1 = compiled.Predicate(contextFields1, issues);

        // Act - Should match the "is not" condition
        var contextFields2 = new Dictionary<string, object?> { ["country"] = "UK" };
        var result2 = compiled.Predicate(contextFields2, issues);

        // Assert
        Assert.False(result1);
        Assert.True(result2);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_StringAnyOf_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.StringAnyOf,
            Field = "country",
            Values = ["US", "UK", "CA"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act - Should match
        var contextFields1 = new Dictionary<string, object?> { ["country"] = "UK" };
        var result1 = compiled.Predicate(contextFields1, issues);

        // Act - Should not match
        var contextFields2 = new Dictionary<string, object?> { ["country"] = "FR" };
        var result2 = compiled.Predicate(contextFields2, issues);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_StringNotAnyOf_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.StringNotAnyOf,
            Field = "country",
            Values = ["US", "UK", "CA"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act - Should not match the "not any of" condition
        var contextFields1 = new Dictionary<string, object?> { ["country"] = "UK" };
        var result1 = compiled.Predicate(contextFields1, issues);

        // Act - Should match the "not any of" condition
        var contextFields2 = new Dictionary<string, object?> { ["country"] = "FR" };
        var result2 = compiled.Predicate(contextFields2, issues);

        // Assert
        Assert.False(result1);
        Assert.True(result2);
        Assert.Empty(issues);
    }

    #endregion

    #region ContextFilter Tests - Numeric Operations

    [Fact]
    public void ContextFilter_NumberGreaterThan_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.NumberGreaterThan,
            Field = "age",
            Values = ["21"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different numeric types
        var contextFields1 = new Dictionary<string, object?> { ["age"] = 25 };   // int
        var contextFields2 = new Dictionary<string, object?> { ["age"] = 18 };   // int
        var contextFields3 = new Dictionary<string, object?> { ["age"] = 25.5 }; // double
        var contextFields4 = new Dictionary<string, object?> { ["age"] = "25" }; // string

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);
        var result4 = compiled.Predicate(contextFields4, issues);

        // Assert
        Assert.True(result1);  // 25 > 21
        Assert.False(result2); // 18 !> 21
        Assert.True(result3);  // 25.5 > 21
        Assert.True(result4);  // "25" > 21
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_NumberLessThan_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.NumberLessThan,
            Field = "age",
            Values = ["21"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different numeric types
        var contextFields1 = new Dictionary<string, object?> { ["age"] = 18 };   // int
        var contextFields2 = new Dictionary<string, object?> { ["age"] = 25 };   // int
        var contextFields3 = new Dictionary<string, object?> { ["age"] = 18.5 }; // double
        var contextFields4 = new Dictionary<string, object?> { ["age"] = "18" }; // string

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);
        var result4 = compiled.Predicate(contextFields4, issues);

        // Assert
        Assert.True(result1);  // 18 < 21
        Assert.False(result2); // 25 !< 21
        Assert.True(result3);  // 18.5 < 21
        Assert.True(result4);  // "18" < 21
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_NumberGreaterThan_InvalidType_ReturnsFalse()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.NumberGreaterThan,
            Field = "age",
            Values = ["21"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with non-numeric type
        var contextFields = new Dictionary<string, object?> { ["age"] = "not-a-number" };

        // Act
        var result = compiled.Predicate(contextFields, issues);

        // Assert
        Assert.False(result);
        _ = Assert.Single(issues);
        Assert.Equal(EvaluationIssueType.InvalidFieldType, issues[0].issue);
        Assert.Equal("age", issues[0].name);
    }

    #endregion

    #region ContextFilter Tests - Boolean Operations

    [Fact]
    public void ContextFilter_IsTrue_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.IsTrue, Field = "subscribed", Values = [] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different boolean representations
        var contextFields1 = new Dictionary<string, object?> { ["subscribed"] = true };   // bool
        var contextFields2 = new Dictionary<string, object?> { ["subscribed"] = false };  // bool
        var contextFields3 = new Dictionary<string, object?> { ["subscribed"] = "true" }; // string

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.True(result3);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_IsFalse_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.IsFalse, Field = "subscribed", Values = [] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different boolean representations
        var contextFields1 = new Dictionary<string, object?> { ["subscribed"] = false };   // bool
        var contextFields2 = new Dictionary<string, object?> { ["subscribed"] = true };    // bool
        var contextFields3 = new Dictionary<string, object?> { ["subscribed"] = "false" }; // string

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.True(result3);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_IsTrueFalse_InvalidType_ReturnsFalse()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.IsTrue, Field = "subscribed", Values = [] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with non-boolean type
        var contextFields = new Dictionary<string, object?> { ["subscribed"] = "not-a-boolean" };

        // Act
        var result = compiled.Predicate(contextFields, issues);

        // Assert
        Assert.False(result);
        _ = Assert.Single(issues);
        Assert.Equal(EvaluationIssueType.InvalidFieldType, issues[0].issue);
        Assert.Equal("subscribed", issues[0].name);
    }

    #endregion

    #region ContextFilter Tests - Set/NotSet Operations

    [Fact]
    public void ContextFilter_Set_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.Set, Field = "email", Values = [] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different values
        var contextFields1 = new Dictionary<string, object?> { ["email"] = "user@example.com" }; // non-empty
        var contextFields2 = new Dictionary<string, object?> { ["email"] = "" };                 // empty string
        var contextFields3 = new Dictionary<string, object?> { ["email"] = null };               // null

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.False(result3);
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_NotSet_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter { Operator = ContextOperatorType.NotSet, Field = "email", Values = [] };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different values
        var contextFields1 = new Dictionary<string, object?> { ["email"] = "user@example.com" }; // non-empty
        var contextFields2 = new Dictionary<string, object?> { ["email"] = "" };                 // empty string
        var contextFields3 = new Dictionary<string, object?> { ["email"] = null };               // null

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);

        // Assert
        Assert.False(result1);
        Assert.True(result2);
        Assert.True(result3);
        Assert.Empty(issues);
    }

    #endregion

    #region ContextFilter Tests - Date Operations

    [Fact]
    public void ContextFilter_DateBefore_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.DateBefore,
            Field = "createdAt",
            Values = ["7"], // 7 days from now
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different date representations
        var past = DateTime.UtcNow.AddDays(-10);  // 10 days ago (should match)
        var future = DateTime.UtcNow.AddDays(10); // 10 days in future (should not match)

        var contextFields1 = new Dictionary<string, object?> { ["createdAt"] = past };
        var contextFields2 = new Dictionary<string, object?> { ["createdAt"] = future };
        var contextFields3 =
            new Dictionary<string, object?> { ["createdAt"] = past.ToString("o", CultureInfo.InvariantCulture) };

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);

        // Assert
        Assert.True(result1);  // past is before (now + 7 days)
        Assert.False(result2); // future is not before (now + 7 days)
        Assert.True(result3);  // string representation works too
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_DateAfter_MatchesCorrectly()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.DateAfter,
            Field = "createdAt",
            Values = ["-7"], // 7 days ago
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with different date representations
        var past = DateTime.UtcNow.AddDays(-10);  // 10 days ago (should not match)
        var future = DateTime.UtcNow.AddDays(10); // 10 days in future (should match)
        var recent = DateTime.UtcNow.AddDays(-5); // 5 days ago (should match)

        var contextFields1 = new Dictionary<string, object?> { ["createdAt"] = past };
        var contextFields2 = new Dictionary<string, object?> { ["createdAt"] = future };
        var contextFields3 = new Dictionary<string, object?> { ["createdAt"] = recent };

        // Act
        var result1 = compiled.Predicate(contextFields1, issues);
        var result2 = compiled.Predicate(contextFields2, issues);
        var result3 = compiled.Predicate(contextFields3, issues);

        // Assert
        Assert.False(result1); // past is not after (now - 7 days)
        Assert.True(result2);  // future is after (now - 7 days)
        Assert.True(result3);  // recent is after (now - 7 days)
        Assert.Empty(issues);
    }

    [Fact]
    public void ContextFilter_DateOperations_InvalidType_ReturnsFalse()
    {
        // Arrange
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.DateBefore,
            Field = "createdAt",
            Values = ["7"],
        };
        var compiled = new CompiledFilter(filter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Test with non-date type
        var contextFields = new Dictionary<string, object?> { ["createdAt"] = "not-a-date" };

        // Act
        var result = compiled.Predicate(contextFields, issues);

        // Assert
        Assert.False(result);
        _ = Assert.Single(issues);
        Assert.Equal(EvaluationIssueType.InvalidFieldType, issues[0].issue);
        Assert.Equal("createdAt", issues[0].name);
    }

    #endregion

    #region Invalid Filter Tests

    [Fact]
    public void Filter_UnsupportedType_ReturnsError()
    {
        // Arrange - Create a custom filter that's not supported
        var mockFilter = new MockUnsupportedFilter();
        var compiled = new CompiledFilter(mockFilter);
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(_empty, issues);

        // Assert
        Assert.False(result);
        _ = Assert.Single(issues);
        Assert.Equal(EvaluationIssueType.UnsupportedFilter, issues[0].issue);
        Assert.Equal("MockUnsupportedFilter", issues[0].name);
    }

    [Fact]
    public void ContextFilter_InvalidOperatorValues_ReturnsError()
    {
        // StringContains requires exactly 1 non-empty value
        var filter = new ContextFilter
        {
            Operator = ContextOperatorType.StringContains,
            Field = "email",
            Values = [], // Empty values array is invalid
        };
        var compiled = new CompiledFilter(filter);
        var contextFields = new Dictionary<string, object?> { ["email"] = "test@example.com" };
        var issues = new List<(EvaluationIssueType issue, string name)>();

        // Act
        var result = compiled.Predicate(contextFields, issues);

        // Assert
        Assert.False(result);
        _ = Assert.Single(issues);
        Assert.Equal(EvaluationIssueType.UnsupportedFilter, issues[0].issue);
    }

    #endregion

    #region ToString, Equals, and GetHashCode Tests

    [Fact]
    public void ToString_ReturnsFilterString()
    {
        // Arrange
        var filter = new ConstantFilter { Value = true };
        var compiled = new CompiledFilter(filter);

        // Act
        var result = compiled.ToString();

        // Assert
        Assert.Contains("Filter = ", result);
        Assert.Contains(filter.ToString(), result);
    }

    [Fact]
    public void Equals_SameFilter_ReturnsTrue()
    {
        // Arrange
        var filter1 = new ConstantFilter { Value = true };
        var filter2 = new ConstantFilter { Value = true };

        var compiled1 = new CompiledFilter(filter1);
        var compiled2 = new CompiledFilter(filter2);

        // Act & Assert
        Assert.Equal(compiled1, compiled2);
        Assert.True(compiled1.Equals(compiled2));
        Assert.True(compiled1 == compiled2);
    }

    [Fact]
    public void Equals_DifferentFilter_ReturnsFalse()
    {
        // Arrange
        var filter1 = new ConstantFilter { Value = true };
        var filter2 = new ConstantFilter { Value = false };

        var compiled1 = new CompiledFilter(filter1);
        var compiled2 = new CompiledFilter(filter2);

        // Act & Assert
        Assert.NotEqual(compiled1, compiled2);
        Assert.False(compiled1.Equals(compiled2));
        Assert.False(compiled1 == compiled2);
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var filter = new ConstantFilter { Value = true };
        var compiled = new CompiledFilter(filter);

        // Act & Assert
        Assert.True(compiled.Equals(compiled));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var filter = new ConstantFilter { Value = true };
        var compiled = new CompiledFilter(filter);

        // Act & Assert
        Assert.False(compiled.Equals(null));
        Assert.NotNull(compiled);
        Assert.NotNull(compiled);
    }

    [Fact]
    public void GetHashCode_SameFilter_ReturnsSameHashCode()
    {
        // Arrange
        var filter1 = new ConstantFilter { Value = true };
        var filter2 = new ConstantFilter { Value = true };

        var compiled1 = new CompiledFilter(filter1);
        var compiled2 = new CompiledFilter(filter2);

        // Act
        var hashCode1 = compiled1.GetHashCode();
        var hashCode2 = compiled2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_DifferentFilter_ReturnsDifferentHashCode()
    {
        // Arrange
        var filter1 = new ConstantFilter { Value = true };
        var filter2 = new ConstantFilter { Value = false };

        var compiled1 = new CompiledFilter(filter1);
        var compiled2 = new CompiledFilter(filter2);

        // Act
        var hashCode1 = compiled1.GetHashCode();
        var hashCode2 = compiled2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }

    #endregion
}
