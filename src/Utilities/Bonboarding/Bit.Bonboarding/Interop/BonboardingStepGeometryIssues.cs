namespace Bit.Bonboarding;

internal static class BonboardingStepGeometryIssues
{
    public static BonboardingTargetIssue Parse(string? issue) => issue switch
    {
        null or "" or "none" => BonboardingTargetIssue.NotFound,
        "hidden" => BonboardingTargetIssue.Hidden,
        "invalid-selector" => BonboardingTargetIssue.InvalidSelector,
        _ => BonboardingTargetIssue.NotFound,
    };
}
