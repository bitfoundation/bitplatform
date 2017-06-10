using System.Collections.Generic;

namespace BitVSEditorUtils.HTML.Schema
{
    public class HtmlAttribute : IHtmlItem
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }

        public string Require { get; set; }

        public IEnumerable<string> Values { get; set; }
    }
}