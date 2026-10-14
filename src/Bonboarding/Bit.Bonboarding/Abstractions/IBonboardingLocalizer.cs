namespace Bit.Bonboarding;

/// <summary>
/// Supplies the user-visible strings. The default implementation merges
/// <see cref="BonboardingOptions.Labels"/> with any <see cref="BonboardingTourDefinition.Labels"/>; replace it
/// to source strings from <c>IStringLocalizer</c> or a translation service.
/// </summary>
public interface IBonboardingLocalizer
{
    /// <summary>The label set in force for a tour.</summary>
    BonboardingLabels GetLabels(BonboardingTourDefinition? tour);

    /// <summary>
    /// Post-processes step text such as titles and descriptions. The default implementation
    /// returns the value unchanged, so authors can treat these as literals or as resource keys as
    /// they prefer.
    /// </summary>
    string? Localize(string? text) => text;
}
