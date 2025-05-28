namespace Bucket.Sdk.Tests;

public sealed class UserTests
{
    private const string _id = "fake-id";
    private const string _name = "John Doe";
    private const string _email = "john@example.com";
    private const string _avatar = "https://example.com/avatar.jpg";
    private const string _customKey = "custom-attribute-key";
    private const string _customValue = "custom-attribute-value";
    private const string _updatedValue = "custom-attribute-updated-value";

    [Fact]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new User(null!));
        _ = Assert.Throws<ArgumentException>(() => new User(string.Empty));
        _ = Assert.Throws<ArgumentException>(() => new User("   "));
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var avatar = new Uri(_avatar);
        var user = new User(_id)
        {
            Name = _name,
            Email = _email,
            Avatar = avatar,
            [_customKey] = _customValue,
        };

        Assert.Equal(_id, user.Id);
        Assert.Equal(_name, user.Name);
        Assert.Equal(_email, user.Email);
        Assert.Equal(avatar, user.Avatar);
        Assert.Equal(1, user.Count);

        Assert.Equal(user, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void AddingCustomAttributes_WorksCorrectly()
    {
        var user = new User(_id) { { _customKey, _customValue } };

        Assert.Equal(1, user.Count);
        Assert.Equal(user, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void IndexerGetSet_WorksCorrectly()
    {
        var user = new User(_id) { [_customKey] = _customValue };
        Assert.Equal(_customValue, user[_customKey]);

        user[_customKey] = _updatedValue;
        Assert.Equal(_updatedValue, user[_customKey]);
        Assert.Equal(1, user.Count);
    }

    [Fact]
    public void Enumeration_WorksCorrectly()
    {
        var user = new User(_id) { [_customKey] = _customValue };

        Assert.Equal(user, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void ToFields_ReturnsCorrectDictionary()
    {
        var user = new User(_id)
        {
            Name = _name,
            Email = _email,
            Avatar = new Uri(_avatar),
            [_customKey] = _customValue,
        };

        var fields = user.ToFields();

        Assert.NotNull(fields);
        Assert.Equal(5, fields.Count);

        Assert.Equal(_id, fields["id"]);
        Assert.Equal(_name, fields["name"]);
        Assert.Equal(_email, fields["email"]);
        Assert.Equal(_avatar, fields["avatar"]);
        Assert.Equal(_customValue, fields[_customKey]);
    }

    [Fact]
    public void ToString_WithAllFields_ReturnsCorrectString()
    {
        // Arrange
        var user = new User(_id)
        {
            Name = _name,
            Email = _email,
            Avatar = new Uri(_avatar),
            [_customKey] = _customValue,
        };

        // Act
        var result = user.ToString();

        // Assert
        Assert.Equal("User { \"custom-attribute-key\" = custom-attribute-value, Id = fake-id, Name = John Doe, Email = john@example.com, Avatar = https://example.com/avatar.jpg }", result);
    }

    [Fact]
    public void ToString_WithMinimalFields_ReturnsCorrectString()
    {
        // Arrange
        var user = new User(_id);

        // Act
        var result = user.ToString();

        // Assert
        Assert.Equal("User { Id = fake-id, Name = , Email = , Avatar =  }", result);
    }

    [Fact]
    public void ToString_WithCustomFieldsOnly_ReturnsCorrectString()
    {
        // Arrange
        var user = new User(_id) { [_customKey] = _customValue };

        // Act
        var result = user.ToString();

        // Assert
        Assert.Equal("User { \"custom-attribute-key\" = custom-attribute-value, Id = fake-id, Name = , Email = , Avatar =  }", result);
    }

    [Fact]
    public void Equals_WithIdenticalUsers_ReturnsTrue()
    {
        // Arrange
        var user1 = new User(_id)
        {
            Name = _name,
            Email = _email,
            Avatar = new Uri(_avatar),
            [_customKey] = _customValue
        };

        var user2 = new User(_id)
        {
            Name = _name,
            Email = _email,
            Avatar = new Uri(_avatar),
            [_customKey] = _customValue
        };

        // Act & Assert
        Assert.Equal(user1, user2);
        Assert.True(user1 == user2);
        Assert.False(user1 != user2);
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var user1 = new User(_id);
        var user2 = new User("different-id");

        // Act & Assert
        Assert.NotEqual(user1, user2);
        Assert.False(user1 == user2);
        Assert.True(user1 != user2);
    }

    [Fact]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        // Arrange
        var user1 = new User(_id) { Name = "First User" };
        var user2 = new User(_id) { Name = "Second User" };

        // Act & Assert
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void Equals_WithDifferentEmail_ReturnsFalse()
    {
        // Arrange
        var user1 = new User(_id) { Email = "user1@example.com" };
        var user2 = new User(_id) { Email = "user2@example.com" };

        // Act & Assert
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void Equals_WithDifferentAvatar_ReturnsFalse()
    {
        // Arrange
        var user1 = new User(_id) { Avatar = new Uri("https://example.com/avatar1.jpg") };
        var user2 = new User(_id) { Avatar = new Uri("https://example.com/avatar2.jpg") };

        // Act & Assert
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void Equals_WithDifferentCustomAttributes_ReturnsFalse()
    {
        // Arrange
        var user1 = new User(_id) { [_customKey] = _customValue };
        var user2 = new User(_id) { [_customKey] = _updatedValue };

        // Act & Assert
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void GetHashCode_WithIdenticalUsers_ReturnsSameValue()
    {
        // Arrange
        var user1 = new User(_id)
        {
            Name = _name,
            Email = _email,
            Avatar = new Uri(_avatar),
            [_customKey] = _customValue
        };

        var user2 = new User(_id)
        {
            Name = _name,
            Email = _email,
            Avatar = new Uri(_avatar),
            [_customKey] = _customValue
        };

        // Act
        var hashCode1 = user1.GetHashCode();
        var hashCode2 = user2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentUsers_ReturnsDifferentValues()
    {
        // Arrange
        var user1 = new User(_id) { Name = "First User" };
        var user2 = new User("different-id") { Name = "Second User" };

        // Act
        var hashCode1 = user1.GetHashCode();
        var hashCode2 = user2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
