namespace Bucket.Sdk;

/// <summary>
///     The JSON context that is used for fast serialization of objects.
/// </summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    AllowOutOfOrderMetadataProperties = true,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)
]
[JsonSerializable(typeof(IReadOnlyList<OutputMessage>), TypeInfoPropertyName = "OutputMessages")]
[JsonSerializable(typeof(TrackEventMessage))]
[JsonSerializable(typeof(ResponseBase))]
[JsonSerializable(typeof(FeaturesDefinitionsResponse))]
[JsonSerializable(typeof(FeaturesEvaluateResponse))]
[JsonSerializable(typeof(JsonElement))]
internal partial class JsonContext: JsonSerializerContext
{
    /// <summary>
    ///     A wrapper for the <see cref="IReadOnlyList{T}" /> interface.
    /// </summary>
    internal sealed record List<T>: IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> _list;

        /// <summary>
        ///     Creates a new instance of the <see cref="List{T}" /> class.
        /// </summary>
        /// <param name="list">The list to wrap.</param>
        [DebuggerStepThrough]
        internal List(IReadOnlyList<T> list)
        {
            Debug.Assert(list != null);
            _list = list;
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        /// <inheritdoc />
        [DebuggerStepThrough]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public int Count => _list.Count;

        /// <inheritdoc />
        public T this[int index] => _list[index];

        [DebuggerStepThrough]
#pragma warning disable IDE0051 // Remove unused private members
        private bool PrintMembers(StringBuilder builder) => _list.ToStringElementWise(builder);
#pragma warning restore IDE0051 // Remove unused private members

        /// <inheritdoc />
        [DebuggerStepThrough]
        public bool Equals(List<T>? other) =>
            ReferenceEquals(this, other) || other != null && _list.EqualsElementWise(other);

        /// <inheritdoc />
        [DebuggerStepThrough]
        public override int GetHashCode() =>
            _list.GetHashCodeElementWise();
    }

    /// <summary>
    ///     A wrapper for the attributes used in communication with the Bucket.
    /// </summary>
    internal sealed record Attributes: IReadOnlyDictionary<string, object?>
    {
        /// <summary>
        ///     The converter used to serialize and deserialize the attributes.
        /// </summary>
        private readonly IReadOnlyDictionary<string, object?> _dictionary;

        /// <summary>
        ///     Creates a new instance of the <see cref="Attributes" /> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to wrap.</param>
        [DebuggerStepThrough]
        internal Attributes(IReadOnlyDictionary<string, object?> dictionary)
        {
            Debug.Assert(dictionary != null);
            _dictionary = dictionary;
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _dictionary.GetEnumerator();

        /// <inheritdoc />
        [DebuggerStepThrough]
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        /// <inheritdoc />
        public int Count => _dictionary.Count;

        /// <inheritdoc />
        [DebuggerStepThrough]
        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

        /// <inheritdoc />
        [DebuggerStepThrough]
        public bool TryGetValue(string key, out object? value) =>
            _dictionary.TryGetValue(key, out value);

        /// <inheritdoc />
        public object? this[string key] => _dictionary[key];

        /// <inheritdoc />
        public IEnumerable<string> Keys => _dictionary.Keys;

        /// <inheritdoc />
        public IEnumerable<object?> Values => _dictionary.Values;

        [DebuggerStepThrough]
#pragma warning disable IDE0051 // Remove unused private members
        private bool PrintMembers(StringBuilder builder) =>
#pragma warning restore IDE0051 // Remove unused private members
            _dictionary.ToStringElementWise(builder);

        /// <inheritdoc />
        [DebuggerStepThrough]
        public bool Equals(Attributes? other) =>
            ReferenceEquals(this, other) || other != null && _dictionary.EqualsElementWise(other);

        /// <inheritdoc />
        [DebuggerStepThrough]
        public override int GetHashCode() =>
            _dictionary.GetHashCodeElementWise();
    }

    /// <summary>
    ///     The JSON options used for transfer of data between Bucket and SDK.
    /// </summary>
    public static JsonSerializerOptions TransferOptions => Default.Options;

    /// <summary>
    ///     The JSON options used for deserialization of config payloads.
    /// </summary>
    public static JsonSerializerOptions PayloadOptions
    {
        get;
    } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
///     Serves as the base type of output messages.
/// </summary>

[JsonConverter(typeof(JsonStringEnumConverter<OutputBulkMessageType>))]
internal enum OutputBulkMessageType
{
    /// <summary>
    ///     Represents a user message.
    /// </summary>
    [JsonStringEnumMemberName("user")] User,

