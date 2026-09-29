namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Element;

public partial class BitElementDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the element. Not rendered into a void element (input, img, br, hr, ...).",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tag to render, used as written (SVG and custom elements included). A value that is not a valid tag name - an ASCII letter followed by letters, digits, \"-\", \"_\", \".\" or \":\" - falls back to the default \"div\".",
        },
        new()
        {
            Name = "NoWrapper",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders only the content, without the tag. Everything describing the tag is ignored, except a Collapsed Visibility, which drops the content too.",
        },
        new()
        {
            Name = "PreventDefault",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the default browser action of the click (@onclick:preventDefault).",
        },
        new()
        {
            Name = "PreventDefaultEvents",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The events whose default browser action is prevented, named with or without the \"on\" prefix. Naming \"click\" wins over PreventDefault.",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the click from bubbling up to the ancestors (@onclick:stopPropagation).",
        },
        new()
        {
            Name = "StopPropagationEvents",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The events stopped from bubbling up, named with or without the \"on\" prefix. Naming \"click\" wins over StopPropagation.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Focuses the rendered element; the overload taking preventScroll focuses it without scrolling it into view. The element must be focusable (a focusable tag or one with a TabIndex). Does nothing while NoWrapper is set or before the first render.",
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Element-disabled-opacity",
            DefaultValue = "--bit-opa-dis",
            Description = "Opacity of a disabled element. Set it to 1 to keep the tag's own look, for a wrapper whose content shows its own disabled state.",
        }
    ];



    private int card;
    private int inner;
    private int counter;
    private int doubled;
    private int toolbar;
    private int prevented;
    private int toolbarCard;
    private string? typed;
    private bool wrapped = true;
    private bool isVisible = true;

    private BitElement? boxElement;
    private BitElement? inputElement;
    private async Task FocusTheInput()
    {
        if (inputElement is null) return;

        await inputElement.FocusAsync();
    }
    private async Task FocusTheBox()
    {
        if (boxElement is null) return;

        await boxElement.FocusAsync(preventScroll: true);
    }

    private string element = "div";
    private readonly List<BitDropdownItem<string>> elementsList =
    [
         new() { Text = "div", Value = "div" },
         new() { Text = "a", Value = "a" },
         new() { Text = "input", Value = "input" },
         new() { Text = "button", Value = "button" },
         new() { Text = "textarea", Value = "textarea" },
         new() { Text = "progress", Value = "progress" }
    ];

    private readonly BitElementParams[] elementParams =
    [
        new()
        {
            Element = "button",
            StopPropagation = true,
        }
    ];
}
