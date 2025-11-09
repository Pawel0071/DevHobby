using RPG.AI.Core;

namespace RPG.AI.Utility;

/// <summary>
///     Evaluates the suitability of an action for the given context.
///     Implementations return values between 0 (never) and 1 (always).
/// </summary>
public interface IUtilityConsideration
{
    float Evaluate(AiContext context);

    string Name { get; }
}
