namespace Bucket.Sdk.Tests;

public class AnyTests
{
    [Fact]
    public void ImplicitConversion_FromString_CreatesAnyWithCorrectValue()
    {
        // Arrange
        const string testString = "test string";

        // Act
        Any any = testString;

        // Assert
        Assert.Equal(testString, any.As<string>());
        Assert.Equal(testString, any.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromBool_CreatesAnyWithCorrectValue()
    {
        // Arrange
        const bool testBool = true;

        // Act
        Any any = testBool;

        // Assert
        Assert.Equal(testBool, any.As<bool>());
        Assert.Equal(testBool.ToString(), any.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromInt_CreatesAnyWithCorrectValue()
    {
        // Arrange
        const int testInt = 42;

        // Act
        Any any = testInt;

        // Assert
        Assert.Equal(testInt, any.As<int>());
        Assert.Equal(testInt.ToString(), any.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromDouble_CreatesAnyWithCorrectValue()
    {
        // Arrange
        const double testDouble = 3.14159;

        // Act
        Any any = testDouble;

        // Assert
        Assert.Equal(testDouble, any.As<double>());
        Assert.Equal(testDouble.ToString(CultureInfo.InvariantCulture), any.ToString());
    }

    [Fact]
    public void ImplicitConversion_FromDateTimeOffset_CreatesAnyWithCorrectValue()
    {
        // Arrange
        var testDate = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        Any any = testDate;

        // Assert
        var result = any.As<DateTimeOffset>();
        Assert.Equal(testDate, result);
    }

    [Fact]
    public void ImplicitConversion_FromDateOnly_CreatesAnyWithCorrectValue()
    {
        // Arrange
        var testDate = new DateOnly(2023, 1, 1);

        // Act
        Any any = testDate;

        // Assert
        var result = any.As<DateOnly>();
        Assert.Equal(testDate, result);
    }

    [Fact]
    public void ImplicitConversion_FromTimeOnly_CreatesAnyWithCorrectValue()
    {
        // Arrange
        var testTime = new TimeOnly(12, 0, 0);

        // Act
        Any any = testTime;

        // Assert
        var result = any.As<TimeOnly>();
        Assert.Equal(testTime, result);
    }

    [Fact]
    public void ImplicitConversion_FromJsonElement_CreatesAnyWithCorrectValue()
    {
        // Arrange
        var testElement = JsonSerializer.SerializeToElement(new
        {
            Name = "Test",
            Value = 123
        });

        // Act
        Any any = testElement;

        // Assert
        var result = any.As<JsonElement>();
        Assert.Equal(testElement.GetRawText(), result.GetRawText());
    }

    [Fact]
    public void As_DeserializesToSpecifiedType()
    {
        // Arrange
        var testObject = new TestClass { Name = "Test", Value = 42 };
        Any any = JsonSerializer.SerializeToElement(testObject);

        // Act
        var result = any.As<TestClass>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testObject.Name, result.Name);
        Assert.Equal(testObject.Value, result.Value);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        // Arrange
        Any any1 = "test";
        Any any2 = "test";

        // Act & Assert
        Assert.True(any1.Equals(any2));
        Assert.True(any1 == any2);
        Assert.False(any1 != any2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        Any any1 = "test";
        Any any2 = "different";

        // Act & Assert
        Assert.False(any1.Equals(any2));
        Assert.False(any1 == any2);
        Assert.True(any1 != any2);
    }

    [Fact]
    public void Equals_WithNonAnyObject_ReturnsFalse()
    {
        // Arrange
        Any any = "test";

        // Act & Assert
        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.False(any.Equals((object?) "test"));
    }

    [Fact]
    public void GetHashCode_ReturnsSameValueForEqualAny_Instances()
    {
        // Arrange
        Any any1 = "test";
        Any any2 = "test";

        // Act & Assert
        Assert.Equal(any1.GetHashCode(), any2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ReturnsDifferentValueForDifferentAny_Instances()
    {
        // Arrange
        Any any1 = "test";
        Any any2 = "different";

        // Act & Assert
        Assert.NotEqual(any1.GetHashCode(), any2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsJsonStringRepresentation()
    {
        // Arrange
        const string testString = "test string";
        const int testInt = 42;
        const bool testBool = true;

        // Act & Assert
        Assert.Equal(testString, ((Any) testString).ToString());
        Assert.Equal(testInt.ToString(), ((Any) testInt).ToString());
        Assert.Equal(testBool.ToString(), ((Any) testBool).ToString());
    }

    [Fact]
    public void JsonConverter_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        Any original = "test string";
        var options = new JsonSerializerOptions();

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<Any>(json, options);

        // Assert
        Assert.Equal(original, deserialized);
        Assert.Equal(original.As<string>(), deserialized.As<string>());
    }

    [Fact]
    public void JsonConverter_SerializesAndDeserializesComplexObject()
    {
        // Arrange
        var testObject = new
        {
            Name = "Test",
            Value = 123
        };
        Any original = JsonSerializer.SerializeToElement(testObject);
        var options = new JsonSerializerOptions();

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<Any>(json, options);

        // Assert
        Assert.Equal(original, deserialized);

        var customObj = new
        {
            Name = "Test",
            Value = 123
        };
        Assert.Equal(JsonSerializer.Serialize(customObj), JsonSerializer.Serialize(deserialized));
    }

    [Fact]
    public void As_WithDifferentType_ReturnsDefault()
    {
        // Arrange
        Any any = "not an int";

        // Act & Assert
        _ = Assert.Throws<JsonException>(() => any.As<int>());
    }

    [Fact]
    public void TryAs_WithValidType_ReturnsTrue()
    {
        // Arrange
        var testObject = new TestClass { Name = "Test", Value = 42 };
        Any any = JsonSerializer.SerializeToElement(testObject);

        // Act
        var success = any.TryAs(out TestClass? result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(testObject.Name, result.Name);
        Assert.Equal(testObject.Value, result.Value);
    }

    [Fact]
    public void TryAs_WithInvalidType_ReturnsFalse()
    {
        // Arrange
        Any any = "not a test class";

        // Act
        var success = any.TryAs(out TestClass? result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryAsInt32_WithValidValue_ReturnsTrue()
    {
        // Arrange
        Any any = 42;

        // Act
        var success = any.TryAsInt32(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(42, result);
    }

    [Fact]
    public void TryAsInt32_WithInvalidValue_ReturnsFalse()
    {
        // Arrange
        Any any = "not an int";

        // Act
        var success = any.TryAsInt32(out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(0, result); // Default value
    }

    [Fact]
    public void TryAsDouble_WithValidValue_ReturnsTrue()
    {
        // Arrange
        Any any = 3.14159;

        // Act
        var success = any.TryAsDouble(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(3.14159, result);
    }

    [Fact]
    public void TryAsDouble_WithInvalidValue_ReturnsFalse()
    {
        // Arrange
        Any any = "not a double";

        // Act
        var success = any.TryAsDouble(out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(0.0, result); // Default value
    }

    [Fact]
    public void TryAsBoolean_WithValidValue_ReturnsTrue()
    {
        // Arrange
        Any any = true;

        // Act
        var success = any.TryAsBoolean(out var result);

        // Assert
        Assert.True(success);
        Assert.True(result);
    }

    [Fact]
    public void TryAsBoolean_WithInvalidValue_ReturnsFalse()
    {
        // Arrange
        Any any = "not a boolean";

        // Act
        var success = any.TryAsBoolean(out var result);

        // Assert
        Assert.False(success);
        Assert.False(result); // Default value
    }

    [Fact]
    public void TryAsDateTimeOffset_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var testDate = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        Any any = testDate;

        // Act
        var success = any.TryAsDateTimeOffset(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(testDate, result);
    }

    [Fact]
    public void TryAsDateTimeOffset_WithInvalidValue_ReturnsFalse()
    {
        // Arrange
        Any any = "not a date";

        // Act
        var success = any.TryAsDateTimeOffset(out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result); // Default value
    }

    [Fact]
    public void TryAsDateTime_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var testDate = new DateTime(2023, 1, 1, 12, 0, 0);
        Any any = testDate;

        // Act
        var success = any.TryAsDateTime(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(testDate, result);
    }

    [Fact]
    public void TryAsDateTime_WithInvalidValue_ReturnsFalse()
    {
        // Arrange
        Any any = "not a date";

        // Act
        var success = any.TryAsDateTime(out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result); // Default value
    }

    [Fact]
    public void TryAsString_WithValidValue_ReturnsTrue()
    {
        // Arrange
        const string testString = "test string";
        Any any = testString;

        // Act
        var success = any.TryAsString(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(testString, result);
    }

    [Fact]
    public void TryAsString_WithInvalidValue_ReturnsFalse()
    {
        // Arrange
        Any any = 42; // Not a string

        // Act
        var success = any.TryAsString(out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryAsGuid_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var testGuid = Guid.NewGuid();
        Any any = testGuid;

        // Act
        var success = any.TryAsGuid(out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(testGuid, result);
    }

    [Fact]
    public void TryAsGuid_WithInvalidValue_ReturnsFalse()
    {
        // Arrange
        Any any = "not a guid";

        // Act
        var success = any.TryAsGuid(out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result); // Default value
    }

    private class TestClass
    {
        public string? Name
        {
            get;
            [UsedImplicitly]
            set;
        }

        public int Value
        {
            get;
            [UsedImplicitly]
            set;
        }
    }
}
