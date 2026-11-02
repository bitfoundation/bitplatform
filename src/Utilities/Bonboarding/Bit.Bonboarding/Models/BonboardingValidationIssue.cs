namespace Bit.Bonboarding;

/// <summary>How serious a <see cref="BonboardingValidationIssue"/> is.</summary>
public enum BonboardingValidationSeverity
{
    /// <summary>The tour runs, but probably not the way its author meant. Logged when the tour starts.</summary>
    Warning,

    /// <summary>The tour cannot run correctly; <c>StartAsync</c> refuses it with a <see cref="BonboardingException"/>.</summary>
    Error,
}

/// <summary>One problem <see cref="BonboardingTourDefinition.Validate"/> found in a tour definition.</summary>
/// <param name="Severity">Whether the tour can still run.</param>
/// <param name="Code">A stable identifier for the kind of problem, e.g. <c>"duplicate-step-id"</c>, for tests and tooling.</param>
/// <param name="Message">What is wrong and how to fix it, naming the tour and the step.</param>
/// <param name="StepId">The step the problem is in, or <see langword="null"/> for the tour itself.</param>
public sealed record BonboardingValidationIssue(BonboardingValidationSeverity Severity, string Code, string Message, string? StepId = null)
{
    public override string ToString() => $"{Severity} {Code}: {Message}";
}
