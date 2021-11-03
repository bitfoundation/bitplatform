using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Modal
{
    public partial class BitModalDemo
    {
        private bool IsOpen = false;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "isAlert",
                Type = "bool?",
                DefaultValue = "",
                Description = "Determines the ARIA role of the dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless.",
            },
            new ComponentParameter()
            {
                Name = "isBlocking",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the dialog can be light dismissed by clicking outside the dialog (on the overlay).",
            },
            new ComponentParameter()
            {
                Name = "isModeless",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the dialog). if true: IsBlocking is ignored, there will be no overlay.",
            },
            new ComponentParameter()
            {
                Name = "isOpen",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the dialog is displayed.",
            },
            new ComponentParameter()
            {
                Name = "childContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of Modal, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "onDismiss",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "A callback function for when the Modal is dismissed light dismiss, before the animation completes.",
            },
            new ComponentParameter()
            {
                Name = "subtitleAriaId",
                Type = "string",
                DefaultValue = "string.Empty",
                Description = "ARIA id for the subtitle of the Modal, if any.",
            },
            new ComponentParameter()
            {
                Name = "titleAriaId",
                Type = "string",
                DefaultValue = "string.Empty",
                Description = "ARIA id for the title of the Modal, if any.",
            },
        };
    }
}
