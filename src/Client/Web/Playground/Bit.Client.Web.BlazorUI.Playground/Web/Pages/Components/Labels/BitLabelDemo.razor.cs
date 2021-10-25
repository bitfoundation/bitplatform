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
                Name = "childContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of label, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "for",
                Type = "string",
                DefaultValue = "",
                Description = "This attribute specifies which form element a label is bound to.",
            },
            new ComponentParameter()
            {
                Name = "isRequired",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the associated field is required or not, it shows a star above of it.",
            },
        };
    }
}
