namespace Bucket.Sdk;

/// <summary>
///     Represents a tracking context entity.
/// </summary>
[PublicAPI]
public sealed record Context: EntityBase
{
    /// <summary>
    ///     The user associated with the context.
    /// </summary>
    public User? User
    {
        get; init;
    }

    /// <summary>
    ///     The company associated with the context.
    /// </summary>
    public Company? Company
    {
        get; init;
    }

    /// <inheritdoc />
    protected internal override IReadOnlyDictionary<string, object?> ToFields()
    {
        var fields = new List<KeyValuePair<string, object?>>();

        foreach (var (key, value) in this)
        {
            fields.Add(new($"other.{key}", value));
        }

        if (User != null)
        {
            foreach (var (key, value) in User.ToFields())
            {
                fields.Add(new($"user.{key}", value));
            }
        }

        if (Company != null)
        {
            foreach (var (key, value) in Company.ToFields())
            {
                fields.Add(new($"company.{key}", value));
            }
        }

        return fields.ToImmutableDictionary();
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(Context? other) => base.Equals(other);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() => base.GetHashCode();
}
