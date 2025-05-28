namespace Bucket.Sdk.Tests;

public sealed class EvaluationDebugDataTests
{
    [Fact]
    public void ToString_WithNoRulesOrIssues_ReturnsCorrectString()
    {
        // Arrange
        var debugData = new EvaluationDebugData
        {
            Version = 42,
            EvaluatedRules = [],
            EvaluationIssues = []
        };

        // Act
        var result = debugData.ToString();

        // Assert
        Assert.Equal("EvaluationDebugData { Version = 42, EvaluatedRules = [ ], EvaluationIssues = [ ] }", result);
    }

    [Fact]
    public void ToString_WithRulesAndIssues_ReturnsCorrectString()
    {
        // Arrange
        var debugData = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues =
            [
                (EvaluationIssueType.UnsupportedFilter, "regex"),
            ]
        };

        // Act
        var result = debugData.ToString();

        // Assert
        Assert.Equal("EvaluationDebugData { Version = 10, EvaluatedRules = [ False, True ], EvaluationIssues = [ (UnsupportedFilter, regex) ] }", result);
    }

    [Fact]
    public void Equals_WithIdenticalDebugData_ReturnsTrue()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues =
            [
                (EvaluationIssueType.MissingField, "userId"),
                (EvaluationIssueType.InvalidFieldType, "age")
            ]
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues =
            [
                (EvaluationIssueType.MissingField, "userId"),
                (EvaluationIssueType.InvalidFieldType, "age")
            ]
        };

        // Act & Assert
        Assert.Equal(debugData1, debugData2);
        Assert.True(debugData1 == debugData2);
        Assert.False(debugData1 != debugData2);
    }

    [Fact]
    public void Equals_WithDifferentVersion_ReturnsFalse()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = []
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 11,
            EvaluatedRules = [false, true],
            EvaluationIssues = []
        };

        // Act & Assert
        Assert.NotEqual(debugData1, debugData2);
        Assert.False(debugData1 == debugData2);
        Assert.True(debugData1 != debugData2);
    }

    [Fact]
    public void Equals_WithDifferentEvaluatedRules_ReturnsFalse()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = []
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [true, false],
            EvaluationIssues = []
        };

        // Act & Assert
        Assert.NotEqual(debugData1, debugData2);
    }

    [Fact]
    public void Equals_WithDifferentNumberOfEvaluatedRules_ReturnsFalse()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = []
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true, false],
            EvaluationIssues = []
        };

        // Act & Assert
        Assert.NotEqual(debugData1, debugData2);
    }

    [Fact]
    public void Equals_WithDifferentEvaluationIssues_ReturnsFalse()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.MissingField, "userId")]
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.InvalidFieldType, "age")]
        };

        // Act & Assert
        Assert.NotEqual(debugData1, debugData2);
    }

    [Fact]
    public void Equals_WithDifferentNumberOfEvaluationIssues_ReturnsFalse()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.MissingField, "userId")]
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues =
            [
                (EvaluationIssueType.MissingField, "userId"),
                (EvaluationIssueType.InvalidFieldType, "age")
            ]
        };

        // Act & Assert
        Assert.NotEqual(debugData1, debugData2);
    }

    [Fact]
    public void GetHashCode_WithIdenticalDebugData_ReturnsSameValue()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.MissingField, "userId")]
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.MissingField, "userId")]
        };

        // Act
        var hashCode1 = debugData1.GetHashCode();
        var hashCode2 = debugData2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentDebugData_ReturnsDifferentValues()
    {
        // Arrange
        var debugData1 = new EvaluationDebugData
        {
            Version = 10,
            EvaluatedRules = [false, true],
            EvaluationIssues = [(EvaluationIssueType.MissingField, "userId")]
        };

        var debugData2 = new EvaluationDebugData
        {
            Version = 11,
            EvaluatedRules = [true, false],
            EvaluationIssues = [(EvaluationIssueType.InvalidFieldType, "age")]
        };

        // Act
        var hashCode1 = debugData1.GetHashCode();
        var hashCode2 = debugData2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
