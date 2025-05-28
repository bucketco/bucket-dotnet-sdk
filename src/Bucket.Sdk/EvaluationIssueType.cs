namespace Bucket.Sdk;

/// <summary>
///     Represents an issue encountered during the evaluation of a feature.
/// </summary>
internal enum EvaluationIssueType
{
    /// <summary>
    ///     A field is missing from the evaluation context.
    /// </summary>
    MissingField,

    /// <summary>
    ///     The field type is invalid and cannot be used in the evaluation.
    /// </summary>
    InvalidFieldType,

    /// <summary>
    ///     The filter used in the rule is not supported.
    /// </summary>
    UnsupportedFilter,
}
