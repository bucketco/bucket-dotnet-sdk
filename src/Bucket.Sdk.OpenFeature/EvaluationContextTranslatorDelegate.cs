namespace Bucket.Sdk;

/// <summary>
///     Used to translate an OpenFeature evaluation context to a Bucket context.
/// </summary>
/// <param name="context">The context used for evaluation.</param>
/// <returns>A Bucket context.</returns>
[PublicAPI]
public delegate Context EvaluationContextTranslatorDelegate(
    EvaluationContext? context
);
