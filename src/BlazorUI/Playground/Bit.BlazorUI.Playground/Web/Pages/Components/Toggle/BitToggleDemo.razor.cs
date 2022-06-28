using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Toggle
{
    public partial class BitToggleDemo
    {
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked;
        private bool BindedIsToggleUnChecked;

        private string SuccessMessage = string.Empty;
        public FormModel ValidationForm { get; set; }

        public class FormModel
        {
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions.")]
            public bool TermsAgreement { get; set; }
        }

        protected override void OnInitialized()
        {
            ValidationForm = new FormModel();
        }

        private async void HandleValidSubmit()
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

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "DefaultText",
                Type = "string",
                DefaultValue = "",
                Description = "Default text of the toggle when it is neither ON or OFF.",
            },
            new ComponentParameter()
            {
                Name = "Value",
                Type = "bool",
                DefaultValue = "false",
                Description = "Checked state of the toggle.",
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
                Name = "IsInlineLabel",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the label (not the onText/offText) should be positioned inline with the toggle control. Left (right in RTL) side when on/off text provided VS right (left in RTL) side when there is no on/off text.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "Label of the toggle.",
            },
            new ComponentParameter()
            {
                Name = "LabelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Custom label of the toggle.",
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
                Name = "OffText",
                Type = "string",
                DefaultValue = "",
                Description = "Text to display when toggle is OFF.",
            },
            new ComponentParameter()
            {
                Name = "OnText",
                Type = "string",
                DefaultValue = "",
                Description = "Text to display when toggle is ON.",
            },
            new ComponentParameter()
            {
                Name = "Role",
                Type = "string",
                DefaultValue = "",
                Description = "Denotes role of the toggle, default is switch.",
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

        private readonly string example1HTMLCode = @"<div>
    <BitToggle Label=""Enabled And Checked"" @bind-Value=""IsToggleChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div>
    <BitToggle Label=""Enabled And Unchecked"" @bind-Value=""IsToggleUnChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div>
    <BitToggle Label=""Disabled And Checked"" Value=""true"" IsEnabled=""false"" OnText=""On"" OffText=""Off"" />
</div>
<div>
    <BitToggle Label=""Disabled And Unchecked"" Value=""false"" IsEnabled=""false"" OnText=""On"" OffText=""Off"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""With Inline Label"" @bind-Value=""IsToggleUnChecked"" IsEnabled=""true"" IsInlineLabel=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""Disabled With Inline Label"" Value=""false"" IsEnabled=""false"" IsInlineLabel=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""With Inline Label And Without OnText And OffText"" @bind-Value=""IsToggleUnChecked"" IsEnabled=""true"" IsInlineLabel=""true"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""Disabled With Inline Label And Without OnText And OffText"" Value=""false"" IsEnabled=""false"" IsInlineLabel=""true"" />
</div>
<div>
    <BitToggle Label=""Enabled And Checked (ARIA 1.0 compatible)"" @bind-Value=""IsToggleChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" Role=""Checkbox"" />
</div>";

        private readonly string example1CSharpCode = @"
private bool BindedIsToggleUnChecked = false;
private bool IsToggleUnChecked = false;";

        private readonly string example2HTMLCode = @"<div>
    <BitToggle @bind-Value=""IsToggleUnChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"">
        <LabelFragment>
            Custom Inline Label
        </LabelFragment>
    </BitToggle>
</div>
<div>
    <BitToggle @bind-Value=""BindedIsToggleUnChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" IsInlineLabel=""true"">
        <LabelFragment>
            Custom Inline Label
        </LabelFragment>
    </BitToggle>
    <div class=""m-t-15"">
        <BitButton Class=""m-t-15"" OnClick=""() => BindedIsToggleUnChecked = true"">Make Toggle Check</BitButton>
    </div>
</div>";

        private readonly string example3CSharpCode = @"
private string SuccessMessage = string.Empty;
public FormModel ValidationForm { get; set; }

public class FormModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

protected override void OnInitialized()
{
    ValidationForm = new FormModel();
}

private async void HandleValidSubmit()
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

        private readonly string example3HTMLCode = @"@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""ValidationForm"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitToggle @bind-Value=""ValidationForm.TermsAgreement"" DefaultText=""I agree with the terms and conditions."" />

            <ValidationMessage For=""@(() => ValidationForm.TermsAgreement)"" />
        </div>

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

    }
}
