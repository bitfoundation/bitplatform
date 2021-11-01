using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Toggles
{
    public partial class BitToggleDemo
    {
        private bool IsToggleChecked = true;
        private bool IsToggleUnChecked = false;
        private bool BindedIsToggleUnChecked;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "defaultText",
                Type = "string",
                DefaultValue = "",
                Description = "Default text of the toggle when it is neither ON or OFF.",
            },
            new ComponentParameter()
            {
                Name = "isChecked",
                Type = "bool",
                DefaultValue = "false",
                Description = "Checked state of the toggle.",
            },
            new ComponentParameter()
            {
                Name = "isCheckedChanged",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the IsChecked parameter changed.",
            },
            new ComponentParameter()
            {
                Name = "isInlineLabel",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the label (not the onText/offText) should be positioned inline with the toggle control. Left (right in RTL) side when on/off text provided VS right (left in RTL) side when there is no on/off text.",
            },
            new ComponentParameter()
            {
                Name = "label",
                Type = "string",
                DefaultValue = "",
                Description = "Label of the toggle.",
            },
            new ComponentParameter()
            {
                Name = "labelFragment",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "Custom label of the toggle.",
            },
            new ComponentParameter()
            {
                Name = "onChange",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the checked value has changed.",
            },
            new ComponentParameter()
            {
                Name = "offText",
                Type = "string",
                DefaultValue = "",
                Description = "Text to display when toggle is OFF.",
            },
            new ComponentParameter()
            {
                Name = "onText",
                Type = "string",
                DefaultValue = "",
                Description = "Text to display when toggle is ON.",
            },
            new ComponentParameter()
            {
                Name = "role",
                Type = "string",
                DefaultValue = "",
                Description = "Denotes role of the toggle, default is switch.",
            },
        };
    }
}
