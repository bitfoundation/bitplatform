namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Container;

public partial class BitContainerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to be rendered inside the BitContainer.",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the custom html element used for the root node, \"div\" by default. A container that holds the main content of the page is more meaningful as a main, one that holds a region of it as a section or an article. Only the tag name changes, and with it the semantics reported to assistive technologies, so a landmark element given here is worth pairing with an AriaLabel that names it.",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the maximum width of the BitContainer, in which its content is then centered. Takes any CSS length (for example 1200px or 75rem), or none for a container that spans the whole width it is given and only keeps its gutters. The width includes the gutters. An explicit value wins over the --bit-Container-max-width variable; left unset, the container is 80rem (1280px, the large breakpoint) wide at most unless that variable says otherwise.",
        },
        new()
        {
            Name = "NoGutter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the inline paddings (the gutters) of the BitContainer, so its content can span its full width. It wins over the --bit-Container-padding-inline variable; the block padding is not a gutter, so it is left alone.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Container-max-width",
            DefaultValue = "80rem",
            Description = "Maximum width of the container, gutters included. The MaxWidth parameter wins over it. It inherits, so a value set on a container also reaches the containers nested in it; MaxWidth is the way to size one container alone.",
        },
        new()
        {
            Name = "--bit-Container-padding-inline",
            DefaultValue = "spacing(2), spacing(3) from sm, spacing(4) from md",
            Description = "The gutters on the two sides of the content. A value set here holds at every width (the steps are the default's alone), and NoGutter wins over it.",
        },
        new()
        {
            Name = "--bit-Container-padding-block",
            DefaultValue = "0",
            Description = "Room above and below the content.",
        },
    ];



    private readonly BitContainerParams[] containerParams =
    [
        new() { MaxWidth = "480px" }
    ];
}
