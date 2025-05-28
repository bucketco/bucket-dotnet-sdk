namespace Bucket.Sdk;

/// <summary>
///     Represents a company entity.
/// </summary>
[PublicAPI]
public sealed record Company: EntityBase
{
    /// <summary>
    ///     Creates a new instance of the <see cref="Company" /> class.
    /// </summary>
    /// <param name="id">The company ID.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id" /> is <see langword="null"/> or whitespace.</exception>
    public Company(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }

    /// <summary>
    ///     The ID of the company.
    /// </summary>
    public string Id
    {
        get;
    }

    /// <summary>
    ///     The name of the company (optional).
    /// </summary>
    public string? Name
    {
        get; init;
    }

    /// <summary>
    ///     The avatar of the company (optional).
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

        if (Avatar != null)
        {
            fields.Add(new("avatar", Avatar.ToString()));
        }

        return fields.ToImmutableDictionary();
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(Company? other) => base.Equals(other);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() => base.GetHashCode();
}