    /// <summary>
    ///     Represents a company message.
    /// </summary>
    [JsonStringEnumMemberName("company")] Company,

    /// <summary>
    ///     Represents a feature message.
    /// </summary>
    [JsonStringEnumMemberName("feature-flag-event")]
    Feature,

    /// <summary>
    ///     Represents a track event message.
    /// </summary>
    [JsonStringEnumMemberName("event")] Event,

    /// <summary>
    ///     Represents a feedback message.
    /// </summary>
    [JsonStringEnumMemberName("feedback")] Feedback,

    /// <summary>
    ///     Represents a prompt event message.
    /// </summary>
    [JsonStringEnumMemberName("prompt-event")]
    PromptEvent,

    /// <summary>
    ///     Represents a channel status event message.
    /// </summary>
    [JsonStringEnumMemberName("channel-status-event")]
    ChannelStatusEvent,
}

/// <summary>
///     The base class for all output messages.
/// </summary>
[JsonDerivedType(typeof(CompanyMessage))]
[JsonDerivedType(typeof(UserMessage))]
[JsonDerivedType(typeof(TrackEventMessage))]
[JsonDerivedType(typeof(FeatureEventMessage))]
internal abstract record OutputMessage;

/// <summary>
///     The base class for all bulk output messages.
/// </summary>
/// <param name="Type">The type of the output message.</param>
internal abstract record OutputBulkMessage([property: JsonPropertyName("type")] OutputBulkMessageType Type): OutputMessage;

/// <summary>
///     Represents the metadata of a tracking request.
/// </summary>
internal sealed record TrackingMetadata
{
    /// <summary>
    ///     The status of the tracked entity.
    /// </summary>
    [JsonPropertyName("active")]
    public bool? Active
    {
        get; init;
    }
}

/// <summary>
///     Represents a company message.
/// </summary>
internal sealed record CompanyMessage(): OutputBulkMessage(OutputBulkMessageType.Company)
{
    private readonly IReadOnlyDictionary<string, object?>? _attributes;

    /// <summary>
    ///     The ID of the company.
    /// </summary>
    [JsonPropertyName("companyId")]
    [JsonRequired]
    public required string CompanyId
    {
        get; init;
    }

    /// <summary>
    ///     The ID of the user to associate with the company (optional).
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId
    {
        get; init;
    }

    /// <summary>
    ///     The attributes of the company (optional).
    /// </summary>
    [JsonPropertyName("attributes")]
    public IReadOnlyDictionary<string, object?>? Attributes
    {
        get => _attributes;
        init => _attributes = value != null ? new JsonContext.Attributes(value) : null;
    }

    /// <summary>
    ///     The metadata of the request (optional).
    /// </summary>
    [JsonPropertyName("context")]
    public TrackingMetadata? Metadata
    {
        get; init;
    }
}

/// <summary>
///     Represents a user message.
/// </summary>
internal sealed record UserMessage(): OutputBulkMessage(OutputBulkMessageType.User)
{
    private readonly IReadOnlyDictionary<string, object?>? _attributes;

    /// <summary>
    ///     The ID of the user.
    /// </summary>
    [JsonPropertyName("userId")]
    [JsonRequired]
    public required string UserId
    {
        get; init;
    }

    /// <summary>
    ///     The attributes of the user (optional).
    /// </summary>
    [JsonPropertyName("attributes")]
    public IReadOnlyDictionary<string, object?>? Attributes
    {
        get => _attributes;
        init => _attributes = value != null ? new JsonContext.Attributes(value) : null;
    }

    /// <summary>
    ///     The metadata of the request (optional).
    /// </summary>
    [JsonPropertyName("context")]
    public TrackingMetadata? Metadata
    {
        get; init;
    }
}

/// <summary>
///     Represents an event message.
/// </summary>
internal sealed record TrackEventMessage: OutputMessage
{
    private readonly IReadOnlyDictionary<string, object?>? _attributes;

    /// <summary>
    ///     The name of the event.
    /// </summary>
    [JsonPropertyName("event")]
    [JsonRequired]
    public required string Name
    {
        get; init;
    }

    /// <summary>
    ///     The ID of the user.
    /// </summary>
    [JsonPropertyName("userId")]
    [JsonRequired]
    public required string UserId
    {
        get; init;
    }

