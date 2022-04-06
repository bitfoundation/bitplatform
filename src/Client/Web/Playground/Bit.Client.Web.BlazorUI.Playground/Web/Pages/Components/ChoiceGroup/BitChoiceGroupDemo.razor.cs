using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Components.ChoiceGroup;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup
{
    public partial class BitChoiceGroupDemo
    {
        public List<BitChoiceGroupOption> Example_1_5_6_Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Option A",
               Value = "A"
            },
            new BitChoiceGroupOption()
            {
                Text = "Option B",
                Value = "B"
            },
            new BitChoiceGroupOption()
            {
               Text = "Option C",
               Value = "C"
            },
            new BitChoiceGroupOption()
            {
               Text = "Option D",
               Value = "D"
            }
        };

        public List<BitChoiceGroupOption> Example2Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Option A",
               Value = "A"
            },
            new BitChoiceGroupOption()
            {
                Text = "Option B",
                Value = "B"
            },
            new BitChoiceGroupOption()
            {
               Text = "Option C",
               Value = "C",
               IsEnabled = false
            },
            new BitChoiceGroupOption()
            {
               Text = "Option D",
               Value = "D"
            }
        };

        public List<BitChoiceGroupOption> Example3Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Bar",
               Value = "Bar",
               ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
               SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
               ImageAlt = "alt for Bar image",
               ImageSize = new System.Drawing.Size(32, 32)
            },
            new BitChoiceGroupOption()
            {
                Text = "Pie",
                Value = "Pie",
                ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
                SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
                ImageAlt = "alt for Pie image",
                ImageSize = new System.Drawing.Size(32, 32)
            }
        };

        public List<BitChoiceGroupOption> Example4Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Day",
               Value = "Day",
               IconName = BitIconName.CalendarDay
            },
            new BitChoiceGroupOption()
            {
                Text = "Week",
                Value = "Week",
                IconName = BitIconName.CalendarWeek
            },
            new BitChoiceGroupOption()
            {
                Text = "Month",
                Value = "Month",
                IconName = BitIconName.Calendar,
                IsEnabled = false
            }
        };

        public ChoiceGroupValidationModel ValidationModel = new();
        public string SuccessMessage { get; set; } = string.Empty;

        public class ChoiceGroupValidationModel
        {
            [Required(ErrorMessage = "Pick one")]
            public string Value { get; set; }
        }

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter
            {
                Name = "Options",
                Type = "List<BitChoiceGroupOption>",
                DefaultValue = "new List<BitChoiceGroupOption>()",
                Description = "List of options, each of which is a selection in the ChoiceGroup."
            },
            new ComponentParameter
            {
                Name = "Name",
                Type = "string",
                DefaultValue = "Guid.NewGuid().ToString()",
                Description = "Name of ChoiceGroup, this name is used to group each option into the same logical ChoiceGroup."
            },
            new ComponentParameter
            {
                Name = "DefaultSelectedKey",
                Type = "string?",
                DefaultValue = "null",
                Description = "Default selected key for ChoiceGroup."
            },
            new ComponentParameter
            {
                Name = "AriaLabelledBy",
                Type = "string?",
                DefaultValue = "null",
                Description = "ID of an element to use as the aria label for this ChoiceGroup."
            },
            new ComponentParameter
            {
                Name = "Label",
                Type = "string?",
                DefaultValue = "null",
                Description = "Descriptive label for the ChoiceGroup."
            },
            new ComponentParameter
            {
                Name = "LabelFragment",
                Type = "RenderFragment?",
                DefaultValue = "null",
                Description = "Used to customize the label for the ChoiceGroup."
            },
            new ComponentParameter
            {
                Name = "OnChange",
                Type = "EventCallback<ChangeEventArgs>",
                DefaultValue = "null",
                Description = "Callback for when the option has been changed."
            },
            new ComponentParameter
            {
                Name = "OnFocus",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "null",
                Description = "Callback for when focus moves into the ChoiceGroup option."
            },
            new ComponentParameter()
            {
                Name = "ValueChanged",
                Type = "EventCallback<string?>",
                DefaultValue = "",
                Description = "Callback for when the option has been changed.",
            },
            new ComponentParameter()
            {
                Name = "Value",
                Type = "string?",
                DefaultValue = "",
                Description = "Value of ChoiceGroup, the value of selected ChoiceGroupOption set on it.",
            }
        };

        private readonly string example_1_5_CSharpCode = @"
        public List<BitChoiceGroupOption> Example_1_5_6_Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Option A"",
               Value = ""A""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option B"",
                Value = ""B""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option C"",
                Value = ""C""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option D"",
                Value = ""D""
            }
        }; 
