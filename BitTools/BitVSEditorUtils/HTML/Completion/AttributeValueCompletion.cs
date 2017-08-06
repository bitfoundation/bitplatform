using System.Collections.Generic;
using System.Linq;
using BitVSEditorUtils.HTML.Schema;
using Microsoft.Html.Editor.Completion;
using Microsoft.Html.Editor.Completion.Def;
using Microsoft.VisualStudio.Utilities;

namespace BitVSEditorUtils.HTML.Completion
{
    [HtmlCompletionProvider(CompletionTypes.Values, "*", "*")]
    [ContentType("htmlx")]
    public class AttributeValueCompletion : CompletionBase
    {
        public override string CompletionType => CompletionTypes.Values;

        public override IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            string tagName = context.Element.Name.ToLowerInvariant();
            string attrName = context.Attribute.Name.ToLowerInvariant();

            List<HtmlAttribute> all = HtmlElementsContainer.Elements.ExtendedSingle("Looking for * tag", e => e.Name == "*").Attributes.ToList();

            HtmlElement element = HtmlElementsContainer.Elements.ExtendedSingleOrDefault($"Looking for {tagName} tag", e => e.Name == tagName);

            if (element?.Attributes != null)
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

            HtmlAttribute attr = attributes.ExtendedSingleOrDefault($"Looking for attribute {attrName}", a => a.Name == attrName);

            return AddAttributeValues(context, attr?.Values);
        }
    }
}