    /// <summary>
    ///     The ID of the company (optional).
    /// </summary>
    [JsonPropertyName("companyId")]
    public string? CompanyId
    {
        get; init;
    }

    /// <summary>
    ///     The attributes of the user (optional).
    /// </summary>

    [JsonPropertyName("attributes")]
    public IReadOnlyDictionary<string, object?>? Attributes
    {
        get => _attributes;
        init => _attributes = value != null ? new JsonContext.Attributes(value) : value;
    }

    /// <summary>
    ///     The metadata of the request (optional).
    /// </summary>
    [JsonPropertyName("context")]
    public TrackingMetadata? Metadata
    {
        get; init;
    }
}

/// <summary>
///     Represents a feature event message.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FeatureEventType>))]
internal enum FeatureEventType
{
    /// <summary>
    ///     Represents a check flag event.
    /// </summary>
    [JsonStringEnumMemberName("check-is-enabled")]
    CheckFlag,

    /// <summary>
    ///     Represents a check config event.
    /// </summary>
    [JsonStringEnumMemberName("check-config")]
    CheckConfig,

    /// <summary>
    ///     Represents an evaluate flag event.
    /// </summary>
    [JsonStringEnumMemberName("evaluate-is-enabled")]
    EvaluateFlag,

    /// <summary>
    ///     Represents an evaluate config event.
    /// </summary>
    [JsonStringEnumMemberName("evaluate-config")]
    EvaluateConfig,
}

/// <summary>
///     Represents a feature event message.
/// </summary>
internal sealed record FeatureEventMessage(): OutputBulkMessage(OutputBulkMessageType.Feature)
{
    private readonly IReadOnlyDictionary<string, object?>? _context;
    private readonly IReadOnlyList<bool>? _evaluatedRules;
    private readonly IReadOnlyList<string>? _missingFields;

    /// <summary>
    ///     The key of the feature.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonRequired]
    public required string FeatureKey
    {
        get; init;
    }

    /// <summary>
    ///     The subtype of the feature event message.
    /// </summary>
    [JsonPropertyName("action")]
    [JsonRequired]
    public required FeatureEventType SubType
    {
        get; init;
    }

    /// <summary>
    ///     The result of the evaluation.
    /// </summary>
    [JsonPropertyName("evalResult")]
    [JsonRequired]
    public required Any EvaluationResult
    {
        get; init;
    }

    /// <summary>
    ///     The targeting version of the feature (optional).
    /// </summary>
    [JsonPropertyName("targetingVersion")]
    public int? TargetingVersion
    {
        get; init;
    }

    /// <summary>
    ///     The evaluation context of the feature (optional).
    /// </summary>
    [JsonPropertyName("evalContext")]
    public IReadOnlyDictionary<string, object?>? Context
    {
        get => _context;
        init => _context = value != null ? new JsonContext.Attributes(value) : value;
    }

    /// <summary>
    ///     The rule evaluation results of the feature (optional).
    /// </summary>
    [JsonPropertyName("evalRuleResults")]
    public IReadOnlyList<bool>? EvaluatedRules
    {
        get => _evaluatedRules;
        init => _evaluatedRules = value != null ? new JsonContext.List<bool>(value) : value;
    }

    /// <summary>
    ///     The evaluation missing fields of the feature (optional).
    /// </summary>
    [JsonPropertyName("evalMissingFields")]
    public IReadOnlyList<string>? MissingFields
    {
        get => _missingFields;
        init => _missingFields = value != null ? new JsonContext.List<string>(value) : value;
    }


}

/// <summary>
///     The error details.
/// </summary>
internal sealed record ErrorDetails
{
    /// <summary>
    ///     The error code.
    /// </summary>
    [JsonPropertyName("code")]
    [JsonRequired]
    public required string Code
    {
        get; init;
    }

    /// <summary>
    ///     The error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message
    {
        get; init;
    }
}

/// <summary>
///     The base class for all responses.
/// </summary>
[JsonDerivedType(typeof(FeaturesDefinitionsResponse))]
[JsonDerivedType(typeof(FeaturesEvaluateResponse))]
internal record ResponseBase
{
    /// <summary>
    ///     Whether the response is successful.
    /// </summary>
    [JsonPropertyName("success")]
    [JsonRequired]
    public required bool Success
    {
        get; init;
    }

