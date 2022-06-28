using System.Collections.Generic;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Models
{
    public class EnumParameter
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<EnumItem> EnumList { get; set; }
    }

    public class EnumItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
}
