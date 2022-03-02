using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup
{
    public partial class BitChoiceGroupDemo
    {
        private string MySelectedKey = "B";

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of action button, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "DefaultSelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Default selected key for ChoiceGroup.",
            },
            new ComponentParameter()
            {
                Name = "IsRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "If true, an option must be selected in the ChoiceGroup.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "Descriptive label for the choice group.",
            },
            new ComponentParameter()
            {
                Name = "LabelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Used to customize the label for the choice group.",
            },
            new ComponentParameter()
            {
                Name = "Name",
                Type = "string",
                DefaultValue = "",
                Description = "Name of ChoiceGroup, this name is used to group each ChoiceOption into the same logical ChoiceGroup.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the action button clicked.",
            },
            new ComponentParameter()
            {
                Name = "OnValueChange",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback that is called when the value parameter is changed.",
            },
            new ComponentParameter()
            {
                Name = "SelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Contains the key of the selected item.",
            },
            new ComponentParameter()
            {
                Name = "SelectedKeyChanged",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when the selected Key changed.",
            },
            new ComponentParameter()
            {
                Name = "Value",
                Type = "string",
                DefaultValue = "",
                Description = "Value of ChoiceGroup, the value of selected ChoiceOption set on it.",
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

        private readonly string example1HTMLCode = @"<BitLabel>Selected Key is : @MySelectedKey</BitLabel>
<BitTextField @bind-Value=""MySelectedKey"" Placeholder=""Select one of A, B or C""></BitTextField>
<BitChoiceGroup Name=""Group1"" Label=""Pick one"" IsRequired=""true"" @bind-SelectedKey=""MySelectedKey"">
    <BitChoiceOption Key=""A"" Text=""Option A"" Value=""1""></BitChoiceOption>
    <BitChoiceOption Key=""B"" Text=""Option B"" Value=""2""></BitChoiceOption>
    <BitChoiceOption Key=""C"" Text=""Disabled option C"" Value=""3"" IsEnabled=""false""></BitChoiceOption>
</BitChoiceGroup>";

        private readonly string example1CSharpCode = @"
private string MySelectedKey = ""B"";";

        private readonly string example2HTMLCode = @"<BitChoiceGroup Name=""Group2"" IsEnabled=""false"" Label=""Pick one"" DefaultSelectedKey=""C"">
    <BitChoiceOption Key=""A"" Text=""Option A"" Value=""1""></BitChoiceOption>
    <BitChoiceOption Key=""B"" Text=""Option2 B"" Value=""2""></BitChoiceOption>
    <BitChoiceOption Key=""C"" Text=""Disabled option C"" Value=""3"" IsEnabled=""false""></BitChoiceOption>
</BitChoiceGroup>";

        private readonly string example3HTMLCode = @"<BitChoiceGroup Name=""Group1"" Label=""Pick one image"" DefaultSelectedKey=""pie"">
    <BitChoiceOption Key=""bar"" Text=""Clustered bar chart"" Value=""1"" ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"" ImageAlt=""alt for image Option 1"" SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" ImageSize=""new System.Drawing.Size( width: 32, height: 32)""></BitChoiceOption>
    <BitChoiceOption Key=""pie"" Text=""Pie chart"" Value=""2"" ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"" ImageAlt=""alt for image Option 2"" SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" ImageSize=""new System.Drawing.Size( width: 32, height: 32)""></BitChoiceOption>
    <BitChoiceOption Key=""disabeled_option"" Text=""Disabeled"" IsEnabled=""false"" Value=""3"" ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"" ImageAlt=""alt for image Option 2"" SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" ImageSize=""new System.Drawing.Size( width: 32, height: 32)""></BitChoiceOption>
</BitChoiceGroup>";

        private readonly string example4HTMLCode = @"<BitChoiceGroup Name=""Group1"" Label=""Pick one icon"">
    <BitChoiceOption Key=""day"" Text=""Day"" Value=""1"" IconName=""BitIconName.CalendarDay""></BitChoiceOption>
    <BitChoiceOption Key=""week"" Text=""Week"" Value=""2"" IconName=""BitIconName.CalendarWeek""></BitChoiceOption>
    <BitChoiceOption Key=""month"" Text=""Month"" Value=""3"" IconName=""BitIconName.Calendar"" IsEnabled=""false""></BitChoiceOption>
</BitChoiceGroup>";
    }
}
