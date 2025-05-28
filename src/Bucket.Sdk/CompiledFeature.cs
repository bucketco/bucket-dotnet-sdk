namespace Bucket.Sdk;

/// <summary>
///     Represents a compiled feature used in the evaluation.
/// </summary>
internal sealed record CompiledFeature
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly IReadOnlyList<CompiledFilter>? _configRules;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly FeatureDefinition _featureDefinition;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly IReadOnlyList<CompiledFilter> _flagRules;

    /// <summary>
    ///     Creates a new instance of the <see cref="CompiledFeature" /> class.
    /// </summary>
    /// <param name="featureDefinition">The feature definition to compile.</param>
    public CompiledFeature(FeatureDefinition featureDefinition)
    {
        Debug.Assert(featureDefinition != null);

        _featureDefinition = featureDefinition;
        _flagRules =
        [
            .. featureDefinition.Targeting.Rules.Select(rule => new CompiledFilter(rule.Filter)
            ),
        ];

        _configRules = featureDefinition.Config?.Variants.Select(
            rule => new CompiledFilter(rule.Filter)
        ).ToImmutableArray();
    }

    /// <summary>
    ///     Evaluates the feature using the provided context fields.
    /// </summary>
    /// <param name="contextFields">The context fields used in the evaluation.</param>
    /// <returns>The evaluated feature.</returns>
    public EvaluatedFeature Evaluate(IReadOnlyDictionary<string, object?> contextFields)
    {
        Debug.Assert(contextFields != null);

        var featureEvaluationIssues = new List<(EvaluationIssueType issue, string name)>();
        var evaluatedFlagRules = _flagRules.Select(
            r => r.Predicate(contextFields, featureEvaluationIssues)
        ).ToImmutableArray();

        var isEnabled = evaluatedFlagRules.Any(r => r);

        if (_configRules != null)
        {
            // Evaluate config rules
            var configEvaluationIssues = new List<(EvaluationIssueType issue, string name)>();
            var evaluatedConfigRules = _configRules.Select(
                r => r.Predicate(contextFields, configEvaluationIssues)
            ).ToImmutableArray();

            // Find the first variant that matches
            FeatureConfigVariantDefinition? variant = null;
            for (var i = 0; i < evaluatedConfigRules.Length; i++)
            {
                if (evaluatedConfigRules[i])
                {
                    variant = _featureDefinition.Config?.Variants[i];
                    break;
                }
            }

            // Build a new evaluated feature using the results
            return new(
                _featureDefinition.Key, isEnabled,
                variant != null ? (variant.Key, variant.Payload) : null)
            {
                FlagEvaluationDebugData =
                    new()
                    {
                        Version = _featureDefinition.Targeting.Version,
                        EvaluatedRules = evaluatedFlagRules,
                        EvaluationIssues = featureEvaluationIssues,
                    },
                ConfigEvaluationDebugData = new()
                {
                    Version = _featureDefinition.Config!.Version,
                    EvaluatedRules = evaluatedConfigRules,
                    EvaluationIssues = configEvaluationIssues,
                },
                EvaluationContext = contextFields,
            };
        }

        // Build a new evaluated feature using the results, no config
        return new(_featureDefinition.Key, isEnabled)
        {
            FlagEvaluationDebugData = new()
            {
                Version = _featureDefinition.Targeting.Version,
                EvaluatedRules = evaluatedFlagRules,
                EvaluationIssues = featureEvaluationIssues,
            },
            EvaluationContext = contextFields,
        };
    }


#pragma warning disable IDE0051 // Remove unused private members
    private bool PrintMembers(StringBuilder builder)
#pragma warning restore IDE0051 // Remove unused private members
    {
        Debug.Assert(builder != null);

        _ = builder.Append($"FeatureDefinition = {_featureDefinition}");

        return true;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(CompiledFeature? other) =>
        ReferenceEquals(this, other) ||
        other != null &&
        _featureDefinition == other._featureDefinition;

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() => _featureDefinition.GetHashCode();
}