    /// <summary>
    ///     The validation errors.
    /// </summary>
    [JsonPropertyName("error")]
    public ErrorDetails? Error
    {
        get; init;
    }
}

/// <summary>
///     The operator type for a group filter.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<GroupFilterOperatorType>))]
internal enum GroupFilterOperatorType
{
    /// <summary>
    ///     The AND operator.
    /// </summary>
    [JsonStringEnumMemberName("and")] And,

    /// <summary>
    ///     The OR operator.
    /// </summary>
    [JsonStringEnumMemberName("or")] Or,
}

/// <summary>
///     The operator type for a context filter.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContextOperatorType>))]
internal enum ContextOperatorType
{
    /// <summary>
    ///     The IS operator.
    /// </summary>
    [JsonStringEnumMemberName("IS")] Is,

    /// <summary>
    ///     The IS NOT operator.
    /// </summary>
    [JsonStringEnumMemberName("IS_NOT")] IsNot,

    /// <summary>
    ///     The ANY OF operator.
    /// </summary>
    [JsonStringEnumMemberName("ANY_OF")] StringAnyOf,

    /// <summary>
    ///     The NOT ANY OF operator.
    /// </summary>
    [JsonStringEnumMemberName("NOT_ANY_OF")]
    StringNotAnyOf,

    /// <summary>
    ///     The CONTAINS operator.
    /// </summary>
    [JsonStringEnumMemberName("CONTAINS")] StringContains,

    /// <summary>
    ///     The NOT CONTAINS operator.
    /// </summary>
    [JsonStringEnumMemberName("NOT_CONTAINS")]
    StringNotContains,

    /// <summary>
    ///     The GREATER THAN operator.
    /// </summary>
    [JsonStringEnumMemberName("GT")] NumberGreaterThan,

    /// <summary>
    ///     The LESS THAN operator.
    /// </summary>
    [JsonStringEnumMemberName("LT")] NumberLessThan,

    /// <summary>
    ///     The AFTER operator.
    /// </summary>
    [JsonStringEnumMemberName("AFTER")] DateAfter,

    /// <summary>
    ///     The BEFORE operator.
    /// </summary>
    [JsonStringEnumMemberName("BEFORE")] DateBefore,

    /// <summary>
    ///     The SET operator.
    /// </summary>
    [JsonStringEnumMemberName("SET")] Set,

    /// <summary>
    ///     The NOT SET operator.
    /// </summary>
    [JsonStringEnumMemberName("NOT_SET")] NotSet,

    /// <summary>
    ///     The IS TRUE operator.
    /// </summary>
    [JsonStringEnumMemberName("IS_TRUE")] IsTrue,

    /// <summary>
    ///     The IS FALSE operator.
    /// </summary>
    [JsonStringEnumMemberName("IS_FALSE")] IsFalse,
}

/// <summary>
///     The base class for all filters.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GroupFilter), GroupFilter.Tag)]
[JsonDerivedType(typeof(NegationFilter), NegationFilter.Tag)]
[JsonDerivedType(typeof(ConstantFilter), ConstantFilter.Tag)]
[JsonDerivedType(typeof(ContextFilter), ContextFilter.Tag)]
[JsonDerivedType(typeof(PartialRolloutFilter), PartialRolloutFilter.Tag)]
internal abstract record Filter;

/// <summary>
///     Represents a group filter.
/// </summary>
internal sealed record GroupFilter: Filter
{
    public const string Tag = "group";

    private readonly IReadOnlyList<Filter> _filters = null!;

    /// <summary>
    ///     The operator of the group filter.
    /// </summary>
    [JsonPropertyName("operator")]
    [JsonRequired]
    public required GroupFilterOperatorType Operator
    {
        get; init;
    }

    /// <summary>
    ///     The filters of the group filter.
    /// </summary>
    [JsonPropertyName("filters")]
    [JsonRequired]
    public required IReadOnlyList<Filter> Filters
    {
        get => _filters;
        init => _filters = new JsonContext.List<Filter>(value);
    }
}

/// <summary>
///     Represents a negation filter.
/// </summary>
internal sealed record NegationFilter: Filter
{
    public const string Tag = "negation";

    /// <summary>
    ///     The filter to negate.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonRequired]
    public required Filter Filter
    {
        get; init;
    }
}

/// <summary>
///     Represents a constant filter.
/// </summary>
internal sealed record ConstantFilter: Filter
{
    public const string Tag = "constant";

