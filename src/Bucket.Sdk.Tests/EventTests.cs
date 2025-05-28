namespace Bucket.Sdk.Tests;

public sealed class EventTests
{
    private const string _eventName = "fake-event";

    private const string _customKey = "custom-attribute-key";
    private const string _customValue = "custom-attribute-value";
    private const string _updatedValue = "custom-attribute-updated-value";

    private readonly User _user = new("user-id");

    [Fact]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new Event(null!, _user));
        _ = Assert.Throws<ArgumentException>(() => new Event(string.Empty, _user));
        _ = Assert.Throws<ArgumentException>(() => new Event("   ", _user));
        _ = Assert.Throws<ArgumentNullException>(() => new Event(_eventName, null!));
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var @event = new Event(_eventName, _user) { [_customKey] = _customValue };

        // Assert
        Assert.Equal(_eventName, @event.Name);
        Assert.Equal(_user, @event.User);

        Assert.Equal(1, @event.Count);
        Assert.Equal(@event, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void AddingCustomAttributes_WorksCorrectly()
    {
        // Arrange
        var @event = new Event(_eventName, _user) { { _customKey, _customValue } };

        // Assert
        _ = Assert.Single(@event);
        Assert.Equal(@event, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void IndexerGetSet_WorksCorrectly()
    {
        // Arrange
        var @event = new Event(_eventName, _user) { [_customKey] = _customValue };

        // Assert
        Assert.Equal(_customValue, @event[_customKey]);

        // Act
        @event[_customKey] = _updatedValue;

        // Assert
        Assert.Equal(_updatedValue, @event[_customKey]);
        _ = Assert.Single(@event);
    }

    [Fact]
    public void Enumeration_WorksCorrectly()
    {
        // Arrange
        var @event = new Event(_eventName, _user) { [_customKey] = _customValue };

        // Assert
        Assert.Equal(@event, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void ToFields_ReturnsCorrectDictionary()
    {
        // Arrange
        const string userId = "user-id";
        const string companyId = "company-id";

        var company = new Company(companyId);
        var @event = new Event(_eventName, _user) { Company = company, [_customKey] = _customValue };

        // Act
        var fields = @event.ToFields();

        // Assert
        Assert.NotNull(fields);
        Assert.Equal(4, fields.Count);

        Assert.Equal(_eventName, fields["name"]);
        Assert.Equal(userId, fields["user.id"]);
        Assert.Equal(companyId, fields["company.id"]);
        Assert.Equal(_customValue, fields[_customKey]);
    }

    [Fact]
    public void ToString_WithAllFields_ReturnsCorrectString()
    {
        // Arrange
        var @event = new Event(_eventName, _user) { Company = new Company("fake-company") };

        // Act
        var result = @event.ToString();

        // Assert
        Assert.Equal("Event { Name = fake-event, User = User { Id = user-id, Name = , Email = , Avatar =  }, Company = Company { Id = fake-company, Name = , Avatar =  } }", result);
    }

    [Fact]
    public void ToString_WithMinimalFields_ReturnsCorrectString()
    {
        // Arrange
        var @event = new Event(_eventName, _user);

        // Act
        var result = @event.ToString();

        // Assert
        Assert.Equal("Event { Name = fake-event, User = User { Id = user-id, Name = , Email = , Avatar =  }, Company =  }", result);
    }

    [Fact]
    public void ToString_WithCustomFieldsOnly_ReturnsCorrectString()
    {
        // Arrange
        var @event = new Event(_eventName, _user) { [_customKey] = _customValue };

        // Act
        var result = @event.ToString();

        // Assert
        Assert.Equal("Event { \"custom-attribute-key\" = custom-attribute-value, Name = fake-event, User = User { Id = user-id, Name = , Email = , Avatar =  }, Company =  }", result);
    }

    [Fact]
    public void Equals_WithIdenticalEvents_ReturnsTrue()
    {
        // Arrange
        var company = new Company("company-id") { Name = "Test Company" };
        var event1 = new Event(_eventName, _user)
        {
            Company = company,
            [_customKey] = _customValue
        };

        var event2 = new Event(_eventName, _user)
        {
            Company = company,
            [_customKey] = _customValue
        };

        // Act & Assert
        Assert.Equal(event1, event2);
        Assert.True(event1 == event2);
        Assert.False(event1 != event2);
    }

    [Fact]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        // Arrange
        var event1 = new Event(_eventName, _user);
        var event2 = new Event("different-event", _user);

        // Act & Assert
        Assert.NotEqual(event1, event2);
        Assert.False(event1 == event2);
        Assert.True(event1 != event2);
    }

    [Fact]
    public void Equals_WithDifferentUser_ReturnsFalse()
    {
        // Arrange
        var user1 = new User("user-1");
        var user2 = new User("user-2");
        var event1 = new Event(_eventName, user1);
        var event2 = new Event(_eventName, user2);

        // Act & Assert
        Assert.NotEqual(event1, event2);
    }

    [Fact]
    public void Equals_WithDifferentCompany_ReturnsFalse()
    {
        // Arrange
        var company1 = new Company("company-1");
        var company2 = new Company("company-2");
        var event1 = new Event(_eventName, _user) { Company = company1 };
        var event2 = new Event(_eventName, _user) { Company = company2 };

        // Act & Assert
        Assert.NotEqual(event1, event2);
    }

    [Fact]
    public void Equals_WithOneMissingCompany_ReturnsFalse()
    {
        // Arrange
        var company = new Company("company-id");
        var event1 = new Event(_eventName, _user) { Company = company };
        var event2 = new Event(_eventName, _user);

        // Act & Assert
        Assert.NotEqual(event1, event2);
    }

    [Fact]
    public void Equals_WithDifferentCustomAttributes_ReturnsFalse()
    {
        // Arrange
        var event1 = new Event(_eventName, _user) { [_customKey] = _customValue };
        var event2 = new Event(_eventName, _user) { [_customKey] = _updatedValue };

        // Act & Assert
        Assert.NotEqual(event1, event2);
    }

    [Fact]
    public void GetHashCode_WithIdenticalEvents_ReturnsSameValue()
    {
        // Arrange
        var company = new Company("company-id") { Name = "Test Company" };
        var event1 = new Event(_eventName, _user)
        {
            Company = company,
            [_customKey] = _customValue
        };

        var event2 = new Event(_eventName, _user)
        {
            Company = company,
            [_customKey] = _customValue
        };

        // Act
        var hashCode1 = event1.GetHashCode();
        var hashCode2 = event2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentEvents_ReturnsDifferentValues()
    {
        // Arrange
        var event1 = new Event(_eventName, _user);
        var event2 = new Event("different-event", new User("different-id"));

        // Act
        var hashCode1 = event1.GetHashCode();
        var hashCode2 = event2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
