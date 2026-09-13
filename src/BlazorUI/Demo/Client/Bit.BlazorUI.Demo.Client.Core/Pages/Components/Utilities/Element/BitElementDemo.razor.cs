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
            Description = "The content of the element. A void element (such as input, img, br or hr) holds no content, so it is not rendered into one.",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. Any tag name is accepted, including SVG and custom elements, and it is used exactly as written. A value that is not a name a tag can be made of - a letter followed by letters, digits and the \"-\", \"_\", \".\" and \":\" that join them - falls back to the default, which is \"div\".",
        },
        new()
        {
            Name = "NoWrapper",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders only the content of the element, without the wrapping HTML tag, which makes the component a conditional wrapper. Everything that describes the element itself is then ignored, apart from a Collapsed Visibility, which drops the content as well.",
        },
        new()
        {
            Name = "PreventDefault",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the default browser action of the click event of the element, which is the @onclick:preventDefault directive Razor only accepts on a plain HTML element.",
        },
        new()
        {
            Name = "PreventDefaultEvents",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The names of the events whose default browser action is prevented on the element, with or without the \"on\" prefix. This is PreventDefault for every event other than the click, and naming the click here has the last word over that parameter.",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the click event of the element from bubbling up to its ancestors, which is the @onclick:stopPropagation directive Razor only accepts on a plain HTML element.",
        },
        new()
        {
            Name = "StopPropagationEvents",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The names of the events that are stopped from bubbling up from the element to its ancestors, with or without the \"on\" prefix. This is StopPropagation for every event other than the click, and naming the click here has the last word over that parameter.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives the browser focus to the rendered element, which has to be one the browser can focus: a tag that is focusable of itself, or any other tag carrying a TabIndex. The overload taking a preventScroll flag focuses it without the browser scrolling the document to bring it into view. Nothing is rendered while NoWrapper is set and nothing is captured before the first render, so there the call does nothing rather than fail.",
        }
    ];



    private int card;
    private int inner;
    private int counter;
    private int doubled;
    private int prevented;
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
    private List<BitDropdownItem<string>> elementsList =
    [
         new() { Text = "div", Value = "div" },
         new() { Text = "a", Value = "a" },
         new() { Text = "input", Value = "input" },
         new() { Text = "button", Value = "button" },
         new() { Text = "textarea", Value = "textarea" },
         new() { Text = "progress", Value = "progress" }
    ];



    private string example1RazorCode = @"
<BitElement>This is the default element (a div).</BitElement>";

    private string example2RazorCode = @"
<BitElement Element=""h4"">A heading (h4)</BitElement>
<BitElement Element=""p"">A paragraph (p) with a <BitElement Element=""mark"">highlighted (mark)</BitElement> word in it.</BitElement>
<BitElement Element=""blockquote"">A quotation (blockquote)</BitElement>
<BitElement Element=""code"">A code span (code)</BitElement>
<BitElement Element=""not a tag name"">A tag name carrying whitespace falls back to a div.</BitElement>
<BitElement Element=""h4!"">And so does one carrying a symbol no tag name is made of.</BitElement>";

    private string example3RazorCode = @"