    /// <summary>
    ///     The value of the constant filter.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonRequired]
    public required bool Value
    {
        get; init;
    }
}

/// <summary>
///     Represents a context filter.
/// </summary>
internal sealed record ContextFilter: Filter
{
    public const string Tag = "context";

    private readonly IReadOnlyList<string> _values = null!;

    /// <summary>
    ///     The operator of the context filter.
    /// </summary>
    [JsonPropertyName("operator")]
    [JsonRequired]
    public required ContextOperatorType Operator
    {
        get; init;
    }

    /// <summary>
    ///     The field of the context filter.
    /// </summary>
    [JsonPropertyName("field")]
    [JsonRequired]
    public required string Field
    {
        get; init;
    }

    /// <summary>
    ///     The values of the context filter.
    /// </summary>
    [JsonPropertyName("values")]
    [JsonRequired]
    public required IReadOnlyList<string> Values
    {
        get => _values;
        init => _values = new JsonContext.List<string>(value);
    }
}

/// <summary>
///     Represents a partial rollout filter.
/// </summary>
internal sealed record PartialRolloutFilter: Filter
{
    public const string Tag = "rolloutPercentage";

    /// <summary>
    ///     The key of the partial rollout filter.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonRequired]
    public required string Key
    {
        get; init;
    }

    /// <summary>
    ///     The partial rollout attribute of the partial rollout filter.
    /// </summary>
    [JsonPropertyName("partialRolloutAttribute")]
    [JsonRequired]
    public required string PartialRolloutAttribute
    {
        get; init;
    }

    /// <summary>
    ///     The partial rollout threshold of the partial rollout filter.
    /// </summary>
    [JsonPropertyName("partialRolloutThreshold")]
    [JsonRequired]
    public required double PartialRolloutThreshold
    {
        get; init;
    }
}

/// <summary>
///     Represents a feature targeting rule.
/// </summary>
internal sealed record FeatureTargetingRule
{
    /// <summary>
    ///     The filter of the feature targeting rule.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonRequired]
    public required Filter Filter
    {
        get; init;
    }
}

/// <summary>
///     Represents a feature targeting configuration.
/// </summary>
internal sealed record FeatureTargetingDefinition
{
    private readonly IReadOnlyList<FeatureTargetingRule> _rules = null!;

    /// <summary>
    ///     The version of the feature targeting.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonRequired]
    public required int Version
    {
        get; init;
    }

    /// <summary>
    ///     The rules of the feature targeting.
    /// </summary>
    [JsonPropertyName("rules")]
    [JsonRequired]
    public required IReadOnlyList<FeatureTargetingRule> Rules
    {
        get => _rules;
        init => _rules = new JsonContext.List<FeatureTargetingRule>(value);
    }
}

/// <summary>
///     Represents a feature configuration variant.
/// </summary>
internal sealed record FeatureConfigVariantDefinition
{
    /// <summary>
    ///     The key of the feature configuration variant.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonRequired]
    public required string Key
    {
        get; init;
    }

    /// <summary>
    ///     The filter of the feature configuration variant.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonRequired]
    public required Filter Filter
    {
        get; init;
    }

    /// <summary>
    ///     The payload of the feature configuration variant.
    /// </summary>
    [JsonPropertyName("payload")]
    public Any Payload
    {
        get; init;
    }
}

/// <summary>
///     Represents a feature configuration.
/// </summary>
internal sealed record FeatureConfigDefinition
{
    private readonly IReadOnlyList<FeatureConfigVariantDefinition> _variants = null!;

    /// <summary>
    ///     The version of the feature configuration.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonRequired]
    public required int Version
    {
        get; init;
    }

    /// <summary>
    ///     The variants of the feature configuration.
    /// </summary>
    [JsonPropertyName("variants")]
    [JsonRequired]
    public required IReadOnlyList<FeatureConfigVariantDefinition> Variants
    {
        get => _variants;
        init => _variants = new JsonContext.List<FeatureConfigVariantDefinition>(value);
    }
}

/// <summary>
///     Represents a feature definition.
/// </summary>
internal sealed record FeatureDefinition
{
    /// <summary>
    ///     The key of the feature.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonRequired]
    public required string Key
    {
        get; init;
    }

    /// <summary>
    ///     The targeting of the feature.
    /// </summary>
    [JsonPropertyName("targeting")]
    [JsonRequired]
    public required FeatureTargetingDefinition Targeting
    {
        get; init;
    }

