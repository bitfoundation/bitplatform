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
                Id = "get-started-section"
            },
            new SideRailItem()
            {
                Title = "First Local Development",
                Id = "first-development-section"
            },
            new SideRailItem()
            {
                Title = "Installation",
                Id = "installation-section"
            },
            new SideRailItem()
            {
                Title = "Create a New Blazor Web",
                Id = "web-assembly-section"
            },
            new SideRailItem()
            {
                Title = "Development & Debugging",
                Id = "debugging-section"
            },
            new SideRailItem()
            {
                Title = "Building & Deployment",
                Id = "building-section"
            },
            new SideRailItem()
            {
                Title = "Install Bit Design Blazor",
                Id = "install-blazor-section"
            },
            new SideRailItem()
            {
                Title = "Register Dependencies",
                Id = "register-dependency-section"
            },
            new SideRailItem()
            {
                Title = "Import Styles",
                Id = "import-style-section"
            }
        };
    }
}
