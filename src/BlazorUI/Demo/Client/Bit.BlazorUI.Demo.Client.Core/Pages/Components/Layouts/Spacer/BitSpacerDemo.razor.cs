namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Spacer;

public partial class BitSpacerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the custom html element used for the root node, \"div\" by default. This is what lets a spacer sit in a container that only accepts specific children, such as an \"li\" inside a list-based toolbar or menu, without breaking the markup of that container.",
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the fixed amount of space the spacer generates, using any CSS length value (for example \"1rem\", \"5%\", or \"var(--my-gap)\"). The space is created along the inline (horizontal) axis, or along the block (vertical) axis when Vertical is set. It takes precedence over Width, Height and Size on the axis it targets, and like them it turns the spacer from a flexible one into a fixed one, so Grow and MinGap no longer apply.",
        },
        new()
        {
            Name = "Grow",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets how much of the leftover space of the container this spacer takes compared to its siblings (the CSS flex-grow factor), 1 by default. Two spacers with the factors \"1\" and \"2\" split the leftover space of the container one third to two thirds. It is ignored as soon as the spacer is given a fixed size through Gap, Width, Height or Size.",
        },
        new()
        {
            Name = "Height",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the fixed amount of space the spacer generates along the block (vertical) axis, in pixels. This is the parameter to reach for inside a vertical (column) layout, where the inline-axis Width creates no visible space. Setting it turns the spacer from a flexible one into a fixed one, so Grow and MinGap no longer apply.",
        },
        new()
        {
            Name = "MinGap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the smallest amount of space a flexible spacer keeps when the container runs out of room, using any CSS length value. It applies to the inline (horizontal) axis, or to the block (vertical) axis when Vertical is set. It only concerns a flexible spacer, so it is ignored as soon as the spacer is given a fixed size through Gap, Width, Height or Size.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Gets or sets the fixed amount of space the spacer generates, picked from the spacing scale of the theme, so the resulting length follows the spacing unit and the density of the current theme instead of being hardcoded. It is created along the inline (horizontal) axis, or along the block (vertical) axis when Vertical is set, and like the other fixed sizes it turns the spacer from a flexible one into a fixed one, so Grow and MinGap no longer apply. Gap, Width and Height are more specific, so they take precedence over it.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gets or sets a value indicating whether Gap, Size and MinGap apply to the block (vertical) axis instead of the inline (horizontal) one. A flexible spacer does not need this parameter: it always grows along the main axis of its container, whichever that is.",
        },
        new()
        {
            Name = "Width",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the fixed amount of space the spacer generates along the inline (horizontal) axis, in pixels. The space follows the text direction of the spacer, so in a right-to-left context it is created on the right side. Setting it turns the spacer from a flexible one into a fixed one, so Grow and MinGap no longer apply.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the fixed amount of space the spacer generates, picked from the spacing scale of the theme.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" },
            ]
        },
    ];
}
