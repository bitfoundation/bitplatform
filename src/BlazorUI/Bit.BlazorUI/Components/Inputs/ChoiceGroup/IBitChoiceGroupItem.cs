namespace Bit.BlazorUI;

// A BitChoiceGroupItem<T> of any T, so that a ChoiceGroup reads its built-in items whatever their T: an id is rarely
// nullable while the property a ChoiceGroup is bound to often is, and the BitChoiceGroupItem<Guid> of a ChoiceGroup of
// Guid? then works as the BitChoiceGroupItem<Guid?> it stands for (see BitItemValueType).
internal interface IBitChoiceGroupItem
{
    string? AriaLabel { get; }

    string? Class { get; }

    string? Description { get; }

    string? Id { get; }

    bool IsDisabled { get; }

    BitIconInfo? Icon { get; }

    string? IconName { get; }

    string? ImageSrc { get; }

    string? ImageAlt { get; }

    BitImageSize? ImageSize { get; }

    string? Prefix { get; }

    string? SelectedImageSrc { get; }

    string? Style { get; }

    string? Suffix { get; }

    // A RenderFragment<BitChoiceGroupItem<T>>, which is the RenderFragment<TItem> of the ChoiceGroup the item is in.
    object? Template { get; }

    string? Text { get; }

    string? Title { get; }

    // Boxed, so that the ChoiceGroup reads it as the TValue it is (a Guid as a Guid?).
    object? Value { get; }

    int Index { get; set; }

    bool IsSelected { get; set; }
}
