using System.Collections.Generic;
using System.Linq;
using Microsoft.Html.Editor.Completion;
using Microsoft.Html.Editor.Completion.Def;
using Microsoft.VisualStudio.Utilities;

namespace BitVSEditorUtils.Html
{
    [HtmlCompletionProvider(CompletionTypes.Values, "*", "*")]
    [ContentType("htmlx")]
    public class AttributeValueCompletion : CompletionBase
    {
        public override string CompletionType
        {
            get { return CompletionTypes.Values; }
        }

        public override IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            List<HtmlCompletion> list = new List<HtmlCompletion>();
            string tagName = context.Element.Name.ToLowerInvariant();
            string attrName = context.Attribute.Name.ToLowerInvariant();

            List<HtmlAttribute> all = HtmlElementsContainer.Elements.Single(e => e.Name == "*").Attributes.ToList();

            HtmlElement element = HtmlElementsContainer.Elements.SingleOrDefault(e => e.Name == tagName);

            if (element != null && element.Attributes != null)
                all.AddRange(element.Attributes);

            List<HtmlAttribute> attributes = new List<HtmlAttribute>();

            foreach (HtmlAttribute attribute in all)
            {
                if (!string.IsNullOrEmpty(attribute.Require))
                {
                    if (context.Element.GetAttribute(attribute.Require) != null)
                        attributes.Add(attribute);
                }
                else
                {
                    attributes.Add(attribute);
                }
            }

            HtmlAttribute attr = attributes.SingleOrDefault(a => a.Name == attrName);

            return AddAttributeValues(context, attr?.Values);
        }
    }
}
