namespace Bit.BlazorUI;

// A BitDropdownItem<T> of any T, so that a dropdown reads its built-in items whatever their T: an id is rarely
// nullable while the property a dropdown is bound to often is, and the BitDropdownItem<Guid> of a dropdown of Guid?
// then works as the BitDropdownItem<Guid?> it stands for (see BitItemValueType).
internal interface IBitDropdownItem
{
    string? AriaLabel { get; }

    string? Class { get; }

    BitIconInfo? Icon { get; }

    string? IconName { get; }

    string? Id { get; }

    object? Data { get; }

    bool IsDisabled { get; }

    bool IsHidden { get; }

    BitDropdownItemType ItemType { get; }

    string? Style { get; }

    string? Text { get; set; }

    string? Title { get; set; }

    // Boxed, so that the dropdown reads it as the TValue it is (a Guid as a Guid?). A value that is not one of the
    // item's own T - the null of a Guid? given to a BitDropdownItem<Guid> - is set as the default of that T.
    object? Value { get; set; }

    bool IsSelected { get; set; }
}
