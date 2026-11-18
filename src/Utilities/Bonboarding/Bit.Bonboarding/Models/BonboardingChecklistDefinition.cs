namespace Bit.Bonboarding;

/// <summary>
/// An onboarding checklist: the handful of things a new user should do, each ticked off as it gets done - by finishing
/// a tour, by a condition becoming true, or by the application saying so. Render it with <see cref="BonboardingChecklist"/>.
/// </summary>
public sealed class BonboardingChecklistDefinition
{
    /// <summary>The prefix of the key a checklist's progress is stored under, next to the tours' records.</summary>
    public const string StorageKeyPrefix = "checklist:";

    /// <summary>Stable identifier; with <see cref="StorageKeyPrefix"/>, the persistence key.</summary>
    public required string Id { get; set; }

    /// <summary>Heading of the checklist.</summary>
    public string? Title { get; set; }

    /// <summary>A line under the heading.</summary>
    public string? Description { get; set; }

    /// <summary>The tasks, in the order they are listed.</summary>
    public List<BonboardingChecklistItem> Items { get; init; } = [];

    /// <summary>Finds an item by id.</summary>
    public BonboardingChecklistItem? FindItem(string itemId) => Items.FirstOrDefault(item => item.Id == itemId);

    /// <summary>
    /// Checks the definition for mistakes: blank or duplicate item ids, a <see cref="BonboardingChecklistItem.DependsOn"/>
    /// naming an item that does not exist, and items that wait for each other in a circle (so none of them can ever be
    /// started). <see cref="BonboardingChecklist"/> logs what it finds; call it from a unit test to catch it first.
    /// <see cref="BonboardingValidationIssue.StepId"/> is the item's id.
    /// </summary>
    public IReadOnlyList<BonboardingValidationIssue> Validate() => BonboardingChecklistValidator.Validate(this);

    public override string ToString() => $"Checklist({Id}, {Items.Count} items)";
}
