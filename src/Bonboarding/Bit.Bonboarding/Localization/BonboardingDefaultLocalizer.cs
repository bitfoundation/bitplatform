using Microsoft.Extensions.Options;

namespace Bit.Bonboarding;

/// <summary>
/// Resolves labels from <see cref="BonboardingTourDefinition.Labels"/> when present, otherwise from
/// <see cref="BonboardingOptions.Labels"/>.
/// </summary>
public sealed class BonboardingDefaultLocalizer(IOptions<BonboardingOptions> options) : IBonboardingLocalizer
{
    private readonly BonboardingOptions _options = options.Value;

    public BonboardingLabels GetLabels(BonboardingTourDefinition? tour)
        => tour?.Labels ?? _options.Labels;
}
