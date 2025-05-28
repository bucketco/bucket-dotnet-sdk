namespace Bucket.Sdk.Tests;

public sealed class CompanyTests
{
    private const string _id = "fake-id";
    private const string _customKey = "custom-attribute-key";
    private const string _customValue = "custom-attribute-value";
    private const string _updatedValue = "custom-attribute-updated-value";

    [Fact]
    public void Constructor_ThrowsWhenArgumentsAreInvalid()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new Company(null!));
        _ = Assert.Throws<ArgumentException>(() => new Company(string.Empty));
        _ = Assert.Throws<ArgumentException>(() => new Company("   "));
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        const string name = "Acme Inc.";
        var avatar = new Uri("https://example.com/avatar.jpg");
        var company = new Company(_id) { Name = name, Avatar = avatar, [_customKey] = _customValue };

        // Assert
        Assert.Equal(_id, company.Id);
        Assert.Equal(name, company.Name);
        Assert.Equal(avatar, company.Avatar);
        _ = Assert.Single(company);

        // Assert
        Assert.Equal(company, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void AddingCustomAttributes_WorksCorrectly()
    {
        // Arrange
        var company = new Company(_id) { { _customKey, _customValue } };

        // Assert
        _ = Assert.Single(company);
        Assert.Equal(company, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void IndexerGetSet_WorksCorrectly()
    {
        // Arrange
        var company = new Company(_id) { [_customKey] = _customValue };

        // Assert
        Assert.Equal(_customValue, company[_customKey]);

        // Act
        company[_customKey] = _updatedValue;

        // Assert
        Assert.Equal(_updatedValue, company[_customKey]);
        _ = Assert.Single(company);
    }

    [Fact]
    public void Enumeration_WorksCorrectly()
    {
        // Arrange
        var company = new Company(_id) { [_customKey] = _customValue };

        // Assert
        Assert.Equal(company, [new KeyValuePair<string, object?>(_customKey, _customValue)]);
    }

    [Fact]
    public void ToFields_ReturnsCorrectDictionary()
    {
        // Arrange
        const string name = "Acme Inc.";
        var avatar = new Uri("https://example.com/avatar.jpg");
        var company = new Company(_id) { Name = name, Avatar = avatar, [_customKey] = _customValue };

        // Act
        var dictionary = company.ToFields();

        // Assert
        Assert.NotNull(dictionary);
        Assert.Equal(4, dictionary.Count);
        Assert.Equal(_id, dictionary["id"]);
        Assert.Equal(name, dictionary["name"]);
        Assert.Equal(avatar.ToString(), dictionary["avatar"]);
        Assert.Equal(_customValue, dictionary[_customKey]);
    }

    [Fact]
    public void ToString_WithAllFields_ReturnsCorrectString()
    {
        // Arrange
        const string name = "Acme Inc.";
        var avatar = new Uri("https://example.com/avatar.jpg");
        var company = new Company(_id) { Name = name, Avatar = avatar, [_customKey] = _customValue };

        // Act
        var result = company.ToString();

        // Assert
        Assert.Equal("Company { \"custom-attribute-key\" = custom-attribute-value, Id = fake-id, Name = Acme Inc., Avatar = https://example.com/avatar.jpg }", result);
    }

    [Fact]
    public void ToString_WithMinimalFields_ReturnsCorrectString()
    {
        // Arrange
        var company = new Company(_id);

        // Act
        var result = company.ToString();

        // Assert
        Assert.Equal("Company { Id = fake-id, Name = , Avatar =  }", result);
    }

    [Fact]
    public void ToString_WithCustomFieldsOnly_ReturnsCorrectString()
    {
        // Arrange
        var company = new Company(_id) { [_customKey] = _customValue };

        // Act
        var result = company.ToString();

        // Assert
        Assert.Equal("Company { \"custom-attribute-key\" = custom-attribute-value, Id = fake-id, Name = , Avatar =  }", result);
    }

    [Fact]
    public void Equals_WithIdenticalCompanies_ReturnsTrue()
    {
        // Arrange
        var company1 = new Company(_id)
        {
            Name = "Test Company",
            Avatar = new Uri("https://example.com/avatar.jpg"),
            [_customKey] = _customValue
        };

        var company2 = new Company(_id)
        {
            Name = "Test Company",
            Avatar = new Uri("https://example.com/avatar.jpg"),
            [_customKey] = _customValue
        };

        // Act & Assert
        Assert.Equal(company1, company2);
        Assert.True(company1 == company2);
        Assert.False(company1 != company2);
    }

    [Fact]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        // Arrange
        var company1 = new Company(_id);
        var company2 = new Company("different-id");

        // Act & Assert
        Assert.NotEqual(company1, company2);
        Assert.False(company1 == company2);
        Assert.True(company1 != company2);
    }

    [Fact]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        // Arrange
        var company1 = new Company(_id) { Name = "First Company" };
        var company2 = new Company(_id) { Name = "Second Company" };

        // Act & Assert
        Assert.NotEqual(company1, company2);
    }

    [Fact]
    public void Equals_WithDifferentAvatar_ReturnsFalse()
    {
        // Arrange
        var company1 = new Company(_id) { Avatar = new Uri("https://example.com/avatar1.jpg") };
        var company2 = new Company(_id) { Avatar = new Uri("https://example.com/avatar2.jpg") };

        // Act & Assert
        Assert.NotEqual(company1, company2);
    }

    [Fact]
    public void Equals_WithDifferentCustomAttributes_ReturnsFalse()
    {
        // Arrange
        var company1 = new Company(_id) { [_customKey] = _customValue };
        var company2 = new Company(_id) { [_customKey] = _updatedValue };

        // Act & Assert
        Assert.NotEqual(company1, company2);
    }

    [Fact]
    public void GetHashCode_WithIdenticalCompanies_ReturnsSameValue()
    {
        // Arrange
        var company1 = new Company(_id)
        {
            Name = "Test Company",
            Avatar = new Uri("https://example.com/avatar.jpg"),
            [_customKey] = _customValue
        };

        var company2 = new Company(_id)
        {
            Name = "Test Company",
            Avatar = new Uri("https://example.com/avatar.jpg"),
            [_customKey] = _customValue
        };

        // Act
        var hashCode1 = company1.GetHashCode();
        var hashCode2 = company2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentCompanies_ReturnsDifferentValues()
    {
        // Arrange
        var company1 = new Company(_id) { Name = "First Company" };
        var company2 = new Company("different-id") { Name = "Second Company" };

        // Act
        var hashCode1 = company1.GetHashCode();
        var hashCode2 = company2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }
}
