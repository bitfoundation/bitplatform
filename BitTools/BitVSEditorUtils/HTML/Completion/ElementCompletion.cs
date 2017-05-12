using System.Collections.Generic;
using System.Linq;
using Microsoft.Html.Editor.Completion;
using Microsoft.Html.Editor.Completion.Def;
using Microsoft.VisualStudio.Utilities;

namespace BitVSEditorUtils.Html
{
    [HtmlCompletionProvider(CompletionTypes.Children, "*")]
    [ContentType("htmlx")]
    public class ListCompletion : CompletionBase
    {
        public override string CompletionType
        {
            get { return CompletionTypes.Children; }
        }

        public override IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            return AddEntries(context, HtmlElementsContainer.Elements.Where(elem => elem.Type != "existing"));
        }
    }
}
