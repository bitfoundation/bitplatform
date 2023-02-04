﻿using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.BreadGroup;

public partial class BitBreadGroupDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the BitBreadGroup, that are BitBreadOption components.",
            LinkType = LinkType.Link,
            Href = "#bread-option"
        },
        new ComponentParameter
        {
            Name = "DividerIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronRight",
            Description = "The divider icon name. The default value is BitIconName.ChevronRight."
        },
        new ComponentParameter
        {
            Name = "MaxDisplayedOptions",
            Type = "uint",
            Description = "The maximum number of BitBreadGroup to display before coalescing. If not specified, all BitBreadGroup will be rendered."
        },
        new ComponentParameter
        {
            Name = "OverflowAriaLabel",
            Type = "string?",
            Description = "Aria label for the overflow button."
        },
        new ComponentParameter
        {
            Name = "OverflowIndex",
            Type = "uint",
            Description = "Optional index where overflow options will be collapsed."
        },
        new ComponentParameter
        {
            Name = "OverflowIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.More",
            Description = "The overflow icon name. The default value is BitIconName.More."
        },
        new ComponentParameter
        {
            Name = "SelectedOptionClass",
            Type = "string?",
            Description = "The CSS class attribute for the selected option."
        },
        new ComponentParameter
        {
            Name = "SelectedOptionStyle",
            Type = "string?",
            Description = "The style attribute for the selected option."
        },
    };

    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "bread-option",
            Title = "<BitBreadOption>",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "Href",
                   Type = "string?",
                   Description = "URL to navigate to when this BitBreadOption is clicked. If provided, the BitBreadOption will be rendered as a link.",
               },
               new ComponentParameter()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Set the Selected option.",
               },
               new ComponentParameter()
               {
                   Name = "OnClick",
                   Type = "EventCallback<MouseEventArgs>",
                   Description = "Callback for when the BitBreadOption clicked.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to display in the BitBreadOption option.",
               },
            }
        }
    };


    private int SelectedOptionNumber = 6;
    private uint MaxDisplayedOptions = 3;
    private uint OverflowIndex = 2;
    private int OptionsCount = 4;
    private int CustomizedSelectedOptionNumber = 4;
    private uint NumericTextFieldStep = 1;


    private readonly string example1HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadGroup>
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>

<div>
    <BitLabel>Group Disabled</BitLabel>
    <BitBreadGroup IsEnabled=""false"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>

<div>
    <BitLabel>Option Disabled</BitLabel>
    <BitBreadGroup>
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" IsEnabled=""false"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" IsEnabled=""false"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
";

    private readonly string example2HTMLCode = @"
<div>
    <BitLabel>MaxDisplayedOptions (1)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""1"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (2)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""2"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""3"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (0)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""3"" OverflowIndex=""0"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (1)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""3"" OverflowIndex=""1"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (2)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""3"" OverflowIndex=""2"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
";

    private readonly string example3HTMLCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""3"" OverflowIndex=""2"" OverflowIcon=""BitIconName.ChevronDown"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>

<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadGroup MaxDisplayedOptions=""3"" OverflowIndex=""2"" OverflowIcon=""BitIconName.CollapseMenu"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
";

    private readonly string example4HTMLCode = @"
<style>
        .bit-brg .custom-option {
            color: red;
            margin: 2px 5px;
            border-radius: 2px;
            background: limegreen;
        }

            .bit-brg .custom-option:hover {
                background: greenyellow;
            }

        .bit-brg .custom-selected-option {
            color: red;
            margin: 2px 5px;
            border-radius: 2px;
            background: mediumspringgreen;
        }

            .bit-brg .custom-selected-option:hover {
                background: greenyellow;
            }
</style>

<div>
    <BitLabel>Options Class</BitLabel>
    <BitBreadGroup>
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" Class=""custom-option"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" Class=""custom-option"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" Class=""custom-option"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" Class=""custom-option"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>Options Style</BitLabel>
    <BitBreadGroup>
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" Style=""color:red;background:greenyellow"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" Style=""color:red;background:greenyellow"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" Style=""color:red;background:greenyellow"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" Style=""color:red;background:greenyellow"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>Selected Option Class</BitLabel>
    <BitBreadGroup SelectedOptionClass=""custom-selected-option"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
<div>
    <BitLabel>Selected Option Style</BitLabel>
    <BitBreadGroup SelectedOptionStyle=""color:red; background:lightgreen;"">
        <BitBreadOption Text=""Option 1"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 2"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 3"" Href=""/components/bread-group"" />
        <BitBreadOption Text=""Option 4"" Href=""/components/bread-group"" IsSelected=""true"" />
    </BitBreadGroup>
</div>
";

    private readonly string example5HTMLCode = @"
<div>
    <BitBreadGroup MaxDisplayedOptions=""3"" OverflowIndex=""2"" SelectedOptionStyle=""color:red; background:lightgreen;"">
        <BitBreadOption Text=""Option 1"" IsSelected=""@(SelectedOptionNumber == 1)"" OnClick=""() => SelectedOptionNumber = 1"" />
        <BitBreadOption Text=""Option 2"" IsSelected=""@(SelectedOptionNumber == 2)"" OnClick=""() => SelectedOptionNumber = 2"" />
        <BitBreadOption Text=""Option 3"" IsSelected=""@(SelectedOptionNumber == 3)"" OnClick=""() => SelectedOptionNumber = 3"" />
        <BitBreadOption Text=""Option 4"" IsSelected=""@(SelectedOptionNumber == 4)"" OnClick=""() => SelectedOptionNumber = 4"" />
        <BitBreadOption Text=""Option 5"" IsSelected=""@(SelectedOptionNumber == 5)"" OnClick=""() => SelectedOptionNumber = 5"" />
        <BitBreadOption Text=""Option 6"" IsSelected=""@(SelectedOptionNumber == 6)"" OnClick=""() => SelectedOptionNumber = 6"" />
    </BitBreadGroup>
</div>
";

    private readonly string example5CSharpCode = @"
private int SelectedOptionNumber;
";

    private readonly string example6HTMLCode = @"
<div>
    <BitBreadGroup MaxDisplayedOptions=""@MaxDisplayedOptions"" OverflowIndex=""@OverflowIndex"">
        @for (int i = 0; i < OptionsCount; i++)
        {
            int index = i + 1;
            <BitBreadOption Text=""@($""Option {index}"")"" IsSelected=""@(CustomizedSelectedOptionNumber == index)"" OnClick=""() => CustomizedSelectedOptionNumber = index"" />
        }
    </BitBreadGroup>
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""() => OptionsCount++"">Add Option</BitButton>
        <BitButton OnClick=""() => OptionsCount--"">Remove Option</BitButton>
    </div>
    <div>
        <BitNumericTextField @bind-Value=""MaxDisplayedOptions"" Step=""@NumericTextFieldStep"" Label=""MaxDisplayedOption"" ShowArrows=""true"" />
        <BitNumericTextField @bind-Value=""OverflowIndex"" Step=""@NumericTextFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";

    private readonly string example6CSharpCode = @"
private uint MaxDisplayedOptions = 3;
private uint OverflowIndex = 2;
private int OptionsCount = 4;
private int CustomizedSelectedOptionNumber = 4;
private uint NumericTextFieldStep = 1;
";
}
