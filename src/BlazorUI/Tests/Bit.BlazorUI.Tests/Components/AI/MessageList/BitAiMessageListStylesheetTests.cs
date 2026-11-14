using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessageList;

[TestClass]
public class BitAiMessageListStylesheetTests
{
    // The transcript is the containing block of whatever its messages position - the empty read-aloud status of every
    // answer is a visually hidden live region, which is absolutely positioned. Were the list not positioned, the status
    // of an answer below the fold would be placed against the chat around it, outside the scroll box, where the
    // transcript neither scrolls nor clips it, and whatever scrolls around the chat would get a scrollbar of its own.
    [TestMethod]
    public void TheTranscriptIsTheContainingBlockOfWhatItsMessagesPosition()
    {
        var rules = SourceFiles.GetScssRules(SourceFiles.Read("Bit.BlazorUI.AI", "Components", "MessageList", "BitAiMessageList.scss"));

        var root = rules.Single(rule => rule.Ancestors.Count == 0 && rule.Header == ".bit-aimsl");
        CollectionAssert.Contains(root.Declarations.ToArray(), "position: relative");
    }

    // A row of actions used on its own, outside a message list, keeps its visually hidden read-aloud status in place too.
    [TestMethod]
    public void ARowOfActionsIsTheContainingBlockOfItsReadAloudStatus()
    {
        var rules = SourceFiles.GetScssRules(SourceFiles.Read("Bit.BlazorUI.AI", "Components", "MessageActions", "BitAiMessageActions.scss"));

        var root = rules.Single(rule => rule.Ancestors.Count == 0 && rule.Header == ".bit-aimac");
        CollectionAssert.Contains(root.Declarations.ToArray(), "position: relative");
    }
}
