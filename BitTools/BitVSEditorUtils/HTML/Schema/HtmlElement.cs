using System.Collections.Generic;

namespace BitVSEditorUtils.HTML.Schema
{
    public class HtmlElement : IHtmlItem
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }

        public List<HtmlAttribute> Attributes { get; set; } = new List<HtmlAttribute> { };
    }
}
