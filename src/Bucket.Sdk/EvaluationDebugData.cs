namespace Bucket.Sdk;

/// <summary>
///     Represents the debug data for the evaluation of a feature.
/// </summary>
internal sealed record EvaluationDebugData
{
    /// <summary>
    ///     The version of the evaluated feature entity.
    /// </summary>
    public required int Version
    {
        get; init;
    }

    /// <summary>
    ///     The evaluation results for all the flag rules.
    /// </summary>
    public required IReadOnlyList<bool> EvaluatedRules
    {
        get; init;
    }

    /// <summary>
    ///     All the issues encountered during the flag rules' evaluation.
    /// </summary>
    public required IReadOnlyList<(EvaluationIssueType type, string name)> EvaluationIssues
    {
        get; init;
    }

    [DebuggerStepThrough]
#pragma warning disable IDE0051 // Remove unused private members
    private bool PrintMembers(StringBuilder builder)
#pragma warning restore IDE0051 // Remove unused private members
    {
        Debug.Assert(builder != null);

        _ = builder.Append($"Version = {Version}, EvaluatedRules = {EvaluatedRules.ToStringElementWise()}, EvaluationIssues = {EvaluationIssues.ToStringElementWise()}");

        return true;
    }

    /// <inheritdoc />
    [DebuggerStepThrough]
    public bool Equals(EvaluationDebugData? other) =>
        ReferenceEquals(this, other) ||
        other != null &&
        Version == other.Version &&
        EvaluatedRules.EqualsElementWise(other.EvaluatedRules) &&
        EvaluationIssues.EqualsElementWise(other.EvaluationIssues);

    /// <inheritdoc />
    [DebuggerStepThrough]
    public override int GetHashCode() =>
        HashCode.Combine(Version, EvaluatedRules.GetHashCodeElementWise(), EvaluationIssues.GetHashCodeElementWise());

}
