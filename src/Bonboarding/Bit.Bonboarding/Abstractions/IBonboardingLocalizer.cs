namespace Bit.Bonboarding;

/// <summary>
/// Supplies the user-visible strings. The default implementation uses a tour's
/// <see cref="BonboardingTourDefinition.Labels"/> when it has any, and
/// <see cref="BonboardingOptions.Labels"/> otherwise; the two are not merged, so a tour-level set
/// replaces the global one entirely (start from <see cref="BonboardingLabels.Clone"/> to override a
/// few strings). Replace it to source strings from <c>IStringLocalizer</c> or a translation service.
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
