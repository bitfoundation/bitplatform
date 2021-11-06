﻿using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Spinner
{
    public partial class BitSpinnerDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AriaLive",
                Type = "BitSpinnerAriaLive",
                LinkType = LinkType.Link,
                Href= "#spinnerAriaLive-enum",
                DefaultValue = "BitSpinnerAriaLive.Polite",
                Description = "Politeness setting for label update announcement.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "The label to show next to the spinner. Label updates will be announced to the screen readers.",
            },
            new ComponentParameter()
            {
                Name = "LabelPosition",
                Type = "BitSpinnerLabelPosition",
                LinkType = LinkType.Link,
                Href = "#spinnerLabelPosition-enum",
                DefaultValue = "BitSpinnerLabelPosition.Top",
                Description = "The position of the label in regards to the spinner animation.",
            },
            new ComponentParameter()
            {
                Name = "Size",
                Type = "BitSpinnerSize",
                LinkType = LinkType.Link,
                Href = "#spinnerSize-enum",
                DefaultValue = "BitSpinnerSize.Medium",
                Description = "The size of spinner to render.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "spinnerAriaLive-enum",
                Title = "BitSpinnerAriaLive Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Assertive",
                        Description="",
                        Value="Assertive = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Polite",
                        Description="",
                        Value="Polite = 1",
                    },
                    new EnumItem()
                    {
                        Name= "Off",
                        Description="",
                        Value="Off = 2",
                    },
                },
            },
            new EnumParameter()
            {
                Id = "spinnerLabelPosition-enum",
                Title = "BitSpinnerLabelPosition Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Top",
                        Description="The label shows on the top of the spinner.",
                        Value="Top = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Right",
                        Description="The label shows on the right side of the spinner.",
                        Value="Right = 1",
                    },
                    new EnumItem()
                    {
                        Name= "Bottom",
                        Description="The label shows on the bottom of the spinner.",
                        Value="Bottom = 2",
                    },
                    new EnumItem()
                    {
                        Name= "Left",
                        Description="The label shows on the left side of the spinner.",
                        Value="Left = 3",
                    },
                }
            },
            new EnumParameter()
            {
                Id = "spinnerSize-enum",
                Title = "BitSpinnerSize Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Medium",
                        Description="20px Spinner diameter.",
                        Value="Medium = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Large",
                        Description="28px Spinner diameter.",
                        Value="Large = 1",
                    },
                    new EnumItem()
                    {
                        Name= "Small",
                        Description="16px Spinner diameter.",
                        Value="Small = 2",
                    },
                    new EnumItem()
                    {
                        Name= "XSmall",
                        Description="12px Spinner diameter.",
                        Value="XSmall = 3",
                    },
                },
            },
        };
    }
}