";

        private readonly string example2CSharpCode = @"
        public List<BitChoiceGroupOption> Example2Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Option A"",
               Value = ""A""
            },
            new BitChoiceGroupOption()
            {
               Text = ""Option B"",
               Value = ""B""
            },
            new BitChoiceGroupOption()
            {
               Text = ""Option C"",
               Value = ""C"",
               IsEnabled = false
            },
            new BitChoiceGroupOption()
            {
               Text = ""Option D"",
               Value = ""D""
            }
        };
";

        private readonly string example3CSharpCode = @"
        public List<BitChoiceGroupOption> Example3Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Bar"",
               Value = ""Bar"",
               ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
               SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
               ImageAlt = ""alt for Bar image"",
               ImageSize = new System.Drawing.Size(32, 32)
            },
            new BitChoiceGroupOption()
            {
                Text = ""Pie"",
                Value = ""Pie"",
                ImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
                SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
                ImageAlt = ""alt for Pie image"",
                ImageSize = new System.Drawing.Size(32, 32)
            }
        };
";

        private readonly string example4CSharpCode = @"
        public List<BitChoiceGroupOption> Example4Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Day"",
               Value = ""Day"",
               IconName = BitIconName.CalendarDay
            },
            new BitChoiceGroupOption()
            {
                Text = ""Week"",
                Value = ""Week"",
                IconName = BitIconName.CalendarWeek
            },
            new BitChoiceGroupOption()
            {
                Text = ""Month"",
                Value = ""Month"",
                IconName = BitIconName.Calendar,
                IsEnabled = false
            }
        };
";

        private readonly string example6CSharpCode = @"
        public List<BitChoiceGroupOption> Example_1_5_6_Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Option A"",
               Value = ""A""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option B"",
                Value = ""B""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option C"",
                Value = ""C""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option D"",
                Value = ""D""
            }
        };

        public ChoiceGroupValidationModel ValidationModel = new();

        public string SuccessMessage { get; set; } = string.Empty;

        public class ChoiceGroupValidationModel
        {
            [Required(ErrorMessage = ""Pick one"")]
            public string Value { get; set; }
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
        }
";

        private readonly string example1HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""Example_1_5_6_Options"">
</BitChoiceGroup>";

        private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""Example2Options"" DefaultValue=""B"">
</BitChoiceGroup>";

        private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image"" Options=""Example3Options"">
</BitChoiceGroup>";

        private readonly string example4HtmlCode = @"
<BitChoiceGroup Label=""Pick one icon"" Options=""Example4Options"">
</BitChoiceGroup>";

        private readonly string example5HtmlCode = @"
<BitChoiceGroup Options=""Example_1_5_6_Options"" >
    <LabelFragment>
        Custom label <BitIconButton IconName= ""BitIconName.Filter"" ></ BitIconButton >
    </ LabelFragment >
</ BitChoiceGroup >";

        private readonly string example6HtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup Options = ""Example_1_5_6_Options"" @bind-Value=""ValidationModel.Value"">
            </BitChoiceGroup>
            <ValidationMessage For = ""@(() => ValidationModel.Value)"" />
        </ div >
        < BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType = ""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";

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
    }
}
