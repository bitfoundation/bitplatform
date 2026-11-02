namespace Bit.BlazorUI;

// A BitChoiceGroupItem<T> of any T, so that a ChoiceGroup without NameSelectors reads its built-in items whatever their
// T: an id is rarely nullable while the property a ChoiceGroup is bound to often is, and the BitChoiceGroupItem<Guid> of
// a ChoiceGroup of Guid? then works as the BitChoiceGroupItem<Guid?> it stands for (see BitItemValueType). Every public
// property of BitChoiceGroupItem<T> has its member here (BitChoiceGroupItemValueTypeTests pins that), the template and
// the value as the two methods below.
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

    string? Text { get; }

    string? Title { get; }

    int Index { get; set; }

    bool IsSelected { get; set; }

    // The template as one of the TItem of the ChoiceGroup, which is the item's own type or a type derived from it.
    RenderFragment<TItem>? GetTemplate<TItem>();

    // The value as the TValue of the ChoiceGroup (a Guid as a Guid?), or the default of it when the value is none.
    T? GetValue<T>();
}
