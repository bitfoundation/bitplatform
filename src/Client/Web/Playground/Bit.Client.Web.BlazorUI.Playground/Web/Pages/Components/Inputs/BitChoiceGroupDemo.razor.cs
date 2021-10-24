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
                Name = "childContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of action button, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "defaultSelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Default selected key for ChoiceGroup.",
            },
            new ComponentParameter()
            {
                Name = "isRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "If true, an option must be selected in the ChoiceGroup.",
            },
            new ComponentParameter()
            {
                Name = "label",
                Type = "string",
                DefaultValue = "",
                Description = "Descriptive label for the choice group.",
            },
            new ComponentParameter()
            {
                Name = "labelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Used to customize the label for the choice group.",
            },
            new ComponentParameter()
            {
                Name = "name",
                Type = "string",
                DefaultValue = "",
                Description = "Name of ChoiceGroup, this name is used to group each ChoiceOption into the same logical ChoiceGroup.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the action button clicked.",
            },
            new ComponentParameter()
            {
                Name = "onValueChange",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback that is called when the value parameter is changed.",
            },
            new ComponentParameter()
            {
                Name = "selectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Contains the key of the selected item.",
            },
            new ComponentParameter()
            {
                Name = "selectedKeyChanged",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when the selected Key changed.",
            },
            new ComponentParameter()
            {
                Name = "value",
                Type = "string",
                DefaultValue = "",
                Description = "Value of ChoiceGroup, the value of selected ChoiceOption set on it.",
            },
        };
    }
}
