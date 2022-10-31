using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Checkbox;

public partial class BitCheckboxDemo
{
    private bool IsIndeterminated = true;
    private bool IsChecked_OneWay;
    private bool IsChecked_TwoWay;
    private bool IsIndeterminated_OneWay = true;
    private bool IsIndeterminated_TwoWay = true;
    private BitCheckboxValidationModel ValidationForm = new();
    private string SuccessMessage = string.Empty;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the checkbox for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "AriaLabelledby",
            Type = "string",
            DefaultValue = "",
            Description = "ID for element that contains label information for the checkbox.",
        },
        new ComponentParameter()
        {
            Name = "AriaPositionInSet",
            Type = "string",
            DefaultValue = "",
            Description = "The position in the parent set (if in a set) for aria-posinset.",
        },
        new ComponentParameter()
        {
            Name = "AriaSetSize",
            Type = "string",
            DefaultValue = "",
            Description = "The total size of the parent set (if in a set) for aria-setsize.",
        },
        new ComponentParameter()
        {
            Name = "BoxSide",
            Type = "BitBoxSide",
            LinkType = LinkType.Link,
            Href = "#box-side-enum",
            DefaultValue = "BitBoxSide.Start",
            Description = "Determines whether the checkbox should be shown before the label (start) or after (end).",
        },
        new ComponentParameter()
        {
            Name = "CheckmarkIconName",
            Type = "BitIcon",
            DefaultValue = "",
            Description = "Custom icon for the check mark rendered by the checkbox instade of default check mark icon.",
        },
        new ComponentParameter()
        {
            Name = "CheckmarkIconAriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "The content of checkbox, It can be Any custom tag or a text.",
        },
        new ComponentParameter()
        {
            Name = "DefaultIsIndeterminate",
            Type = "bool",
            DefaultValue = "",
            Description = "Default indeterminate visual state for checkbox.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "bool",
            DefaultValue = "",
            Description = "Use this if you want an uncontrolled component, meaning the Checkbox instance maintains its own state.",
        },
        new ComponentParameter()
        {
            Name = "IsIndeterminate",
            Type = "bool",
            DefaultValue = "",
            Description = "Callback that is called when the IsIndeterminate parameter changed.",
        },
        new ComponentParameter()
        {
            Name = "IsIndeterminateChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "An indeterminate visual state for checkbox. Setting indeterminate state takes visual precedence over checked given but does not affect on Value state.",
        },
        new ComponentParameter()
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "",
            Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback that is called when the checked value has changed.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the checkbox clicked.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "",
            Description = "Title text applied to the root element and the hidden checkbox input.",
        },

        new ComponentParameter()
        {
            Name = "Value",
            Type = "bool",
            DefaultValue = "",
            Description = "Checkbox state, control the checked state at a higher level.",
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback that is called when the Value parameter changed.",
        },
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "box-side-enum",
            Title = "BitBoxSide Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Start",
                    Description="The checkbox shows before the label.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "End",
                    Description="The checkbox shows after the label.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"
<BitCheckbox>Basic Checkbox</BitCheckbox>
<BitCheckbox Value=""true"">One-way Checked Checkbox (Fixed)</BitCheckbox>
<BitCheckbox IsEnabled=""false"">Disable Checkbox</BitCheckbox>
<BitCheckbox IsEnabled=""false"" Value=""true"">Disable Checked Checkbox</BitCheckbox>
<BitCheckbox CheckmarkIconName=""BitIconName.Heart"">Custom checkmark Checkbox</BitCheckbox>";
    private readonly string example2TMLCode = @"
<BitCheckbox BoxSide=""@BitCheckBoxSide.End"">Reversed - Basic Checkbox</BitCheckbox>
<BitCheckbox BoxSide=""@BitCheckBoxSide.End"" Value=""true"">Reversed - One-way Checked Checkbox (Fixed)</BitCheckbox>
<BitCheckbox BoxSide=""@BitCheckBoxSide.End"" IsEnabled=""false"">Reversed - Disable Checkbox</BitCheckbox>
<BitCheckbox BoxSide=""@BitCheckBoxSide.End"" IsEnabled=""false"" Value=""true"">Reversed - Disable Checked Checkbox</BitCheckbox>";
    private readonly string example3HTMLCode = @"
<BitCheckbox @bind-IsIndeterminate=""IsIndeterminated"">Indeterminate checkbox</BitCheckbox>
<BitCheckbox IsIndeterminate=""true"">One-way indeterminate Checkbox (Fixed)</BitCheckbox>
<BitCheckbox IsIndeterminate=""true"" IsEnabled=""false"">Disabled indeterminate checkbox</BitCheckbox>";
    private readonly string example4HTMLCode = @"
<BitCheckbox Value=""IsChecked_OneWay"">One-way Controlled Checkbox</BitCheckbox>
<BitButton OnClick=""() => IsChecked_OneWay = !IsChecked_OneWay"">
    @(IsChecked_OneWay ? ""Remove"" : ""Make"") Checked
</BitButton>

<BitCheckbox @bind-Value=""IsChecked_TwoWay"">Two-way Controlled Checkbox</BitCheckbox>
<BitButton OnClick=""() => IsChecked_TwoWay = !IsChecked_TwoWay"">
    @(IsChecked_TwoWay ? ""Remove"" : ""Make"") Checked
</BitButton>

<BitCheckbox IsIndeterminate=""IsIndeterminated_OneWay"">One-way Controlled indeterminate checkbox</BitCheckbox>
<BitButton OnClick=""() => IsIndeterminated_OneWay = !IsIndeterminated_OneWay"">
    @(IsIndeterminated_OneWay ? ""Remove"" : ""Make"") Indeterminate
</BitButton>

<BitCheckbox @bind-IsIndeterminate=""IsIndeterminated_TwoWay"">Two-way Controlled indeterminate checkbox</BitCheckbox>
<BitButton OnClick=""() => IsIndeterminated_TwoWay = !IsIndeterminated_TwoWay"">
    @(IsIndeterminated_TwoWay ? ""Remove"" : ""Make"") Indeterminate
</BitButton>
";
    private readonly string example5HTMLCode = @"
<BitCheckbox>
    Custom-rendered label with a link go to
    <a href=""https://github.com/bitfoundation/bitplatform"">
        Bit Platform repository page
    </a>
</BitCheckbox>";
    private readonly string example6HTMLCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""ValidationForm"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitCheckbox @bind-Value=""ValidationForm.TermsAgreement"">
                I agree with the terms and conditions.
            </BitCheckbox>

            <ValidationMessage For=""@(() => ValidationForm.TermsAgreement)"" />
        </div>

        <br />

        <BitButton ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";

    private readonly string example3CSharpCode = @"
private bool IsIndeterminated = true;";
    private readonly string example4CSharpCode = @"
private bool IsChecked_OneWay;
private bool IsChecked_TwoWay;
private bool IsIndeterminated_OneWay = true;
private bool IsIndeterminated_TwoWay = true;";
    private readonly string example6CSharpCode = @"
private BitCheckboxValidationModel ValidationForm = new();
private string SuccessMessage = string.Empty;

public class BitCheckboxValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }
}
