namespace Bucket.Sdk;

/// <summary>
///     Represents any value that is not known at compile time.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonConverter))]
public readonly struct Any: IEquatable<Any>
{
    /// <summary>
    ///     The JSON converter for the <see cref="Any" /> type.
    /// </summary>
    internal class JsonConverter: JsonConverter<Any>
    {
        /// <inheritdoc />
        public override Any Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<JsonElement>(ref reader, options));

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Any value, JsonSerializerOptions options)
        {
            if (value.Value.ValueKind != JsonValueKind.Undefined)
            {
                JsonSerializer.Serialize(writer, value.Value, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    /// <summary>
    ///     The value of the current <see cref="Any" /> instance.
    /// </summary>
    internal JsonElement Value
    {
        get;
    }

    private Any(JsonElement value) => Value = value;

    private Any(object? value) => Value = JsonSerializer.SerializeToElement(value);

    /// <summary>
    ///     Deserializes the value of the current <see cref="Any" /> instance to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the value to.</typeparam>
    /// <returns>
    ///     The deserialized value.
    /// </returns>
    /// <exception cref="JsonException">
    ///     The value is not a valid <typeparamref name="T" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    ///     The value is not a valid <typeparamref name="T" />.
    /// </exception>
    public T? As<T>() => Value.Deserialize<T>(JsonContext.PayloadOptions);

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the value to.</typeparam>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAs<T>(out T? value)
    {
        try
        {
            value = Value.Deserialize<T>(JsonContext.PayloadOptions);
            return true;
        }
        catch (Exception exception)
        {
            if (exception is JsonException or NotSupportedException)
            {
                value = default;
                return false;
            }
            else
            {
                throw;
            }
        }
    }

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to an <see cref="int" />.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAsInt32(out int value)
    {
        if (Value.ValueKind == JsonValueKind.Number)
        {
            return Value.TryGetInt32(out value);
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to a <see cref="double" />.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAsDouble(out double value)
    {
        if (Value.ValueKind == JsonValueKind.Number)
        {
            return Value.TryGetDouble(out value);
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to a <see cref="bool" />.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAsBoolean(out bool value)
    {
        if (Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = Value.GetBoolean();
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to a <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAsDateTimeOffset(out DateTimeOffset value)
    {
        if (Value.ValueKind == JsonValueKind.String)
        {
            return Value.TryGetDateTimeOffset(out value);
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to a <see cref="DateTime" />.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAsDateTime(out DateTime value)
    {
        if (Value.ValueKind == JsonValueKind.String)
        {
            return Value.TryGetDateTime(out value);
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to a <see cref="string" />.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAsString(out string? value)
    {
        if (Value.ValueKind == JsonValueKind.String)
        {
            value = Value.GetString();
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Tries to deserialize the value of the current <see cref="Any" /> instance to a <see cref="Guid" />.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <returns>
    ///     <see langword="true" /> if the value was deserialized successfully; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryAsGuid(out Guid value)
    {
        if (Value.ValueKind == JsonValueKind.String)
        {
            return Value.TryGetGuid(out value);
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override bool Equals(object? obj) => obj is Any other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Any other) => JsonElement.DeepEquals(Value, other.Value);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() => Value.GetRawText().GetHashCode();

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override string ToString() => Value.ToString();

    /// <summary>
    ///     Determines if two <see cref="Any" /> instances are equal.
    /// </summary>
    public static bool operator ==(Any left, Any right) => left.Equals(right);

    /// <summary>
    ///     Determines if two <see cref="Any" /> instances are not equal.
    /// </summary>
    public static bool operator !=(Any left, Any right) => !left.Equals(right);

    /// <summary>
    ///     Implicitly converts a <see cref="string" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(string value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="bool" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(bool value) => new(value);

    /// <summary>
    ///     Implicitly converts an <see cref="int" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(int value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="double" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(double value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="Guid" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(Guid value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="DateTime" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(DateTime value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="DateTimeOffset" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(DateTimeOffset value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="DateOnly" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(DateOnly value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="TimeOnly" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(TimeOnly value) => new(value);

    /// <summary>
    ///     Implicitly converts a <see cref="JsonElement" /> to an <see cref="Any" />.
    /// </summary>
    public static implicit operator Any(JsonElement value) => new(value);
}
