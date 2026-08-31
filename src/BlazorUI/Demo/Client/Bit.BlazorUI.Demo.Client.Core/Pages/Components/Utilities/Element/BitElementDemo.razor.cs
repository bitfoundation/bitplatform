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
            Description = "The custom html element used for the root node. Any tag name is accepted, including SVG and custom elements, and it is used exactly as written. The default is \"div\".",
        },
        new()
        {
            Name = "NoWrapper",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders only the content of the element, without the wrapping HTML tag, which makes the component a conditional wrapper. Everything that describes the element itself is then ignored.",
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
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the click event of the element from bubbling up to its ancestors, which is the @onclick:stopPropagation directive Razor only accepts on a plain HTML element.",
        }
    ];



    private int card;
    private int inner;
    private int counter;
    private int prevented;
    private string? typed;
    private bool wrapped = true;

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
<BitElement Element=""code"">A code span (code)</BitElement>";

    private string example3RazorCode = @"
<BitElement Element=""input"" placeholder=""An input"" />
<BitElement Element=""hr"" />
<BitElement Element=""img"" src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" alt=""bit logo"" width=""64"" />
<BitElement Element=""br"">This content is not rendered, because a br holds none.</BitElement>";

    private string example4RazorCode = @"
<BitElement Element=""a"" href=""https://bitplatform.dev/"" target=""_blank"" rel=""noopener"">An anchor to bitplatform.dev</BitElement>
<BitElement Element=""button"" data-demo=""counter"" @onclick=""() => counter++"">Clicked @counter times</BitElement>
<BitElement Element=""input"" placeholder=""Type something"" @oninput=""e => typed = e.Value?.ToString()"" />
<BitElement>You typed: @typed</BitElement>";
    private string example4CsharpCode = @"
private int counter;
private string? typed;";

    private string example5RazorCode = @"
<div class=""demo-card"" @onclick=""() => card++"">
    The card was clicked @card times.
    <BitElement Element=""button"" StopPropagation @onclick=""() => inner++"">Stops propagation (@inner)</BitElement>
    <BitElement Element=""button"" @onclick=""() => inner++"">Bubbles up (@inner)</BitElement>
</div>

<BitElement Element=""a"" href=""https://bitplatform.dev/"" PreventDefault @onclick=""() => prevented++"">
    An anchor that does not navigate (@prevented)
</BitElement>";
    private string example5CsharpCode = @"
private int card;
private int inner;
private int prevented;";

    private string example6RazorCode = @"
<BitElement Element=""button"" IsEnabled=""false"" @onclick=""() => counter++"">A disabled button</BitElement>
<BitElement Element=""input"" IsEnabled=""false"" placeholder=""A disabled input"" />
<BitElement Element=""a"" href=""https://bitplatform.dev/"" IsEnabled=""false"">A disabled anchor</BitElement>";

    private string example7RazorCode = @"
<BitToggle @bind-Value=""wrapped"" Text=""Wrap the content"" />

<BitElement Element=""mark"" NoWrapper=""@(wrapped is false)"">The same content, highlighted or bare.</BitElement>";
    private string example7CsharpCode = @"
private bool wrapped = true;";

    private string example8RazorCode = @"
<BitDropdown Label=""Elements"" Items=""elementsList"" @bind-Value=""element"" />

<BitElement Element=""@element""
            placeholder=""@element""
            target=""_blank""
            href=""https://bitplatform.dev/"">
    @element
</BitElement>";
    private string example8CsharpCode = @"
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

    private string example9RazorCode = @"
<BitElement Style=""color: tomato; font-weight: bold;"">Styled through the Style parameter</BitElement>
<BitElement Class=""demo-boxed"">Classed through the Class parameter</BitElement>
<BitElement Class=""demo-boxed"" style=""color: mediumseagreen;"">Both a Class parameter and a splatted style</BitElement>";

    private string example10RazorCode = @"
<BitElement Dir=""BitDir.Rtl"">این یک المنت راست‌چین است.</BitElement>
<BitElement Element=""blockquote"" Dir=""BitDir.Rtl"">یک نقل قول راست‌چین.</BitElement>";
}
