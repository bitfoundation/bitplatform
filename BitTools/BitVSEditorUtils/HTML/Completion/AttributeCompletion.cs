using System.Collections.Generic;
using System.Linq;
using BitVSEditorUtils.HTML.Schema;
using Microsoft.Html.Editor.Completion;
using Microsoft.Html.Editor.Completion.Def;
using Microsoft.VisualStudio.Utilities;

namespace BitVSEditorUtils.HTML.Completion
{
    [HtmlCompletionProvider(CompletionTypes.Attributes, "*")]
    [ContentType("htmlx")]
    public class AttributeCompletion : CompletionBase
    {
        public override string CompletionType => CompletionTypes.Attributes;

        public override IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            string tagName = context.Element.Name.ToLowerInvariant();

            List<HtmlAttribute> all = HtmlElementsContainer.Elements.ExtendedSingle("Looking for * tag", e => e.Name == "*").Attributes.ToList();

            HtmlElement element = HtmlElementsContainer.Elements.ExtendedSingleOrDefault($"Looking for tag {tagName}" ,e => e.Name == tagName);

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

            return AddEntries(context, attributes);
        }
    }
}
