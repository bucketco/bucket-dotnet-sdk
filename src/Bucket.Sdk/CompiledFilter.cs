namespace Bucket.Sdk;

using System.Security.Cryptography;

/// <summary>
///     A compiled filter used in the evaluation of a features' rules.
/// </summary>
internal sealed record CompiledFilter
{
    /// <summary>
    ///     Creates a new instance of the <see cref="CompiledFilter" /> class.
    /// </summary>
    /// <param name="filter">The filter to compile.</param>
    public CompiledFilter(Filter filter)
    {
        Debug.Assert(filter != null);

        Filter = filter;
        Predicate = Compile(filter);
    }

    /// <summary>
    ///     The original filter that was compiled.
    /// </summary>
    private Filter Filter
    {
        get;
    }

    /// <summary>
    ///     The compiled predicate used in the evaluation.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public CompiledFilterPredicate Predicate
    {
        get;
    }

    private static CompiledFilterPredicate Error(Filter filter)
    {
        Debug.Assert(filter != null);

        return (_, issues) =>
        {
            issues.Add((EvaluationIssueType.UnsupportedFilter, filter.GetType().Name));
            return false;
        };
    }

    private static CompiledFilterPredicate Compile(GroupFilter filter)
    {
        Debug.Assert(filter != null);

        var innerPredicates = filter.Filters.Select(Compile).ToImmutableArray();

        return filter.Operator == GroupFilterOperatorType.And
            ? (contextFields, issues) => innerPredicates.All(p => p(contextFields, issues))
            : (contextFields, issues) => innerPredicates.Any(p => p(contextFields, issues));
    }

    private static CompiledFilterPredicate Compile(NegationFilter filter)
    {
        Debug.Assert(filter != null);

        var innerPredicate = Compile(filter.Filter);
        return (contextFields, issues) => !innerPredicate(contextFields, issues);
    }

    private static CompiledFilterPredicate Compile(ConstantFilter filter)
    {
        Debug.Assert(filter != null);

        return (_, _) => filter.Value;
    }

    private static CompiledFilterPredicate Compile(PartialRolloutFilter filter)
    {
        Debug.Assert(filter != null);

        return (contextFields, issues) =>
        {
            if (!contextFields.TryGetValue(filter.PartialRolloutAttribute,
                    out var rolloutFieldValue))
            {
                issues.Add((EvaluationIssueType.MissingField, filter.PartialRolloutAttribute));

                return false;
            }

            rolloutFieldValue ??= string.Empty;

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{filter.Key}.{rolloutFieldValue}"));
            var hashValue = BitConverter.ToInt32(hash, 0) & 0xfffff; // Extract 20 bits
            var rolloutBucket = (int) (hashValue / (double) 0xfffff * 100000);

            return rolloutBucket < filter.PartialRolloutThreshold;
        };
    }