    /// <summary>
    ///     The config of the feature.
    /// </summary>
    [JsonPropertyName("config")]
    public FeatureConfigDefinition? Config
    {
        get; init;
    }
}

/// <summary>
///     Represents a features response.
/// </summary>
internal sealed record FeaturesDefinitionsResponse: ResponseBase
{
    private readonly IReadOnlyList<FeatureDefinition> _features = null!;

    /// <summary>
    ///     The features of the features response.
    /// </summary>
    [JsonPropertyName("features")]
    [JsonRequired]
    public required IReadOnlyList<FeatureDefinition> Features
    {
        get => _features;
        init => _features = new JsonContext.List<FeatureDefinition>(value);
    }
}

/// <summary>
///     Represents an evaluated feature configuration.
/// </summary>
internal sealed record FeatureConfigEvaluated
{
    private readonly IReadOnlyList<string>? _missingFields;
    private readonly IReadOnlyList<bool>? _evaluatedRules;

    /// <summary>
    ///     The key of the feature configuration.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonRequired]
    public required string Key
    {
        get; init;
    }

    /// <summary>
    ///     The version of the feature configuration.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonRequired]
    public required int Version
    {
        get; init;
    }

    /// <summary>
    ///     The payload of the feature configuration.
    /// </summary>
    [JsonPropertyName("payload")]
    public Any Payload
    {
        get; init;
    }

    /// <summary>
    ///     The missing context fields when evaluating the config.
    /// </summary>
    [JsonPropertyName("missingContextFields")]
    public IReadOnlyList<string>? MissingFields
    {
        get => _missingFields;
        init => _missingFields = value != null ? new JsonContext.List<string>(value) : null;
    }

    /// <summary>
    ///     The rule evaluation results of the feature.
    /// </summary>

    [JsonPropertyName("ruleEvaluationResults")]
    public IReadOnlyList<bool>? EvaluatedRules
    {
        get => _evaluatedRules;
        init => _evaluatedRules = value != null ? new JsonContext.List<bool>(value) : null;
    }
}

/// <summary>
///     Represents an evaluated feature.
/// </summary>
internal sealed record FeatureEvaluated
{
    private readonly IReadOnlyList<string>? _missingFields;
    private readonly IReadOnlyList<bool>? _evaluatedRules;

    /// <summary>
    ///     The key of the feature.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonRequired]
    public required string Key
    {
        get; init;
    }

    /// <summary>
    ///     The enabled status of the feature.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    [JsonRequired]
    public required bool Enabled
    {
        get; init;
    }

    /// <summary>
    ///     The version of the feature.
    /// </summary>
    [JsonPropertyName("targetingVersion")]
    [JsonRequired]
    public required int TargetingVersion
    {
        get; init;
    }

    /// <summary>
    ///     The evaluated feature configuration.
    /// </summary>
    [JsonPropertyName("config")]
    public FeatureConfigEvaluated? Config
    {
        get; init;
    }

    /// <summary>
    ///     The missing context fields of the feature.
    /// </summary>
    [JsonPropertyName("missingContextFields")]
    public IReadOnlyList<string>? MissingFields
    {
        get => _missingFields;
        init => _missingFields = value != null ? new JsonContext.List<string>(value) : null;
    }

    /// <summary>
    ///     The rule evaluation results of the feature.
    /// </summary>
    [JsonPropertyName("ruleEvaluationResults")]
    public IReadOnlyList<bool>? EvaluatedRules
    {
        get => _evaluatedRules;
        init => _evaluatedRules = value != null ? new JsonContext.List<bool>(value) : null;
    }
}

/// <summary>
///     Represents an evaluate response.
/// </summary>
internal sealed record FeaturesEvaluateResponse: ResponseBase
{
    private readonly IReadOnlyList<FeatureEvaluated> _features = null!;

    /// <summary>
    ///     The features of the evaluate response.
    /// </summary>
    [JsonPropertyName("features")]
    [JsonRequired]
    public required IReadOnlyList<FeatureEvaluated> Features
    {
        get => _features;
        init => _features = new JsonContext.List<FeatureEvaluated>(value);
    }

    /// <summary>
    ///     The remote context used status of the evaluate response.
    /// </summary>
    [JsonPropertyName("remoteContextUsed")]
    [JsonRequired]
    public required bool UsedRemoteContext
    {
        get; init;
    }
}
