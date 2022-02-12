﻿using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
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
    <BitToggle Label=""Enabled And Checked"" @bind-IsChecked=""IsToggleChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div>
    <BitToggle Label=""Enabled And Unchecked"" @bind-IsChecked=""IsToggleUnChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div>
    <BitToggle Label=""Disabled And Checked"" IsChecked=""true"" IsEnabled=""false"" OnText=""On"" OffText=""Off"" />
</div>
<div>
    <BitToggle Label=""Disabled And Unchecked"" IsChecked=""false"" IsEnabled=""false"" OnText=""On"" OffText=""Off"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""With Inline Label"" @bind-IsChecked=""IsToggleUnChecked"" IsEnabled=""true"" IsInlineLabel=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""Disabled With Inline Label"" IsChecked=""false"" IsEnabled=""false"" IsInlineLabel=""true"" OnText=""On"" OffText=""Off"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""With Inline Label And Without OnText And OffText"" @bind-IsChecked=""IsToggleUnChecked"" IsEnabled=""true"" IsInlineLabel=""true"" />
</div>
<div class=""m-t-15"">
    <BitToggle Label=""Disabled With Inline Label And Without OnText And OffText"" IsChecked=""false"" IsEnabled=""false"" IsInlineLabel=""true"" />
</div>
<div>
    <BitToggle Label=""Enabled And Checked (ARIA 1.0 compatible)"" @bind-IsChecked=""IsToggleChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" Role=""Checkbox"" />
</div>";

        private readonly string example1CSharpCode = @"@code {
    private bool BindedIsToggleUnChecked = false;
    private bool IsToggleUnChecked = false;
}";

        private readonly string example2HTMLCode = @"<div>
    <BitToggle @bind-IsChecked=""IsToggleUnChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"">
        <LabelFragment>
            Custom Inline Label
        </LabelFragment>
    </BitToggle>
</div>
<div>
    <BitToggle @bind-IsChecked=""BindedIsToggleUnChecked"" IsEnabled=""true"" OnText=""On"" OffText=""Off"" IsInlineLabel=""true"">
        <LabelFragment>
            Custom Inline Label
        </LabelFragment>
    </BitToggle>
    <div class=""m-t-15"">
        <BitButton Class=""m-t-15"" OnClick=""() => BindedIsToggleUnChecked = true"">Make Toggle Check</BitButton>
    </div>
</div>";
    }
}
