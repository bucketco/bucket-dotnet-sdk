namespace Bucket.Sdk;

/// <summary>
///     The base class for all entities that can be stored in the Bucket such as <see cref="User" />,
///     <see cref="Company" />,
///     and <see cref="Event" />.
/// </summary>
[DebuggerDisplay("{ToFields(),nq}")]
[PublicAPI]
public abstract record EntityBase: IEnumerable<KeyValuePair<string, object?>>
{
    // The dictionary of attributes for the entity
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Dictionary<string, object?> _attributes = [];

    /// <summary>
    ///     The indexer for the attributes of the entity.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null"/>.</exception>
    public object? this[string key]
    {
        get => _attributes[key];
        set => _attributes[key] = value;
    }

    /// <summary>
    ///     The number of attributes in the entity.
    /// </summary>
    public int Count => _attributes.Count;

    /// <inheritdoc cref="IEnumerable{TValue}.GetEnumerator()" />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _attributes.GetEnumerator();

    /// <inheritdoc cref="IEnumerable.GetEnumerator()" />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     Adds an attribute to the entity.
    /// </summary>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    public void Add(string key, object? value) => _attributes.Add(key, value);

    /// <summary>
    ///     Returns the entity as a dictionary of fields.
    /// </summary>
    /// <returns>The entity as a dictionary.</returns>
    protected internal abstract IReadOnlyDictionary<string, object?> ToFields();

    /// <summary>
    ///     Prints the members of the entity to a <see cref="StringBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder" /> to print to.</param>
    /// <returns><see langword="true"/> if the members were printed; otherwise, <see langword="false"/>.</returns>
    [DebuggerStepThrough]
    protected virtual bool PrintMembers(StringBuilder builder) =>
        this.ToStringElementWise(builder);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public virtual bool Equals(EntityBase? other)
        => ReferenceEquals(this, other) ||
           other != null &&
           ToFields().EqualsElementWise(other.ToFields());

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() =>
        ToFields().GetHashCodeElementWise();
}
