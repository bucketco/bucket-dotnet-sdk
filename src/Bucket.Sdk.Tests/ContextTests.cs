namespace Bucket.Sdk.Tests;

public sealed class ContextTests
{
    private const string _customKey = "custom-attribute-key";
    private const string _customValue = "custom-attribute-value";
    private const string _updatedValue = "custom-attribute-updated-value";
    private readonly Company _company = new("company-id");

    private readonly User _user = new("user-id");

    [Fact]
    public void Constructor_DoesNotNeedArguments()
    {
        var context = new Context();

        Assert.Null(context.Company);
        Assert.Null(context.User);
        Assert.Empty(context);
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var context = new Context { User = _user, Company = _company, [_customKey] = _customValue };

        // Assert
        Assert.Equal(_company, context.Company);
        Assert.Equal(_user, context.User);
        Assert.Equal(context, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void AddingCustomAttributes_WorksCorrectly()
    {
        // Arrange
        var context = new Context { { _customKey, _customValue } };

        // Assert
        Assert.Equal(context, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void IndexerGetSet_WorksCorrectly()
    {
        // Arrange
        var context = new Context { [_customKey] = _customValue };

        // Assert
        Assert.Equal(_customValue, context[_customKey]);

        // Act
        context[_customKey] = _updatedValue;

        // Assert
        Assert.Equal(_updatedValue, context[_customKey]);
        _ = Assert.Single(context);
    }

    [Fact]
    public void Enumeration_WorksCorrectly()
    {
        // Arrange
        var context = new Context { [_customKey] = _customValue };

        // Assert
        Assert.Equal(context, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void ToDictionary_ReturnsCorrectDictionary()
    {
        // Arrange
        var context = new Context { User = _user, Company = _company, [_customKey] = _customValue };

        // Act
        var fields = context.ToFields();

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(3, fields.Count);
        Assert.Equal("user-id", fields["user.id"]);
        Assert.Equal("company-id", fields["company.id"]);
        Assert.Equal(_customValue, fields[$"other.{_customKey}"]);
    }


    [Fact]
    public void ToString_WithAllFields_ReturnsCorrectString()
    {
        // Arrange
        var context = new Context
        {
            User = _user,
            Company = _company,
            [_customKey] = _customValue
        };

        // Act
        var result = context.ToString();

        // Assert
        Assert.Equal("Context { \"custom-attribute-key\" = custom-attribute-value, User = User { Id = user-id, Name = , Email = , Avatar =  }, Company = Company { Id = company-id, Name = , Avatar =  } }", result);
    }

    [Fact]
    public void ToString_WithMinimalFields_ReturnsCorrectString()
    {
        // Arrange
        var context = new Context();

        // Act
        var result = context.ToString();

        // Assert
        Assert.Equal("Context { User = , Company =  }", result);
    }

    [Fact]
    public void ToString_WithCustomFieldsOnly_ReturnsCorrectString()
    {
        // Arrange
        var context = new Context
        {
            [_customKey] = _customValue
        };

        // Act
        var result = context.ToString();

        // Assert
        Assert.Equal("Context { \"custom-attribute-key\" = custom-attribute-value, User = , Company =  }", result);
    }

    [Fact]
    public void Equals_WithIdenticalContexts_ReturnsTrue()
    {
        // Arrange
        var context1 = new Context
        {
            User = _user,
            Company = _company,
            [_customKey] = _customValue
        };

        var context2 = new Context
        {
            User = _user,
            Company = _company,
            [_customKey] = _customValue
        };

        // Act & Assert
        Assert.Equal(context1, context2);
        Assert.True(context1 == context2);
        Assert.False(context1 != context2);
    }

    [Fact]
    public void Equals_WithDifferentUser_ReturnsFalse()
    {
        // Arrange
        var user2 = new User("different-user-id");
        var context1 = new Context { User = _user };
        var context2 = new Context { User = user2 };

        // Act & Assert
        Assert.NotEqual(context1, context2);
        Assert.False(context1 == context2);
        Assert.True(context1 != context2);
    }

    [Fact]
    public void Equals_WithDifferentCompany_ReturnsFalse()
    {
        // Arrange
        var company2 = new Company("different-company-id");
        var context1 = new Context { Company = _company };
        var context2 = new Context { Company = company2 };

        // Act & Assert
        Assert.NotEqual(context1, context2);
    }

    [Fact]
    public void Equals_WithOneMissingUser_ReturnsFalse()
    {
        // Arrange
        var context1 = new Context { User = _user };
        var context2 = new Context();

        // Act & Assert
        Assert.NotEqual(context1, context2);
    }

    [Fact]
    public void Equals_WithOneMissingCompany_ReturnsFalse()
    {
        // Arrange
        var context1 = new Context { Company = _company };
        var context2 = new Context();

        // Act & Assert
        Assert.NotEqual(context1, context2);
    }

    [Fact]
    public void Equals_WithDifferentCustomAttributes_ReturnsFalse()
    {
        // Arrange
        var context1 = new Context { [_customKey] = _customValue };
        var context2 = new Context { [_customKey] = _updatedValue };

        // Act & Assert
        Assert.NotEqual(context1, context2);
    }

    [Fact]
    public void GetHashCode_WithIdenticalContexts_ReturnsSameValue()
    {
        // Arrange
        var context1 = new Context
        {
            User = _user,
            Company = _company,
            [_customKey] = _customValue
        };

        var context2 = new Context
        {
            User = _user,
            Company = _company,
            [_customKey] = _customValue
        };

        // Act
        var hashCode1 = context1.GetHashCode();
        var hashCode2 = context2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentContexts_ReturnsDifferentValues()
    {
        // Arrange
        var context1 = new Context { User = _user };
        var context2 = new Context { Company = _company };

        // Act
        var hashCode1 = context1.GetHashCode();
        var hashCode2 = context2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
