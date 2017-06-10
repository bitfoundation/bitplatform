using System.Collections.Generic;
using System.Linq;
using BitVSEditorUtils.HTML.Schema;
using Microsoft.Html.Editor.Completion;
using Microsoft.Html.Editor.Completion.Def;
using Microsoft.VisualStudio.Utilities;

namespace BitVSEditorUtils.HTML.Completion
{
    [HtmlCompletionProvider(CompletionTypes.Children, "*")]
    [ContentType("htmlx")]
    public class ListCompletion : CompletionBase
    {
        public override string CompletionType => CompletionTypes.Children;

        public override IList<HtmlCompletion> GetEntries(HtmlCompletionContext context)
        {
            return AddEntries(context, HtmlElementsContainer.Elements.Where(elem => elem.Type != "existing"));
        }
    }
}