<BitElement Element=""svg"" width=""160"" height=""48"" viewBox=""0 0 160 48"" role=""img"" AriaLabel=""A gradient bar"">
    <BitElement Element=""defs"">
        <BitElement Element=""linearGradient"" id=""demo-gradient"" x1=""0"" y1=""0"" x2=""1"" y2=""0"">
            <BitElement Element=""stop"" offset=""0%"" stop-color=""tomato"" />
            <BitElement Element=""stop"" offset=""100%"" stop-color=""mediumseagreen"" />
        </BitElement>
    </BitElement>
    <BitElement Element=""rect"" width=""160"" height=""48"" rx=""8"" fill=""url(#demo-gradient)"" />
</BitElement>

<BitElement Element=""demo-badge"">A custom element (demo-badge)</BitElement>";

    private string example4RazorCode = @"
<BitElement Element=""input"" placeholder=""An input"" />
<BitElement Element=""hr"" />
<BitElement Element=""img"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" alt=""bit logo"" width=""64"" />
<BitElement Element=""br"">This content is not rendered, because a br holds none.</BitElement>";

    private string example5RazorCode = @"
<BitElement Element=""a"" href=""https://bitplatform.dev/"" target=""_blank"" rel=""noopener"">An anchor to bitplatform.dev</BitElement>
<BitElement Element=""button"" data-demo=""counter"" @onclick=""() => counter++"">Clicked @counter times</BitElement>
<BitElement Element=""input"" placeholder=""Type something"" @oninput=""e => typed = e.Value?.ToString()"" />
<BitElement>You typed: @typed</BitElement>";
    private string example5CsharpCode = @"
private int counter;
private string? typed;";

    private string example6RazorCode = @"
<div class=""demo-card"" @onclick=""() => card++"">
    The card was clicked @card times.
    <BitElement Element=""button"" StopPropagation @onclick=""() => inner++"">Stops propagation (@inner)</BitElement>
    <BitElement Element=""button"" @onclick=""() => inner++"">Bubbles up (@inner)</BitElement>
</div>

<BitElement Element=""a"" href=""https://bitplatform.dev/"" PreventDefault @onclick=""() => prevented++"">
    An anchor that does not navigate (@prevented)
</BitElement>

<div class=""demo-card"" @ondblclick=""() => doubled++"">
    The card was double-clicked @doubled times.
    <BitElement class=""demo-boxed""
                StopPropagationEvents=""@(new[] { ""dblclick"" })""
                PreventDefaultEvents=""@(new[] { ""contextmenu"" })""
                @ondblclick=""() => inner++""
                @oncontextmenu=""() => inner++"">
        Double-click keeps the card out of it, right-click opens no browser menu (@inner)
    </BitElement>
</div>";
    private string example6CsharpCode = @"
private int card;
private int inner;
private int doubled;
private int prevented;";

    private string example7RazorCode = @"
<BitElement Element=""button"" IsEnabled=""false"" @onclick=""() => counter++"">A disabled button</BitElement>
<BitElement Element=""input"" IsEnabled=""false"" placeholder=""A disabled input"" />
<BitElement Element=""a"" href=""https://bitplatform.dev/"" IsEnabled=""false"">A disabled anchor</BitElement>";
    private string example7CsharpCode = @"
private int counter;";

    private string example8RazorCode = @"
<BitToggle @bind-Value=""isVisible"" Text=""Visible"" />

<BitElement class=""demo-boxed"" Visibility=""@(isVisible ? BitVisibility.Visible : BitVisibility.Hidden)"">Hidden keeps its space.</BitElement>
<BitElement class=""demo-boxed"" Visibility=""@(isVisible ? BitVisibility.Visible : BitVisibility.Collapsed)"">Collapsed takes its space with it.</BitElement>
<BitElement NoWrapper Visibility=""@(isVisible ? BitVisibility.Visible : BitVisibility.Collapsed)"">Even unwrapped content is dropped while collapsed.</BitElement>";
    private string example8CsharpCode = @"
private bool isVisible = true;";

    private string example9RazorCode = @"
<BitToggle @bind-Value=""wrapped"" Text=""Wrap the content"" />

<BitElement Element=""mark"" NoWrapper=""@(wrapped is false)"">The same content, highlighted or bare.</BitElement>";
    private string example9CsharpCode = @"
private bool wrapped = true;";

    private string example10RazorCode = @"
<BitDropdown Label=""Elements"" Items=""elementsList"" @bind-Value=""element"" Style=""width: 8rem;"" />

<BitElement Element=""@element""
            placeholder=""@element""
            target=""_blank""
            href=""https://bitplatform.dev/"">
    @element
</BitElement>";
    private string example10CsharpCode = @"
private string element = ""div"";
private List<BitDropdownItem<string>> elementsList =
[
    new() { Text = ""div"", Value = ""div"" },
    new() { Text = ""a"", Value = ""a"" },
    new() { Text = ""input"", Value = ""input"" },
    new() { Text = ""button"", Value = ""button"" },
    new() { Text = ""textarea"", Value = ""textarea"" },
    new() { Text = ""progress"", Value = ""progress"" }
];";

    private string example11RazorCode = @"
<BitElement Element=""input"" @ref=""inputElement"" placeholder=""Focused by the button"" />
<BitElement class=""demo-boxed"" TabIndex=""0"" @ref=""boxElement"">A div, focusable because it has a TabIndex.</BitElement>

<BitButton OnClick=""FocusTheInput"">Focus the input</BitButton>
<BitButton OnClick=""FocusTheBox"">Focus the div without scrolling</BitButton>";
    private string example11CsharpCode = @"
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
}";

    private string example12RazorCode = @"
<BitElement Style=""color: tomato; font-weight: bold;"">Styled through the Style parameter</BitElement>
<BitElement Class=""demo-boxed"">Classed through the Class parameter</BitElement>
<BitElement Class=""demo-boxed"" style=""color: mediumseagreen;"">Both a Class parameter and a splatted style</BitElement>";

    private string example13RazorCode = @"
<BitElement Dir=""BitDir.Rtl"">این یک المنت راست‌چین است.</BitElement>
<BitElement Element=""blockquote"" Dir=""BitDir.Rtl"">یک نقل قول راست‌چین.</BitElement>";
}
