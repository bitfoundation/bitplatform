using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Components.ChoiceGroup;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup
{
    public partial class BitChoiceGroupDemo
    {
        public List<BitChoiceGroupOption> Example1And5Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Option A",
               Key = "A"
            },
            new BitChoiceGroupOption()
            {
                Text = "Option B",
                Key = "B"
            },
            new BitChoiceGroupOption()
            {
               Text = "Option C",
               Key = "C"
            },
            new BitChoiceGroupOption()
            {
               Text = "Option D",
               Key = "D"
            }
        };

        public List<BitChoiceGroupOption> Example2Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Option A",
               Key = "A"
            },
            new BitChoiceGroupOption()
            {
                Text = "Option B",
                Key = "B"
            },
            new BitChoiceGroupOption()
            {
               Text = "Option C",
               Key = "C",
               IsEnabled = false
            },
            new BitChoiceGroupOption()
            {
               Text = "Option D",
               Key = "D"
            }
        };

        public List<BitChoiceGroupOption> Example3Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Bar",
               Key = "Bar",
               ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
               SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
               ImageAlt = "alt for Bar image",
               ImageSize = new System.Drawing.Size(32, 32)
            },
            new BitChoiceGroupOption()
            {
                Text = "Pie",
                Key = "Pie",
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
               Key = "Day",
               iconName = BitIconName.CalendarDay
            },
            new BitChoiceGroupOption()
            {
                Text = "Week",
                Key = "Week",
                iconName = BitIconName.CalendarWeek
            },
            new BitChoiceGroupOption()
            {
                Text = "Month",
                Key = "Month",
                iconName = BitIconName.Calendar,
                IsEnabled = false
            }
        };

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
                Description = "Callback for when the option has been changed"
            },
            new ComponentParameter
            {
                Name = "OnClick",
                Type = "EventCallback<ChangeEventArgs>",
                DefaultValue = "null",
                Description = "Callback for when the option clicked"
            },
        };

        private readonly string example1And5CSharpCode = @"
        public List<BitChoiceGroupOption> Example1And5Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Option A"",
               Key = ""A""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option B"",
                Key = ""B""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option C"",
                Key = ""C""
            },
            new BitChoiceGroupOption()
            {
                Text = ""Option D"",
                Key = ""D""
            }
        }; 
";

        private readonly string example2CSharpCode = @"
        public List<BitChoiceGroupOption> Example2Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Option A"",
               Key = ""A""
            },
            new BitChoiceGroupOption()
            {
               Text = ""Option B"",
               Key = ""B""
            },
            new BitChoiceGroupOption()
            {
               Text = ""Option C"",
               Key = ""C"",
               IsEnabled = false
            },
            new BitChoiceGroupOption()
            {
               Text = ""Option D"",
               Key = ""D""
            }
        };
";
        
        private readonly string example3CSharpCode = @"
        public List<BitChoiceGroupOption> Example3Options { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
               Text = ""Bar"",
               Key = ""Bar"",
               ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
               SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
               ImageAlt = ""alt for Bar image"",
               ImageSize = new System.Drawing.Size(32, 32)
            },
            new BitChoiceGroupOption()
            {
                Text = ""Pie"",
                Key = ""Pie"",
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
               Key = ""Day"",
               iconName = BitIconName.CalendarDay
            },
            new BitChoiceGroupOption()
            {
                Text = ""Week"",
                Key = ""Week"",
                iconName = BitIconName.CalendarWeek
            },
            new BitChoiceGroupOption()
            {
                Text = ""Month"",
                Key = ""Month"",
                iconName = BitIconName.Calendar,
                IsEnabled = false
            }
        };
";

        private readonly string example1HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""Example1And5Options"">
</BitChoiceGroup>";

        private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""Example2Options"" DefaultSelectedKey=""B"">
</BitChoiceGroup>";

        private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image"" Options=""Example3Options"">
</BitChoiceGroup>";

        private readonly string example4HtmlCode = @"
<BitChoiceGroup Label=""Pick one icon"" Options=""Example4Options"">
</BitChoiceGroup>";

        private readonly string example5HtmlCode = @"
<BitChoiceGroup Options=""Example1And5Options"" >
    <LabelFragment>
        Custom label <BitIconButton IconName= ""BitIconName.Filter"" ></ BitIconButton >
    </ LabelFragment >
</ BitChoiceGroup >";

    }
}
