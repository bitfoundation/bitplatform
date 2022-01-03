using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitCompoundButtonDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AllowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the compound button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "AriaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the compound button for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "AriaHidden",
                Type = "bool",
                DefaultValue = "false",
                Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element.",
            },
            new ComponentParameter()
            {
                Name = "ButtonStyle",
                Type = "BitButtonStyle",
                LinkType = LinkType.Link,
                Href = "#button-style-enum",
                DefaultValue = "BitButtonStyle.Primary",
                Description = "The style of compound button, Possible values: Primary | Standard",
            },
            new ComponentParameter()
            {
                Name = "Href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to, if provided, compound button renders as an anchor.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the compound button clicked.",
            },
            new ComponentParameter()
            {
                Name = "SecondaryText",
                Type = "string",
                DefaultValue = "",
                Description = "Description of the action compound button takes.",
            },
            new ComponentParameter()
            {
                Name = "Target",
                Type = "string",
                DefaultValue = "",
                Description = "If Href provided, specifies how to open the link.",
            },
            new ComponentParameter()
            {
                Name = "Text",
                Type = "string",
                DefaultValue = "",
                Description = "The text of compound button.",
            },
            new ComponentParameter()
            {
                Name = "Title",
                Type = "string",
                DefaultValue = "",
                Description = "The title to show when the mouse is placed on the compound button.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "button-style-enum",
                Title = "BitButtonStyle Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Primary",
                        Description="The button with white text on a blue background.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Standard",
                        Description="The button with black text on a white background.",
                        Value="1",
                    }
                }
            }
        };

        private readonly string compoundButtonSampleCode = $"<BitCompoundButton IsEnabled='true' Text='Primary' AriaLabel='Detailed label used for screen reader.' SecondaryText='This Button is compound button'></BitCompoundButton>{Environment.NewLine}" +
             $"<BitCompoundButton IsEnabled='true' Text='Primary' AriaHidden='true' SecondaryText='This Button is compound button' ButtonStyle='BitButtonStyle.Standard'></BitCompoundButton>{Environment.NewLine}" +
             $"<BitCompoundButton IsEnabled='false' Text='Disabled' AriaDescription='Detailed description used for screen reader.' SecondaryText='This Button is a disabled compound button' ButtonStyle='BitButtonStyle.Standard'></BitCompoundButton>";

        private readonly string styleAndClassSampleCode = $"<BitCompoundButton Style='background-color:#32385b;border-color:#32385b' Text='Styled' SecondaryText='This is styled compound button'></BitCompoundButton>{Environment.NewLine}" +
                 $"<BitCompoundButton Class='custom-compound-button' Text='Classed' SecondaryText='This is classed compound button' ButtonStyle='BitButtonStyle.Standard'></BitCompoundButton>{Environment.NewLine}" +
                 $"<style>{Environment.NewLine}" +
                 $".custom-compound-button {{ {Environment.NewLine}" +
                 $"width:230px;{Environment.NewLine}" +
                 $"font-family:Axiforma;{Environment.NewLine}" +
                 $"font-size:16px;{Environment.NewLine}" +
                 $"color:white;{Environment.NewLine}" +
                 $"background-color:#2F455A;{Environment.NewLine}" +
                 $"border-color:#2F455A;{Environment.NewLine}" +
                 $"}} {Environment.NewLine}" +
                 $"</style>";

        private readonly string visibilitySampleCode = $"<BitCompoundButton Visibility='BitComponentVisibility.Visible' Text='Visible' SecondaryText='This Button is a visible compound button'></BitCompoundButton>{Environment.NewLine}" +
                 $"<BitCompoundButton Visibility='BitComponentVisibility.Hidden' Text='Hidden' SecondaryText='This Button is a hidden compound button'></BitCompoundButton>{Environment.NewLine}" +
                 $"<BitCompoundButton Visibility='BitComponentVisibility.Collapsed' Text='Collapsed' SecondaryText='This Button is a collapsed compound button'></BitCompoundButton>";

        private readonly string ariasSampleCode = $"<BitCompoundButton AriaDescription='Detailed description used for screen reader' Text='AriaDescription' SecondaryText='This is a compound button with aria description' ButtonStyle='BitButtonStyle.Standard'></BitCompoundButton>{Environment.NewLine}" +
                $"<BitCompoundButton AriaHidden='true' Style='height: 85px;' Text='AriaDescription' SecondaryText='This is a compound button with aria description'></BitCompoundButton>";

        private readonly string compoundButtonLikeAnchorSampleCode = $"<BitCompoundButton Target='_blank' Href='https://github.com/bitfoundation/bitframework' Text='Open the site' SecondaryText='Open Bit Foundation In New Tab'></BitCompoundButton>{Environment.NewLine}" +
                 $"<BitCompoundButton ButtonStyle='BitButtonStyle.Standard' Href='https://github.com/bitfoundation/bitframework' Text='Open the site' SecondaryText='Go To Bit Foundation'></BitCompoundButton>{Environment.NewLine}" +
                 $"<BitCompoundButton Href='https://github.com/bitfoundation/bitframework' IsEnabled='false' Text='Open the site' SecondaryText='Go To Bit Foundation'></BitCompoundButton>";
    }
}
