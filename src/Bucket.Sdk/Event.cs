namespace Bucket.Sdk;

/// <summary>
///     Represents an event entity.
/// </summary>
[PublicAPI]
public sealed record Event: EntityBase
{
    /// <summary>
    ///     Creates a new instance of the <see cref="Event" /> class.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="user">The user associated with the event.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is <see langword="null"/> or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user" /> is <see langword="null"/>.</exception>
    public Event(string name, User user)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(user);

        Name = name;
        User = user;
    }

    /// <summary>
    ///     The name of the event.
    /// </summary>
    public string Name
    {
        get;
    }

    /// <summary>
    ///     The user that triggered the event.
    /// </summary>
    public User User
    {
        get;
    }

    /// <summary>
    ///     The company associated with the event (optional).
    /// </summary>
    public Company? Company
    {
        get; init;
    }

    /// <inheritdoc />
    protected internal override IReadOnlyDictionary<string, object?> ToFields()
    {
        var fields = this.ToList();

        fields.Add(new("name", Name));

        foreach (var (key, value) in User.ToFields())
        {
            fields.Add(new($"user.{key}", value));
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
    public bool Equals(Event? other) => base.Equals(other);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() => base.GetHashCode();
}
