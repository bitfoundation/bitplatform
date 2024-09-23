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
            Description = "The content of the element.",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node. The default is \"div\".",
        }
    ];



    private int counter;

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
<BitElement>This is default (div)</BitElement>";

    private string example2RazorCode = @"
<BitElement Element=""input"" placeholder=""Input"" />
<BitElement Element=""a"" href=""https://bitplatform.dev/"" target=""_blank"">Anchor (Link)</BitElement>
<BitElement Element=""button"" @onclick=""@(() => counter++)"">Button (Click count @counter)</BitElement>";
    private string example2CsharpCode = @"
private int counter;";

    private string example3RazorCode = @"
<BitElement Element=""@element""
            placeholder=""@element""
            target=""_blank""
            href=""https://bitplatform.dev/"">
    @element
</BitElement>

<BitDropdown Label=""Elements"" Items=""elementsList"" @bind-Value=""element"" />";
    private string example3CsharpCode = @"
private string element = ""div"";
private List<BitDropdownItem<string>> elementsList = new()
{
        new() { Text = ""div"", Value = ""div"" },
        new() { Text = ""a"", Value = ""a"" },
        new() { Text = ""input"", Value = ""input"" },
        new() { Text = ""button"", Value = ""button"" },
        new() { Text = ""textarea"", Value = ""textarea"" },
        new() { Text = ""progress"", Value = ""progress"" }
};";
}
