namespace Bucket.Sdk;

/// <summary>
///     Represents a compiled filter predicate.
/// </summary>
/// <param name="contextFields">The context fields used in the evaluation.</param>
/// <param name="issues">The issues encountered during the evaluation (output).</param>
/// <returns><c>true</c> if the predicate is satisfied; otherwise, <c>false</c>.</returns>
internal delegate bool CompiledFilterPredicate(
    IReadOnlyDictionary<string, object?> contextFields,
    IList<(EvaluationIssueType issue, string name)> issues
);
