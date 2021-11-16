using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Inputs
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
        };

        private readonly string choiceGroupSampleCode = $"<BitLabel>Selected Key is : @MySelectedKey</BitLabel>{Environment.NewLine}" +
                $"<BitTextField @bind-Value='MySelectedKey' Placeholder='Select one of A, B or C'></BitTextField>{Environment.NewLine}" +
                $"<BitChoiceGroup Name='Group1' Label='Pick one' IsRequired='true' @bind-SelectedKey='MySelectedKey'>{Environment.NewLine}" +
                $"<BitCheckbox CheckmarkIconName='Heart'>Custom checkmark Checkbox</BitCheckbox>{Environment.NewLine}" +
                $"<BitChoiceOption Key='A' Text='Option A' Value='1'></BitChoiceOption> {Environment.NewLine}" +
                $"<BitChoiceOption Key='B' Text='Option B' Value='2'></BitChoiceOption>{Environment.NewLine}" +
                $"<BitChoiceOption Key='C' Text='Disabled option C' Value='3' IsEnabled='false'></BitChoiceOption>{Environment.NewLine}" +
                $"<@code {{ {Environment.NewLine}" +
                $"private string MySelectedKey = 'B';{Environment.NewLine}" +
                "}";

        private readonly string disabledSampleCode = $"<BitChoiceGroup Name='Group2' IsEnabled='false' Label='Pick one' DefaultSelectedKey='C'>{Environment.NewLine}" +
                $"<BitChoiceOption Key='A' Text='Option A' Value='1'></BitChoiceOption>{Environment.NewLine}" +
                $"<BitChoiceOption Key='B' Text='Option2 B' Value='2'></BitChoiceOption>{Environment.NewLine}" +
                $"<BitChoiceOption Key='C' Text='Disabled option C' Value='3' IsEnabled='false'></BitChoiceOption>{Environment.NewLine}" +
                $"</BitChoiceGroup>";

        private readonly string choiceGroupwithImagesSampleCode = $"<BitChoiceGroup Name='Group1' Label='Pick one image' DefaultSelectedKey='pie'>{Environment.NewLine}" +
                $"<BitChoiceOption Key='bar' Text='Clustered bar chart' Value='1' ImageSrc='https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png' ImageAlt='alt for image Option 1' SelectedImageSrc='https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png' ImageSize='new System.Drawing.Size( width: 32, height: 32)'></BitChoiceOption>{Environment.NewLine}" +
                $"<BitChoiceOption Key='pie' Text='Pie chart' Value='2' ImageSrc='https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png' ImageAlt='alt for image Option 2' SelectedImageSrc='https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png' ImageSize='new System.Drawing.Size( width: 32, height: 32)'></BitChoiceOption>{Environment.NewLine}" +
                $"<BitChoiceOption Key='disabeled_option' Text='Disabeled' IsEnabled='false' Value='3' ImageSrc='https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png' ImageAlt='alt for image Option 2' SelectedImageSrc='https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png' ImageSize='new System.Drawing.Size( width: 32, height: 32)'></BitChoiceOption>{Environment.NewLine}" +
                $"</BitChoiceGroup>";

        private readonly string choiceGroupwithIconsSampleCode = $"<BitChoiceGroup Name='Group1' Label='Pick one icon'>{Environment.NewLine}" +
                $"<BitChoiceOption Key='day' Text='Day' Value='1' IconName='CalendarDay'></BitChoiceOption>{Environment.NewLine}" +
                $"<BitChoiceOption Key='week' Text='Week' Value='2' IconName='CalendarWeek'></BitChoiceOption>{Environment.NewLine}" +
                $"<BitChoiceOption Key='month' Text='Month' Value='3' IconName='Calendar' IsEnabled='false'></BitChoiceOption>{Environment.NewLine}" +
                "</BitChoiceGroup>";
    }
}
