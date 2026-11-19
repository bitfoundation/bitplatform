namespace Bit.Bonboarding;

/// <summary>How serious a <see cref="BonboardingValidationIssue"/> is.</summary>
public enum BonboardingValidationSeverity
{
    /// <summary>The tour runs, but probably not the way its author meant. Logged when the tour starts.</summary>
    Warning,

    /// <summary>The tour cannot run correctly; <c>StartAsync</c> refuses it with a <see cref="BonboardingException"/>.</summary>
    Error,
}
