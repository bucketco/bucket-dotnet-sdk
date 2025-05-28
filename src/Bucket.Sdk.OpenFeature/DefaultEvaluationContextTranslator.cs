namespace Bucket.Sdk;

using System.Diagnostics;

/// <summary>
///     Default implementation of <see cref="EvaluationContextTranslatorDelegate" />.
/// </summary>
public static class DefaultEvaluationContextTranslator
{
    private const string _targetingKey = "targetingKey";
    private const string _userIdKey = "userId";
    private const string _userNameKey = "name";
    private const string _userEmailKey = "email";
    private const string _userAvatarKey = "avatar";
    private const string _companyIdKey = "companyId";
    private const string _companyNameKey = "companyName";
    private const string _companyAvatarKey = "companyAvatar";

    private static readonly HashSet<string> _standardKeys =
    [
        _targetingKey,
        _userIdKey,
        _userNameKey,
        _userEmailKey,
        _userAvatarKey,
        _companyIdKey,
        _companyNameKey,
        _companyAvatarKey,
    ];

    /// <summary>
    ///     Translates an OpenFeature evaluation context to a Bucket context.
    /// </summary>
    /// <param name="evaluationContext">The evaluation context to translate.</param>
    /// <returns>The translated context.</returns>
    public static Context Translate(EvaluationContext? evaluationContext)
    {
        if (evaluationContext != null)
        {
            var userId =
                evaluationContext.TargetingKey ??
                evaluationContext.GetStringOrNull(_userIdKey);

            _ = Uri.TryCreate(
                evaluationContext.GetStringOrNull(_userAvatarKey), UriKind.Absolute,
                out var userAvatar
            );

            var user = !string.IsNullOrEmpty(userId)
                ? new User(userId)
                {
                    Name = evaluationContext.GetStringOrNull(_userNameKey),
                    Email = evaluationContext.GetStringOrNull(_userEmailKey),
                    Avatar = userAvatar,
                }
                : null;

            var companyId =
                evaluationContext.GetStringOrNull(_companyIdKey) ??
                evaluationContext.GetStringOrNull(_companyIdKey);

            _ = Uri.TryCreate(
                evaluationContext.GetStringOrNull(_companyAvatarKey),
                UriKind.Absolute,
                out var companyAvatar
            );

            var company = !string.IsNullOrEmpty(companyId)
                ? new Company(companyId)
                {
                    Name = evaluationContext.GetStringOrNull(_companyNameKey),
                    Avatar = companyAvatar,
                }
                : null;

            var context = new Context { User = user, Company = company };
            foreach (var (key, value) in evaluationContext)
            {
                if (!_standardKeys.Contains(key))
                {
                    ExpandValue(context, key, value);
                }
            }

            return context;
        }

        return [];
    }

    /// <summary>
    ///     Expands a value into an entity.
    /// </summary>
    /// <param name="entity">The entity to expand the value into.</param>
    /// <param name="name">The name of the value to expand.</param>
    /// <param name="value">The value to expand.</param>
    internal static void ExpandValue(EntityBase entity, string name, Value value)
    {
        Debug.Assert(entity != null);
        Debug.Assert(!string.IsNullOrEmpty(name));

        var structure = value.AsStructure;
        if (structure == null)
        {
            entity.Add(name, value.AsObject);
        }
        else
        {
            foreach (var (p, v) in structure)
            {
                ExpandValue(entity, $"{name}.{p}", v);
            }
        }
    }

    private static string? GetStringOrNull(this EvaluationContext evaluationContext, string key)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));

        return evaluationContext.TryGetValue(key, out var value) ? value?.AsString : null;
    }
}
