namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Params;

public partial class BitParamsDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to which the values should be provided.",
        },
        new()
        {
            Name = "Isolated",
            Type = "bool",
            DefaultValue = "false",
            Description = "Ignores the params objects the BitParams ancestors provide, so the components below take their defaults from this one alone.",
        },
        new()
        {
            Name = "Parameters",
            Type = "IEnumerable<IBitComponentParams>?",
            DefaultValue = "null",
            Description = "The params objects to provide, one per component type (BitButtonParams, BitTagParams, ...). A later or nested object of the same type only replaces the parameters it sets.",
            LinkType = LinkType.Link,
            Href = "#component-params",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "component-params",
            Title = "IBitComponentParams",
            Description = "Implemented by every <Component>Params class; its Name is what the component reads it by.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The cascading name the component reads the params object by (its ParamName constant).",
                }
            ]
        },
        new()
        {
            Id = "bit-component-base-params",
            Title = "BitComponentBaseParams",
            Description = "The base class of every <Component>Params class: the parameters every component inherits from BitComponentBase. A null value is left unset.",
            Parameters =
            [
                new()
                {
                    Name = "AriaLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the accessible label for the component, used by assistive technologies. Every component that reads it is announced by the same name, so share it only between components that do the same thing.",
                },
                new()
                {
                    Name = "Class",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS class name(s) to apply to the rendered element.",
                },
                new()
                {
                    Name = "Dir",
                    Type = "BitDir?",
                    DefaultValue = "null",
                    Description = "Gets or sets the text directionality for the component's content.",
                },
                new()
                {
                    Name = "ForceAnimation",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Gets or sets a value indicating whether the component's animations play at their full duration even when reduced motion is requested.",
                },
                new()
                {
                    Name = "HtmlAttributes",
                    Type = "Dictionary<string, object>?",
                    DefaultValue = "null",
                    Description = "Additional HTML attributes for the root element. A nested object of the same type adds its entries to the outer ones.",
                },
                new()
                {
                    Name = "Id",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the unique identifier for the component's root element. Every component that reads it gets the same id, so share it only with a single component.",
                },
                new()
                {
                    Name = "Disabled",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Gets or sets a value indicating whether the component is disabled and cannot respond to user interaction.",
                },
                new()
                {
                    Name = "Style",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS style string to apply to the rendered element.",
                },
                new()
                {
                    Name = "TabIndex",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the tab order index for the component when navigating with the keyboard.",
                },
                new()
                {
                    Name = "Visibility",
                    Type = "BitVisibility?",
                    DefaultValue = "null",
                    Description = "Gets or sets the visibility state (visible, hidden, or collapsed) of the component.",
                }
            ]
        },
        new()
        {
            Id = "bit-input-base-params",
            Title = "BitInputBaseParams<TValue>",
            Description = "The base class of the params of an input: BitComponentBaseParams plus the parameters every input inherits from BitInputBase. What identifies a single field (Value, Name, DisplayName) is left out.",
            Parameters =
            [
                new()
                {
                    Name = "ReadOnly",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Makes the input read-only.",
                },
                new()
                {
                    Name = "Required",
                    Type = "bool?",
                    DefaultValue = "null",
                    Description = "Makes the input required.",
                }
            ]
        },
    ];



    private readonly List<IBitComponentParams> basicParams =
    [
        new BitButtonParams { Variant = BitVariant.Outline, Color = BitColor.Tertiary, Size = BitSize.Small },
        new BitTagParams { Variant = BitVariant.Fill, Color = BitColor.Tertiary, Size = BitSize.Small },
    ];

    private bool isCompact = true;

    private readonly List<IBitComponentParams> outerParams =
    [
        new BitButtonParams { Variant = BitVariant.Outline, Size = BitSize.Small },
    ];

    private readonly List<IBitComponentParams> dangerParams =
    [
        new BitButtonParams { Color = BitColor.Error },
    ];
}
