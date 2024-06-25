namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Element;

public partial class BitElementDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
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
    };



    private int counter;

    private string elementTag = "div";
    private List<BitDropdownItem<string>> tagsList = new()
    {
         new() { Text = "div", Value = "div" },
         new() { Text = "a", Value = "a" },
         new() { Text = "input", Value = "input" },
         new() { Text = "button", Value = "button" },
         new() { Text = "textarea", Value = "textarea" },
         new() { Text = "progress", Value = "progress" }
    };



    private string example1RazorCode = @"
<BitElement>This is default (div)</BitElement>";

    private string example2RazorCode = @"
<BitElement Tag=""input"" placeholder=""Input"" />
<BitElement Tag=""a"" href=""https://bitplatform.dev/"" target=""_blank"">Anchor (Link)</BitElement>
<BitElement Tag=""button"" @onclick=""@(() => counter++)"">Button (Click count @counter)</BitElement>";
    private string example2CsharpCode = @"
private int counter;";

    private string example3RazorCode = @"
<BitElement Tag=""@elementTag""
            placeholder=""@elementTag""
            target=""_blank""
            href=""https://bitplatform.dev/"">
    @elementTag
</BitElement>

<BitDropdown Label=""Tags"" Items=""tagsList"" @bind-Value=""elementTag"" />";
    private string example3CsharpCode = @"
private string elementTag = ""div"";
private List<BitDropdownItem<string>> tagsList = new()
{
        new() { Text = ""div"", Value = ""div"" },
        new() { Text = ""a"", Value = ""a"" },
        new() { Text = ""input"", Value = ""input"" },
        new() { Text = ""button"", Value = ""button"" },
        new() { Text = ""textarea"", Value = ""textarea"" },
        new() { Text = ""progress"", Value = ""progress"" }
};";

}
