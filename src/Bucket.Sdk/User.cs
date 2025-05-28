namespace Bucket.Sdk;

/// <summary>
///     Represents a user entity.
/// </summary>
[PublicAPI]
public sealed record User: EntityBase
{
    /// <summary>
    ///     Creates a new instance of the <see cref="User" /> class.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id" /> is <see langword="null"/> or whitespace.</exception>
    public User(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }

    /// <summary>
    ///     The ID of the user.
    /// </summary>
    public string Id
    {
        get;
    }

    /// <summary>
    ///     The name of the user (optional).
    /// </summary>
    public string? Name
    {
        get; init;
    }

    /// <summary>
    ///     The email of the user (optional).
    /// </summary>
    public string? Email
    {
        get; init;
    }

    /// <summary>
    ///     The avatar of the user (optional).
    /// </summary>
    public Uri? Avatar
    {
        get; init;
    }

    /// <inheritdoc />
    protected internal override IReadOnlyDictionary<string, object?> ToFields()
    {
        var fields = this.ToList();

        fields.Add(new("id", Id));

        if (Name != null)
        {
            fields.Add(new("name", Name));
        }

        if (Email != null)
        {
            fields.Add(new("email", Email));
        }

        if (Avatar != null)
        {
            fields.Add(new("avatar", Avatar.ToString()));
        }

        return fields.ToImmutableDictionary();
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(User? other) => base.Equals(other);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() => base.GetHashCode();
}
