namespace Bit.Bonboarding;

/// <summary>What a checklist item's callbacks are given.</summary>
public sealed class BonboardingChecklistItemContext(
    BonboardingChecklistDefinition checklist,
    BonboardingChecklistItem item,
    IServiceProvider services,
    CancellationToken cancellationToken = default)
{
    public BonboardingChecklistDefinition Checklist { get; } = checklist;

    public BonboardingChecklistItem Item { get; } = item;

    /// <summary>The application's service provider.</summary>
    public IServiceProvider Services { get; } = services;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}