    private static CompiledFilterPredicate Compile(ContextFilter filter)
    {
        Debug.Assert(filter != null);

        switch (filter.Operator)
        {
            case ContextOperatorType.StringContains:
            case ContextOperatorType.StringNotContains:
                if (filter.Values.Count != 1 || string.IsNullOrEmpty(filter.Values[0]))
                {
                    return Error(filter);
                }

                break;
            case ContextOperatorType.NumberGreaterThan:
            case ContextOperatorType.NumberLessThan:
            case ContextOperatorType.DateBefore:
            case ContextOperatorType.DateAfter:
                if (filter.Values.Count != 1 || !double.TryParse(filter.Values[0], out _))
                {
                    return Error(filter);
                }

                break;
            case ContextOperatorType.Is:
            case ContextOperatorType.IsNot:
                if (filter.Values.Count != 1)
                {
                    return Error(filter);
                }

                break;
            case ContextOperatorType.Set:
            case ContextOperatorType.NotSet:
            case ContextOperatorType.IsTrue:
            case ContextOperatorType.IsFalse:
                if (filter.Values.Count != 0)
                {
                    return Error(filter);
                }

                break;
            case ContextOperatorType.StringAnyOf:
            case ContextOperatorType.StringNotAnyOf:
                break;

            default:
                return Error(filter);
        }

        var valueAsString = (filter.Values.Count == 1 ? filter.Values[0] : null) ?? string.Empty;
        var valueAsDouble = double.TryParse(valueAsString, out var parsedDouble) ? parsedDouble : 0;

        return (contextFields, issues) =>
        {
            if (contextFields.TryGetValue(filter.Field, out var contextFieldValue))
            {
                return filter.Operator switch
                {
                    ContextOperatorType.StringContains =>
                        coerceToString(contextFieldValue).Contains(valueAsString, StringComparison.OrdinalIgnoreCase),
                    ContextOperatorType.StringNotContains =>
                        !coerceToString(contextFieldValue).Contains(valueAsString, StringComparison.OrdinalIgnoreCase),
                    ContextOperatorType.NumberGreaterThan =>
                        tryCoerceToDouble(contextFieldValue, issues, out var v) &&
                        v > valueAsDouble,
                    ContextOperatorType.NumberLessThan =>
                        tryCoerceToDouble(contextFieldValue, issues, out var v) &&
                        v < valueAsDouble,
                    ContextOperatorType.Is =>
                        coerceToString(contextFieldValue) == valueAsString,
                    ContextOperatorType.IsNot =>
                        coerceToString(contextFieldValue) != valueAsString,
                    ContextOperatorType.Set =>
                        !string.IsNullOrEmpty(coerceToString(contextFieldValue)),
                    ContextOperatorType.NotSet =>
                        string.IsNullOrEmpty(coerceToString(contextFieldValue)),
                    ContextOperatorType.IsTrue =>
                        tryCoerceToBool(contextFieldValue, issues, out var v) &&
                        v,
                    ContextOperatorType.IsFalse =>
                        tryCoerceToBool(contextFieldValue, issues, out var v) &&
                        !v,
                    ContextOperatorType.StringAnyOf =>
                        filter.Values.Contains(coerceToString(contextFieldValue)),
                    ContextOperatorType.StringNotAnyOf =>
                        !filter.Values.Contains(coerceToString(contextFieldValue)),
                    ContextOperatorType.DateBefore =>
                        tryCoerceToDateTime(contextFieldValue, issues, out var v) &&
                        v < DateTime.UtcNow.AddDays(valueAsDouble),
                    ContextOperatorType.DateAfter =>
                        tryCoerceToDateTime(contextFieldValue, issues, out var v) &&
                        v > DateTime.UtcNow.AddDays(valueAsDouble),
                    _ => fail(issues),
                };
            }

            issues.Add((EvaluationIssueType.MissingField, filter.Field));
            return false;
        };

        string coerceToString(object? value)
        {
            return value switch
            {
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value?.ToString() ?? string.Empty,
            };
        }

        bool tryCoerceToDouble(
            object? value,
            IList<(EvaluationIssueType issue, string name)> issues,
            out double output)
        {
            Debug.Assert(issues != null);

            switch (value)
            {
                case null:
                    output = 0;
                    return fail(issues);
                case double d:
                    output = d;
                    return true;
                case int i:
                    output = i;
                    return true;
                case long l:
                    output = l;
                    return true;
                case float f:
                    output = f;
                    return true;
                case decimal dc:
                    output = (double) dc;
                    return true;
                default:
                    return double.TryParse(coerceToString(value), NumberStyles.Any, CultureInfo.InvariantCulture,
                        out output) || fail(issues);
            }
        }

        bool tryCoerceToBool(
            object? value,
            IList<(EvaluationIssueType issue, string name)> issues,
            out bool output)
        {
            Debug.Assert(issues != null);

            switch (value)
            {
                case null:
                    output = false;
                    return fail(issues);
                case bool b:
                    output = b;
                    return true;
                default:
                    return bool.TryParse(coerceToString(value), out output) || fail(issues);
            }
        }

        bool tryCoerceToDateTime(
            object? value,
            IList<(EvaluationIssueType issue, string name)> issues,
            out DateTime output
        )
        {
            Debug.Assert(issues != null);

            switch (value)
            {
                case null:
                    output = DateTime.MinValue;
                    return fail(issues);
                case DateTime dt:
                    output = dt.ToUniversalTime();
                    return true;
                case DateTimeOffset dto:
                    output = dto.UtcDateTime;
                    return true;
                case DateOnly d:
                    output = d.ToDateTime(TimeOnly.MinValue);
                    return true;
                default:
                    {
                        return DateTime.TryParse(coerceToString(value), CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal, out output) || fail(issues);
                    }
            }
        }

        bool fail(
            IList<(EvaluationIssueType issue, string name)> issues)
        {
            Debug.Assert(issues != null);

            issues.Add((EvaluationIssueType.InvalidFieldType, filter.Field));
            return false;
        }
    }

    private static CompiledFilterPredicate Compile(Filter filter)
    {
        Debug.Assert(filter != null);

        return filter switch
        {
            GroupFilter groupFilter => Compile(groupFilter),
            NegationFilter negationFilter => Compile(negationFilter),
            ConstantFilter constantFilter => Compile(constantFilter),
            PartialRolloutFilter partialRolloutFilter => Compile(partialRolloutFilter),
            ContextFilter contextFilter => Compile(contextFilter),
            _ => Error(filter),
        };
    }

#pragma warning disable IDE0051 // Remove unused private members
    private bool PrintMembers(StringBuilder builder)
#pragma warning restore IDE0051 // Remove unused private members
    {
        Debug.Assert(builder != null);

        _ = builder.Append($"Filter = {Filter}");

        return true;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(CompiledFilter? other) =>
        ReferenceEquals(this, other) ||
        other != null &&
        Filter == other.Filter;

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() => Filter.GetHashCode();
}
