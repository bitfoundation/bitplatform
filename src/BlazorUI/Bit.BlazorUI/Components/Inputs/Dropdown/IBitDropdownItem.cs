namespace Bit.BlazorUI;

// A BitDropdownItem<T> of any T, so that a dropdown without NameSelectors reads its built-in items whatever their T: an
// id is rarely nullable while the property a dropdown is bound to often is, and the BitDropdownItem<Guid> of a dropdown
// of Guid? then works as the BitDropdownItem<Guid?> it stands for (see BitItemValueType). Every public property of
// BitDropdownItem<T> has its member here (BitDropdownItemValueTypeTests pins that), the value as the two methods below.
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

    bool IsSelected { get; set; }

    // The value as the TValue of the dropdown (a Guid as a Guid?), or the default of it when the value is none.
    T? GetValue<T>();

    // Sets the value a dropdown gives a new item of the Dynamic mode, or returns false when the item cannot hold it: the
    // null of a Guid? is no value of a BitDropdownItem<Guid>.
    bool TrySetValue<T>(T? value);
}
