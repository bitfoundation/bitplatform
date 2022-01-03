using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Toggle
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
                Name = "DefaultText",
                Type = "string",
                DefaultValue = "",
                Description = "Default text of the toggle when it is neither ON or OFF.",
            },
            new ComponentParameter()
            {
                Name = "IsChecked",
                Type = "bool",
                DefaultValue = "false",
                Description = "Checked state of the toggle.",
            },
            new ComponentParameter()
            {
                Name = "IsCheckedChanged",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the IsChecked parameter changed.",
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
        };

        private readonly string toggleSampleCode = $"<BitToggle Label='Enabled And Checked'{Environment.NewLine}" +
              $"@bind-IsChecked='@IsToggleChecked'{Environment.NewLine}" +
              $"IsEnabled='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"<BitToggle Label='Enabled And Unchecked'{Environment.NewLine}" +
              $"@bind-IsChecked='@IsToggleChecked'{Environment.NewLine}" +
              $"IsEnabled='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"<BitToggle Label='Disabled And Checked'{Environment.NewLine}" +
              $"IsChecked='true'{Environment.NewLine}" +
              $"IsEnabled='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"<BitToggle Label='Disabled And Unchecked'{Environment.NewLine}" +
              $"IsChecked='false'{Environment.NewLine}" +
              $"IsEnabled='false'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"<BitToggle Label='Disabled With Inline Label'{Environment.NewLine}" +
              $"IsChecked='false'{Environment.NewLine}" +
              $"IsEnabled='false'{Environment.NewLine}" +
              $"IsInlineLabel='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"<BitToggle Label='Enabled And Unchecked'{Environment.NewLine}" +
              $"@bind-IsChecked='@IsToggleUnChecked'{Environment.NewLine}" +
              $"IsEnabled='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"<BitToggle Label='Disabled With Inline Label'{Environment.NewLine}" +
              $"IsChecked='false'{Environment.NewLine}" +
              $"IsEnabled='false'{Environment.NewLine}" +
              $"IsInlineLabel='true'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"<BitToggle Label='Enabled And Checked (ARIA 1.0 compatible)'{Environment.NewLine}" +
              $"@bind-IsChecked='@IsToggleChecked'{Environment.NewLine}" +
              $"IsEnabled='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $"Role='Checkbox'{Environment.NewLine}" +
              $" />{Environment.NewLine}" +
              $"@code {{ {Environment.NewLine}" +
              $"private bool IsToggleChecked = true;{Environment.NewLine}" +
              $"private bool IsToggleUnChecked = false;{Environment.NewLine}" +
              $"}}";

        private readonly string customLabelSampleCode = $"<BitToggle @bind-IsChecked='@IsToggleUnChecked'{Environment.NewLine}" +
              $"IsEnabled='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'>{Environment.NewLine}" +
              $"<LabelFragment>{Environment.NewLine}" +
              $"Custom Inline Label{Environment.NewLine}" +
              $"</LabelFragment>{Environment.NewLine}" +
              $"</BitToggle>{Environment.NewLine}" +
              $"<BitToggle @bind-IsChecked='@BindedIsToggleUnChecked'{Environment.NewLine}" +
              $"IsEnabled='true'{Environment.NewLine}" +
              $"OnText='On'{Environment.NewLine}" +
              $"OffText='Off'{Environment.NewLine}" +
              $"IsInlineLabel='true'>{Environment.NewLine}" +
              $"<LabelFragment>{Environment.NewLine}" +
              $"Custom Inline Label{Environment.NewLine}" +
              $"</LabelFragment>{Environment.NewLine}" +
              $"</BitToggle>{Environment.NewLine}" +
              $"<BitButton OnClick='() => BindedIsToggleUnChecked = true'>{Environment.NewLine}" +
              $"Make Toggle Check{Environment.NewLine}" +
              $"</BitButton>{Environment.NewLine}" +
              $"@code {{ {Environment.NewLine}" +
              $"private bool BindedIsToggleUnChecked = false;{Environment.NewLine}" +
              $"private bool IsToggleUnChecked = false;{Environment.NewLine}" +
              $"}}";
    }
}
