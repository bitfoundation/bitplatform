using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Labels
{
    public partial class BitLabelDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of label, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "For",
                Type = "string",
                DefaultValue = "",
                Description = "This attribute specifies which form element a label is bound to.",
            },
            new ComponentParameter()
            {
                Name = "IsRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the associated field is required or not, it shows a star above of it.",
            },
        };

        private readonly string labelSampleCode = $"<BitLabel>I'm a Label</BitLabel>{Environment.NewLine}" +
              $"<BitLabel IsEnabled='false'>I'm a disabled Label</BitLabel>{Environment.NewLine}" +
              $"<BitLabel IsRequired='true'>I'm a required Label</BitLabel>{Environment.NewLine}" +
              $"<BitLabel For='labels-container__input'>A Label for An Input</BitLabel>{Environment.NewLine}" +
              "<input type='text' name='labels-container__input' id='labels-container__input' />";
    }
}
