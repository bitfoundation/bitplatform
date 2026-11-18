namespace Bit.Bheadless;

/// <summary>An item that participates in a <see cref="BitBhCollection{TItem}"/>.</summary>
public interface IBitBhCollectionItem
{
    /// <summary>The item's DOM id (stable for the item's lifetime). Used for <c>aria-activedescendant</c> and focus.</summary>
    string ItemId { get; }

    /// <summary>Whether the item is disabled and should be skipped by keyboard navigation.</summary>
    bool IsItemDisabled { get; }

    /// <summary>Plain text used for typeahead and announcements.</summary>
    string? ItemText { get; }
}
