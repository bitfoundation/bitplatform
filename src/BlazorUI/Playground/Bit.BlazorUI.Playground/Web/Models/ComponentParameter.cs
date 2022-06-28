using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase
{
    public class ComponentParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }
        public LinkType LinkType { get; set; }
        public string Href { get; set; }
    }

    public class ComponentSubParameter
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<ComponentParameter> Parameters {  get; set;}
    }

    public enum LinkType
    {
        Normal,
        Link
    }
}
