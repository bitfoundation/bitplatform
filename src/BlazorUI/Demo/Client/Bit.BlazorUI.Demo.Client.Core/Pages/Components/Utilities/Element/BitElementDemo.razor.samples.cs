namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Element;

public partial class BitElementDemo
{
    private readonly string example1RazorCode = @"
<BitElement>The default element (div)</BitElement>
<BitElement Element=""h4"">A heading (h4)</BitElement>
<BitElement Element=""p"">A paragraph (p) with a <BitElement Element=""mark"">highlighted (mark)</BitElement> word.</BitElement>
<BitElement Element=""blockquote"">A quotation (blockquote)</BitElement>
<BitElement Element=""not a tag name"">An invalid tag name falls back to a div.</BitElement>";

    private readonly string example2RazorCode = @"
<BitElement Element=""a"" href=""https://bitplatform.dev/"" target=""_blank"" rel=""noopener"">An anchor to bitplatform.dev</BitElement>
<BitElement Element=""button"" data-demo=""counter"" @onclick=""() => counter++"">Clicked @counter times</BitElement>
<BitElement Element=""input"" AriaLabel=""Your text"" placeholder=""Type something"" aria-describedby=""typed-output"" @oninput=""e => typed = e.Value?.ToString()"" />
<BitElement id=""typed-output"" role=""status"">You typed: @typed</BitElement>";
    private readonly string example2CsharpCode = @"
private int counter;
private string? typed;";

    private readonly string example3RazorCode = @"
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

    private readonly string example4RazorCode = @"
<BitElement Element=""input"" placeholder=""An input"" AriaLabel=""An input"" />
<BitElement Element=""hr"" />
<BitElement Element=""img"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" alt=""bit logo"" width=""64"" />
<BitElement Element=""br"">Not rendered: a br holds no content.</BitElement>";

    private readonly string example5RazorCode = @"
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
                StopPropagationEvents=""@([""dblclick""])""
                PreventDefaultEvents=""@([""contextmenu""])""
                @ondblclick=""() => inner++""
                @oncontextmenu=""() => inner++"">
        Double-click stays here, right-click opens no browser menu (@inner)
    </BitElement>
</div>";
    private readonly string example5CsharpCode = @"
private int card;
private int inner;
private int doubled;
private int prevented;";

    private readonly string example6RazorCode = @"
<BitDropdown Label=""Element"" Items=""elementsList"" @bind-Value=""element"" Style=""width: 8rem;"" />
<BitElement Element=""@element""
            placeholder=""@element""
            target=""_blank""
            href=""https://bitplatform.dev/"">
    @element
</BitElement>";
    private readonly string example6CsharpCode = @"
private string element = ""div"";
private readonly List<BitDropdownItem<string>> elementsList =
[
    new() { Text = ""div"", Value = ""div"" },
    new() { Text = ""a"", Value = ""a"" },
    new() { Text = ""input"", Value = ""input"" },
    new() { Text = ""button"", Value = ""button"" },
    new() { Text = ""textarea"", Value = ""textarea"" },
    new() { Text = ""progress"", Value = ""progress"" }
];";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""wrapped"" Text=""Wrap the content"" />
<BitElement Element=""mark"" NoWrapper=""@(wrapped is false)"">The same content, highlighted or bare.</BitElement>";
    private readonly string example7CsharpCode = @"
private bool wrapped = true;";

    private readonly string example8RazorCode = @"
<BitElement Element=""input"" @ref=""inputElement"" placeholder=""Focused by the button"" AriaLabel=""Focus target"" />
<BitElement class=""demo-boxed"" TabIndex=""0"" @ref=""boxElement"">A div, focusable because it has a TabIndex.</BitElement>
<BitButton OnClick=""FocusTheInput"">Focus the input</BitButton>
<BitButton OnClick=""FocusTheBox"">Focus the div without scrolling</BitButton>";
    private readonly string example8CsharpCode = @"
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

    private readonly string example9RazorCode = @"
<BitElement Element=""button"" IsEnabled=""false"">A disabled button</BitElement>
<BitElement Element=""input"" IsEnabled=""false"" placeholder=""A disabled input"" AriaLabel=""A disabled input"" />
<BitElement Element=""a"" href=""https://bitplatform.dev/"" IsEnabled=""false"">A disabled anchor</BitElement>
<BitElement class=""demo-boxed"" TabIndex=""0"" role=""button"" IsEnabled=""false"" @onclick=""() => disabledClicks++"">A disabled div button, out of the tab order (@disabledClicks)</BitElement>";
    private readonly string example9CsharpCode = @"
private int disabledClicks;";

    private readonly string example10RazorCode = @"
<BitToggle @bind-Value=""isVisible"" Text=""Visible"" />
<BitElement class=""demo-boxed"" Visibility=""@(isVisible ? BitVisibility.Visible : BitVisibility.Hidden)"">Hidden keeps its space.</BitElement>
<BitElement class=""demo-boxed"" Visibility=""@(isVisible ? BitVisibility.Visible : BitVisibility.Collapsed)"">Collapsed takes its space with it.</BitElement>
<BitElement NoWrapper Visibility=""@(isVisible ? BitVisibility.Visible : BitVisibility.Collapsed)"">Unwrapped content is dropped while collapsed.</BitElement>";
    private readonly string example10CsharpCode = @"
private bool isVisible = true;";

    private readonly string example11RazorCode = @"
<div class=""demo-card"" @onclick=""() => toolbarCard++"">
    The card was clicked @toolbarCard times.
    <BitParams Parameters=""@elementParams"">
        <BitElement @onclick=""() => toolbar++"">Edit (@toolbar)</BitElement>
        <BitElement @onclick=""() => toolbar++"">Share (@toolbar)</BitElement>
        <BitElement StopPropagation=""false"" @onclick=""() => toolbar++"">Bubbles up (@toolbar)</BitElement>
    </BitParams>
</div>
<BitElement>Outside the cascade: a plain div again.</BitElement>";
    private readonly string example11CsharpCode = @"
private int toolbar;
private int toolbarCard;

private readonly BitElementParams[] elementParams =
[
    new()
    {
        Element = ""button"",
        StopPropagation = true,
    }
];";

    private readonly string example12RazorCode = @"
<BitElement Style=""color: tomato; font-weight: bold;"">Styled through the Style parameter</BitElement>
<BitElement Class=""demo-boxed"">Classed through the Class parameter</BitElement>
<BitElement Class=""demo-boxed"" style=""color: mediumseagreen;"">A Class parameter and a plain style attribute</BitElement>

<BitElement Element=""button"" IsEnabled=""false"" Style=""--bit-Element-disabled-opacity: 0.2;"">Disabled, dimmed further</BitElement>
<BitElement Element=""fieldset"" IsEnabled=""false"" Style=""--bit-Element-disabled-opacity: 1;"">
    <BitElement Element=""legend"">A disabled fieldset, not dimmed</BitElement>
    <BitElement Element=""input"" placeholder=""Its input shows its own disabled look"" AriaLabel=""Fieldset input"" />
</BitElement>";

    private readonly string example13RazorCode = @"
<BitElement Dir=""BitDir.Rtl"">این یک المنت راست‌چین است.</BitElement>
<BitElement Element=""blockquote"" Dir=""BitDir.Rtl"">یک نقل قول راست‌چین.</BitElement>";
}
