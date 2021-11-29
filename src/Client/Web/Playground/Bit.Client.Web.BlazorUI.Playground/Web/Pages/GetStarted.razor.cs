using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages
{
    public partial class GetStarted
    {
        private List<SideRailItem> items { get; set; } = new()
        {
            new SideRailItem()
            {
                Title = "Getting Started",
                Id = "getSartedSection"
            },
            new SideRailItem()
            {
                Title = "Install",
                Id = "installationSection"
            },
            new SideRailItem()
            {
                Title = "Import the namespace",
                Id = "importNamespaceSection"
            },
            new SideRailItem()
            {
                Title = "Include styles",
                Id = "includeStyleSection"
            },
            new SideRailItem()
            {
                Title = "Include the script file",
                Id = "includeScriptSection"
            }
        };
    }
}
