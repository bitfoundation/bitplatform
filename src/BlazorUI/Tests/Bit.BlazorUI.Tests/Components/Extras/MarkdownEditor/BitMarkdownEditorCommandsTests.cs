using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

[TestClass]
public class BitMarkdownEditorCommandsTests
{
    [TestMethod]
    public void BoldShouldWrapSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "hello world", 0, 5);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("**hello** world", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
        Assert.AreEqual(7, result.SelectionEnd);
    }

    [TestMethod]
    public void BoldShouldUnwrapAlreadyWrappedSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "**hello** world", 2, 7);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("hello world", result.Text);
        Assert.AreEqual(0, result.SelectionStart);
        Assert.AreEqual(5, result.SelectionEnd);
    }

    [TestMethod]
    public void BoldShouldInsertPlaceholderOnEmptySelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "", 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("**bold text**", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
        Assert.AreEqual(11, result.SelectionEnd);
    }

    [TestMethod]
    public void ItalicShouldWrapSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "hello", 0, 5);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("*hello*", result.Text);
    }

    [TestMethod]
    public void ItalicShouldUnwrapAlreadyWrappedSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "*hello* world", 1, 6);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("hello world", result.Text);
        Assert.AreEqual(0, result.SelectionStart);
        Assert.AreEqual(5, result.SelectionEnd);
    }

    [TestMethod]
    public void ItalicShouldUnwrapMarkersCapturedInsideSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "*hello* world", 0, 7);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("hello world", result.Text);
    }

    [TestMethod]
    public void ItalicShouldWrapSelectionInsideBoldInsteadOfUnwrapping()
    {
        // The surrounding '*' belong to the bold '**' markers, so italic must wrap,
        // not strip a star from each side.
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "**bold**", 2, 6);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("***bold***", result.Text);
    }

    [TestMethod]
    public void ItalicShouldNotUnwrapBoldMarkersCapturedInsideSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "**bold**", 0, 8);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("***bold***", result.Text);
    }

    [TestMethod]
    public void ItalicShouldUnwrapInsideBoldItalic()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "***both***", 3, 7);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("**both**", result.Text);
    }

    [TestMethod]
    public void BoldShouldUnwrapInsideBoldItalic()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "***both***", 3, 7);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("*both*", result.Text);
    }

    [TestMethod]
    public void StrikethroughShouldWrapSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Strikethrough, "hello", 0, 5);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("~~hello~~", result.Text);
    }

    [TestMethod]
    public void StrikethroughShouldUnwrapAlreadyWrappedSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Strikethrough, "~~hello~~", 2, 7);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("hello", result.Text);
    }

    [TestMethod]
    public void InlineCodeShouldWrapSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.InlineCode, "code", 0, 4);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("`code`", result.Text);
    }

    [TestMethod]
    public void InlineCodeShouldUnwrapAlreadyWrappedSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.InlineCode, "`code`", 1, 5);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("code", result.Text);
    }

    [TestMethod]
    public void HeadingShouldPrefixLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading2, "title", 2, 2);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("## title", result.Text);
    }

    [TestMethod]
    public void HeadingShouldToggleOffSameLevel()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading2, "## title", 4, 4);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("title", result.Text);
    }

    [TestMethod]
    public void HeadingShouldSwitchLevel()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading1, "## title", 4, 4);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("# title", result.Text);
    }

    [TestMethod]
    public void QuoteShouldPrefixSelectedLines()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Quote, "a\nb", 0, 3);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("> a\n> b", result.Text);
    }

    [TestMethod]
    public void QuoteShouldToggleOffPrefixedLines()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Quote, "> a\n> b", 0, 7);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a\nb", result.Text);
    }

    [TestMethod]
    public void UnorderedListShouldToggleSelectedLines()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.UnorderedList, "a\nb", 0, 3);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("- a\n- b", result.Text);

        result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.UnorderedList, result.Text, 0, result.Text.Length);

        Assert.AreEqual("a\nb", result.Text);
    }

    [TestMethod]
    public void OrderedListShouldNumberSelectedLines()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.OrderedList, "a\nb\nc", 0, 5);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("1. a\n2. b\n3. c", result.Text);
    }

    [TestMethod]
    public void TaskListShouldToggleSelectedLines()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TaskList, "a\nb", 0, 3);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("- [ ] a\n- [ ] b", result.Text);
    }

    [TestMethod]
    public void CodeBlockShouldFenceSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.CodeBlock, "var a = 1;", 0, 10);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("```\nvar a = 1;\n```", result.Text);
    }

    [TestMethod]
    public void CodeBlockShouldStartOnItsOwnLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.CodeBlock, "text", 4, 4);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("text\n\n```\n\n```", result.Text);
        // caret on the empty line between the fences
        Assert.AreEqual(10, result.SelectionStart);
        Assert.AreEqual(10, result.SelectionEnd);
    }

    [TestMethod]
    public void CodeBlockShouldSeparateFromSurroundingText()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.CodeBlock, "ab", 1, 1);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a\n\n```\n\n```\nb", result.Text);
    }

    [TestMethod]
    public void CodeBlockShouldFenceSelectionOnItsOwnLines()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.CodeBlock, "before\ncode\nafter", 7, 11);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("before\n\n```\ncode\n```\nafter", result.Text);
        Assert.AreEqual("code", result.Text[result.SelectionStart..result.SelectionEnd]);
    }

    [TestMethod]
    public void LinkShouldUseSelectionAsLabel()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Link, "bit", 0, 3);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("[bit](url)", result.Text);
        // the "url" placeholder is selected
        Assert.AreEqual("url", result.Text[result.SelectionStart..result.SelectionEnd]);
    }

    [TestMethod]
    public void ImageShouldInsertPlaceholder()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Image, "", 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("![alt](url)", result.Text);
    }

    [TestMethod]
    public void TableShouldInsertTemplate()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Table, "", 0, 0);

        Assert.IsTrue(result.Handled);
        StringAssert.StartsWith(result.Text, "| Column 1 | Column 2 |");
        Assert.AreEqual("Column 1", result.Text[result.SelectionStart..result.SelectionEnd]);
    }

    [TestMethod]
    public void HorizontalRuleShouldInsertAfterBlankLine()
    {
        // a "---" right below a text line would be parsed as a setext heading,
        // so a true blank line must separate them.
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.HorizontalRule, "text", 4, 4);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("text\n\n---\n", result.Text);
    }

    [TestMethod]
    public void HorizontalRuleShouldAddBlankLineAtLineStartBelowText()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.HorizontalRule, "text\n", 5, 5);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("text\n\n---\n", result.Text);
    }

    [TestMethod]
    public void HorizontalRuleShouldNotAddBlankLineWhenAlreadyPresent()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.HorizontalRule, "text\n\n", 6, 6);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("text\n\n---\n", result.Text);
    }

    [TestMethod]
    public void HorizontalRuleShouldNotAddBlankLineAtDocumentStart()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.HorizontalRule, "", 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("---\n", result.Text);
    }

    [TestMethod]
    public void IndentShouldInsertIndentUnitAtCaret()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "a", 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("  a", result.Text);
    }

    [TestMethod]
    public void IndentShouldIndentSelectedLines()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "a\nb", 0, 3);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("  a\n  b", result.Text);
    }

    [TestMethod]
    public void OutdentShouldRemoveOneIndentLevel()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Outdent, "  a\n  b", 0, 7);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a\nb", result.Text);
    }

    [TestMethod]
    public void NewLineShouldContinueUnorderedList()
    {
        const string text = "- item";
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.NewLine, text, text.Length, text.Length);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("- item\n- ", result.Text);
    }

    [TestMethod]
    public void NewLineShouldContinueOrderedListWithIncrement()
    {
        const string text = "1. item";
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.NewLine, text, text.Length, text.Length);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("1. item\n2. ", result.Text);
    }

    [TestMethod]
    public void NewLineShouldContinueTaskList()
    {
        const string text = "- [x] done";
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.NewLine, text, text.Length, text.Length);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("- [x] done\n- [ ] ", result.Text);
    }

    [TestMethod]
    public void NewLineShouldClearEmptyListItem()
    {
        const string text = "- item\n- ";
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.NewLine, text, text.Length, text.Length);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("- item\n", result.Text);
    }

    [TestMethod]
    public void ApplyShouldClampOutOfRangeSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "ab", 5, 100);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("ab**bold text**", result.Text);
    }

    [TestMethod]
    public void ApplyShouldNormalizeReversedSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "hello", 5, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("**hello**", result.Text);
    }

    [TestMethod]
    public void Heading4ShouldPrefixLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading4, "title", 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("#### title", result.Text);
    }

    [TestMethod]
    public void Heading6ShouldToggleOffSameLevel()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading6, "###### title", 8, 8);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("title", result.Text);
    }

    [TestMethod]
    public void SuperscriptShouldWrapSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Superscript, "2", 0, 1);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("^2^", result.Text);
    }

    [TestMethod]
    public void SubscriptShouldWrapSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Subscript, "2", 0, 1);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("~2~", result.Text);
    }

    [TestMethod]
    public void ClearFormattingShouldStripInlineMarkers()
    {
        const string text = "**bold** and *italic* and `code`";
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.ClearFormatting, text, 0, text.Length);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("bold and italic and code", result.Text);
    }

    [TestMethod]
    public void ClearFormattingShouldStripBlockPrefixes()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.ClearFormatting, "## Heading", 0, 10);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("Heading", result.Text);
    }

    [TestMethod]
    public void ClearFormattingShouldStripTaskListPrefix()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.ClearFormatting, "- [ ] task", 0, 10);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("task", result.Text);
    }

    [TestMethod]
    public void NewLineShouldRenumberFollowingOrderedItems()
    {
        // Pressing Enter in the middle of "1. a" / "2. b" inserts a new item and the
        // trailing item is renumbered so the sequence stays consecutive.
        const string text = "1. a\n2. b";
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.NewLine, text, 4, 4);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("1. a\n2. \n3. b", result.Text);
    }

    [TestMethod]
    public void DetectActiveFormatsShouldDetectBoldAndHeading()
    {
        // Caret inside a bold run on a heading line.
        const string text = "## a **bold** b";
        var formats = BitMarkdownEditorCommands.DetectActiveFormats(text, 8, 8);

        Assert.IsTrue(formats.Contains(BitMarkdownEditorCommand.Bold));
        Assert.IsTrue(formats.Contains(BitMarkdownEditorCommand.Heading2));
    }

    [TestMethod]
    public void DetectActiveFormatsShouldDetectSelectionWrappedItalic()
    {
        var formats = BitMarkdownEditorCommands.DetectActiveFormats("*italic*", 0, 8);

        Assert.IsTrue(formats.Contains(BitMarkdownEditorCommand.Italic));
    }

    [TestMethod]
    public void DetectActiveFormatsShouldDetectTaskList()
    {
        var formats = BitMarkdownEditorCommands.DetectActiveFormats("- [ ] task", 8, 8);

        Assert.IsTrue(formats.Contains(BitMarkdownEditorCommand.TaskList));
        Assert.IsFalse(formats.Contains(BitMarkdownEditorCommand.UnorderedList));
    }

    [TestMethod]
    public void DetectActiveFormatsShouldReturnEmptyForPlainText()
    {
        var formats = BitMarkdownEditorCommands.DetectActiveFormats("plain text", 3, 3);

        Assert.AreEqual(0, formats.Count);
    }

    [TestMethod]
    public void DetectActiveFormatsShouldWorkOnASingleLineSlice()
    {
        // The interop script sends only the lines the selection touches, with the offsets
        // rebased on that slice, so the detection has to hold for a bare line too.
        var formats = BitMarkdownEditorCommands.DetectActiveFormats("> **quoted**", 5, 5);

        Assert.IsTrue(formats.Contains(BitMarkdownEditorCommand.Quote));
        Assert.IsTrue(formats.Contains(BitMarkdownEditorCommand.Bold));
    }

    [TestMethod]
    public void MoveLineUpShouldSwapWithThePreviousLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.MoveLineUp, "a\nb\nc", 2, 2);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("b\na\nc", result.Text);
        Assert.AreEqual(0, result.SelectionStart);
        Assert.AreEqual(0, result.SelectionEnd);
    }

    [TestMethod]
    public void MoveLineUpShouldDoNothingOnTheFirstLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.MoveLineUp, "a\nb", 0, 0);

        Assert.IsFalse(result.Handled);
        Assert.AreEqual("a\nb", result.Text);
    }

    [TestMethod]
    public void MoveLineDownShouldSwapWithTheNextLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.MoveLineDown, "a\nb\nc", 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("b\na\nc", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
    }

    [TestMethod]
    public void MoveLineDownShouldDoNothingOnTheLastLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.MoveLineDown, "a\nb", 2, 2);

        Assert.IsFalse(result.Handled);
        Assert.AreEqual("a\nb", result.Text);
    }

    [TestMethod]
    public void MoveLineShouldCarryAWholeSelectedBlock()
    {
        // "b\nc" selected, moved under "d".
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.MoveLineDown, "a\nb\nc\nd", 2, 5);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a\nd\nb\nc", result.Text);
        Assert.AreEqual(4, result.SelectionStart);
        Assert.AreEqual(7, result.SelectionEnd);
    }

    [TestMethod]
    public void DuplicateLineShouldCopyTheLineBelowItself()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.DuplicateLine, "a\nb", 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a\na\nb", result.Text);
        // The selection lands on the copy so a second run duplicates the newest one.
        Assert.AreEqual(2, result.SelectionStart);
    }

    [TestMethod]
    public void DuplicateLineShouldCopyEveryTouchedLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.DuplicateLine, "a\nb\nc", 0, 3);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a\nb\na\nb\nc", result.Text);
    }

    [TestMethod]
    public void DeleteLineShouldRemoveTheLineAndItsNewline()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.DeleteLine, "a\nb\nc", 2, 2);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a\nc", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
        Assert.AreEqual(2, result.SelectionEnd);
    }

    [TestMethod]
    public void DeleteLineShouldEatThePrecedingNewlineOnTheLastLine()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.DeleteLine, "a\nb", 2, 2);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("a", result.Text);
        Assert.AreEqual(1, result.SelectionStart);
    }

    [TestMethod]
    public void DeleteLineShouldEmptyASingleLineDocument()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.DeleteLine, "only", 2, 2);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("", result.Text);
        Assert.AreEqual(0, result.SelectionStart);
    }

    [TestMethod]
    public void LinkShouldUseASelectedUrlAsTheTarget()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Link, "https://bitplatform.dev", 0, 23);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("[text](https://bitplatform.dev)", result.Text);
        // The caret lands on the label, which is the part still to be typed.
        Assert.AreEqual(1, result.SelectionStart);
        Assert.AreEqual(5, result.SelectionEnd);
    }

    [TestMethod]
    public void ImageShouldUseASelectedUrlAsTheSource()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Image, "https://a.dev/b.png", 0, 19);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("![alt](https://a.dev/b.png)", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
        Assert.AreEqual(5, result.SelectionEnd);
    }

    [TestMethod]
    public void LinkShouldStillUsePlainSelectionsAsTheLabel()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Link, "bit platform", 0, 12);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("[bit platform](url)", result.Text);
    }

    [TestMethod]
    public void ClearFormattingShouldUnwrapLinksAndImages()
    {
        const string text = "see [the **docs**](https://bit.dev) and ![logo](a.png)";
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.ClearFormatting, text, 0, text.Length);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("see the docs and logo", result.Text);
    }

    [TestMethod]
    public void ApplyShouldNotHandleAnUndefinedCommand()
    {
        var result = BitMarkdownEditorCommands.Apply((BitMarkdownEditorCommand)999, "text", 0, 4);

        Assert.IsFalse(result.Handled);
        Assert.AreEqual("text", result.Text);
    }

    [TestMethod]
    public void TableShouldFollowTheConfiguredSize()
    {
        var options = new BitMarkdownEditorCommandOptions { TableColumns = 3, TableRows = 2 };
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Table, "", 0, 0, options);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual(
            "| Column 1 | Column 2 | Column 3 |\n" +
            "| -------- | -------- | -------- |\n" +
            "| Cell     | Cell     | Cell     |\n" +
            "| Cell     | Cell     | Cell     |\n", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
        Assert.AreEqual(10, result.SelectionEnd);
    }

    [TestMethod]
    public void TableShouldClampAnImpossibleSize()
    {
        var options = new BitMarkdownEditorCommandOptions { TableColumns = 0, TableRows = -3 };
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Table, "", 0, 0, options);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("| Column 1 |\n| -------- |\n", result.Text);
    }

    [TestMethod]
    public void ApplyShouldFallBackToTheDefaultIndentUnit()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "text", 0, 0, string.Empty);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("  text", result.Text);
    }

    [TestMethod]
    public void DetectActiveFormatsShouldTellSubscriptFromStrikethrough()
    {
        var strike = BitMarkdownEditorCommands.DetectActiveFormats("~~gone~~", 4, 4);
        Assert.IsTrue(strike.Contains(BitMarkdownEditorCommand.Strikethrough));
        Assert.IsFalse(strike.Contains(BitMarkdownEditorCommand.Subscript));

        var sub = BitMarkdownEditorCommands.DetectActiveFormats("H~2~O", 3, 3);
        Assert.IsTrue(sub.Contains(BitMarkdownEditorCommand.Subscript));
        Assert.IsFalse(sub.Contains(BitMarkdownEditorCommand.Strikethrough));

        var sup = BitMarkdownEditorCommands.DetectActiveFormats("x^2^", 3, 3);
        Assert.IsTrue(sup.Contains(BitMarkdownEditorCommand.Superscript));
    }

    [TestMethod]
    public void ApplyShouldTreatNullTextAsEmpty()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, null!, 0, 0);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("**bold text**", result.Text);
    }

    [TestMethod]
    public void HeadingShouldKeepACaretWhereItWas()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading1, "hello", 5, 5);

        Assert.AreEqual("# hello", result.Text);
        Assert.AreEqual(7, result.SelectionStart);
        Assert.AreEqual(7, result.SelectionEnd);
    }

    [TestMethod]
    public void HeadingShouldCarryACaretAtTheLineStartPastTheNewMarker()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading2, "hello", 0, 0);

        Assert.AreEqual("## hello", result.Text);
        Assert.AreEqual(3, result.SelectionStart);
        Assert.AreEqual(3, result.SelectionEnd);
    }

    [TestMethod]
    public void HeadingShouldKeepACaretOnTheRightLineOfABlock()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Heading1, "one\ntwo\nthree", 6, 6);

        Assert.AreEqual("one\n# two\nthree", result.Text);
        // "one\n# tw|o": the caret kept its place inside the second line.
        Assert.AreEqual(8, result.SelectionStart);
        Assert.AreEqual(8, result.SelectionEnd);
    }

    [TestMethod]
    public void QuoteShouldKeepACaretWhereItWas()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Quote, "hello", 3, 3);

        Assert.AreEqual("> hello", result.Text);
        Assert.AreEqual(5, result.SelectionStart);
        Assert.AreEqual(5, result.SelectionEnd);
    }

    [TestMethod]
    public void OutdentShouldKeepACaretWhereItWas()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Outdent, "  hello", 4, 4);

        Assert.AreEqual("hello", result.Text);
        Assert.AreEqual(2, result.SelectionStart);
        Assert.AreEqual(2, result.SelectionEnd);
    }

    [TestMethod]
    public void IndentShouldNestAListItemFromACaretInsideIt()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "- item", 3, 3);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("  - item", result.Text);
        Assert.AreEqual(5, result.SelectionStart);
        Assert.AreEqual(5, result.SelectionEnd);
    }

    [TestMethod]
    public void IndentShouldNestAnOrderedItemATaskAndAQuoteFromACaret()
    {
        Assert.AreEqual("  1. one", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "1. one", 4, 4).Text);
        Assert.AreEqual("  - [ ] todo", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "- [ ] todo", 8, 8).Text);
        Assert.AreEqual("  > quoted", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "> quoted", 4, 4).Text);
    }

    [TestMethod]
    public void IndentShouldStillInsertAtTheCaretInPlainText()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "hello", 2, 2);

        Assert.AreEqual("he  llo", result.Text);
        Assert.AreEqual(4, result.SelectionStart);
        Assert.AreEqual(4, result.SelectionEnd);
    }

    [TestMethod]
    public void ClearFormattingShouldStillClearTheWholeLineFromACaret()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.ClearFormatting, "# **big** title", 4, 4);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual("big title", result.Text);
        // The marker the caret sat in is gone, so it lands on the nearest surviving text.
        Assert.AreEqual(3, result.SelectionStart);
        Assert.AreEqual(3, result.SelectionEnd);
    }

    [TestMethod]
    public void ListToggleShouldStillSelectTheBlockForARealSelection()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.UnorderedList, "one\ntwo", 0, 7);

        Assert.AreEqual("- one\n- two", result.Text);
        Assert.AreEqual(0, result.SelectionStart);
        Assert.AreEqual(11, result.SelectionEnd);
    }

    [TestMethod]
    public void DetectActiveFormatsShouldDetectItalicAtABareCaret()
    {
        var italic = BitMarkdownEditorCommands.DetectActiveFormats("an *emphatic* word", 6, 6);

        Assert.IsTrue(italic.Contains(BitMarkdownEditorCommand.Italic));
        Assert.IsFalse(italic.Contains(BitMarkdownEditorCommand.Bold));
    }

    [TestMethod]
    public void DetectActiveFormatsShouldNotReportItalicInsideBold()
    {
        var bold = BitMarkdownEditorCommands.DetectActiveFormats("a **strong** word", 6, 6);

        Assert.IsTrue(bold.Contains(BitMarkdownEditorCommand.Bold));
        Assert.IsFalse(bold.Contains(BitMarkdownEditorCommand.Italic));
    }

    [TestMethod]
    public void DetectActiveFormatsShouldReportBothInsideBoldItalic()
    {
        var both = BitMarkdownEditorCommands.DetectActiveFormats("***loud***", 5, 5);

        Assert.IsTrue(both.Contains(BitMarkdownEditorCommand.Bold));
        Assert.IsTrue(both.Contains(BitMarkdownEditorCommand.Italic));
    }

    [TestMethod]
    public void BoldAndItalicShouldFollowTheConfiguredEmphasisStyle()
    {
        var options = new BitMarkdownEditorCommandOptions
        {
            BoldStyle = BitMarkdownEditorEmphasisStyle.Underscore,
            ItalicStyle = BitMarkdownEditorEmphasisStyle.Underscore
        };

        Assert.AreEqual("__hello__", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "hello", 0, 5, options).Text);
        Assert.AreEqual("_hello_", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "hello", 0, 5, options).Text);
    }

    [TestMethod]
    public void EmphasisShouldUnwrapInTheConfiguredStyle()
    {
        var options = new BitMarkdownEditorCommandOptions
        {
            BoldStyle = BitMarkdownEditorEmphasisStyle.Underscore,
            ItalicStyle = BitMarkdownEditorEmphasisStyle.Underscore
        };

        Assert.AreEqual("hello", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Bold, "__hello__", 2, 7, options).Text);
        Assert.AreEqual("hello", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "_hello_", 1, 6, options).Text);
    }

    [TestMethod]
    public void UnderscoreItalicShouldNotUnwrapUnderscoreBold()
    {
        var options = new BitMarkdownEditorCommandOptions { ItalicStyle = BitMarkdownEditorEmphasisStyle.Underscore };

        // The '_' either side of the selection belongs to the '__' pair, so this wraps rather
        // than unwrapping - exactly the way a lone '*' inside '**' behaves.
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Italic, "__hello__", 2, 7, options);

        Assert.AreEqual("___hello___", result.Text);
    }

    [TestMethod]
    public void ListsShouldFollowTheConfiguredBulletStyle()
    {
        var options = new BitMarkdownEditorCommandOptions { BulletStyle = BitMarkdownEditorBulletStyle.Asterisk };

        Assert.AreEqual("* one\n* two", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.UnorderedList, "one\ntwo", 0, 7, options).Text);
        Assert.AreEqual("* [ ] one", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TaskList, "one", 0, 3, options).Text);

        var plus = new BitMarkdownEditorCommandOptions { BulletStyle = BitMarkdownEditorBulletStyle.Plus };
        Assert.AreEqual("+ one", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.UnorderedList, "one", 0, 3, plus).Text);
    }

    [TestMethod]
    public void ListToggleShouldStillRemoveAnyBulletCharacter()
    {
        var options = new BitMarkdownEditorCommandOptions { BulletStyle = BitMarkdownEditorBulletStyle.Asterisk };

        // The text was not necessarily written in the configured style, so toggling off has to
        // recognise every bullet character rather than only the one it writes.
        Assert.AreEqual("one", BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.UnorderedList, "- one", 2, 5, options).Text);
    }

    [TestMethod]
    public void ClearFormattingShouldStripBothEmphasisSpellings()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.ClearFormatting, "__very__ _plain_ **now** *here*", 0, 30);

        Assert.AreEqual("very plain now here", result.Text);
    }

    [TestMethod]
    public void DetectActiveFormatsShouldFollowTheConfiguredEmphasisStyle()
    {
        var options = new BitMarkdownEditorCommandOptions
        {
            BoldStyle = BitMarkdownEditorEmphasisStyle.Underscore,
            ItalicStyle = BitMarkdownEditorEmphasisStyle.Underscore
        };

        var bold = BitMarkdownEditorCommands.DetectActiveFormats("a __strong__ word", 6, 6, options);
        Assert.IsTrue(bold.Contains(BitMarkdownEditorCommand.Bold));
        Assert.IsFalse(bold.Contains(BitMarkdownEditorCommand.Italic));

        var italic = BitMarkdownEditorCommands.DetectActiveFormats("an _emphatic_ word", 6, 6, options);
        Assert.IsTrue(italic.Contains(BitMarkdownEditorCommand.Italic));
        Assert.IsFalse(italic.Contains(BitMarkdownEditorCommand.Bold));
    }

    private const string Table =
        "| Name | Age |\n" +
        "| ---- | --- |\n" +
        "| Ann  | 30  |\n";

    // The caret sits on "Ann" in the body row.
    private static int TableCaret => Table.IndexOf("Ann");

    [TestMethod]
    public void TableCommandsShouldDoNothingOutsideATable()
    {
        foreach (var command in new[]
        {
            BitMarkdownEditorCommand.TableInsertRowAbove,
            BitMarkdownEditorCommand.TableInsertRowBelow,
            BitMarkdownEditorCommand.TableDeleteRow,
            BitMarkdownEditorCommand.TableInsertColumnBefore,
            BitMarkdownEditorCommand.TableInsertColumnAfter,
            BitMarkdownEditorCommand.TableDeleteColumn,
            BitMarkdownEditorCommand.TableAlignLeft,
            BitMarkdownEditorCommand.TableAlignCenter,
            BitMarkdownEditorCommand.TableAlignRight,
        })
        {
            var result = BitMarkdownEditorCommands.Apply(command, "just a paragraph", 4, 4);

            Assert.IsFalse(result.Handled, command.ToString());
            Assert.AreEqual("just a paragraph", result.Text);
        }
    }

    [TestMethod]
    public void TableCommandsShouldIgnoreAPipeThatIsNotATable()
    {
        // No delimiter row below the first line, so this is prose that happens to carry pipes.
        var text = "a | b\nc | d";

        Assert.IsFalse(BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertRowBelow, text, 0, 0).Handled);
    }

    [TestMethod]
    public void TableInsertRowBelowShouldAddAnEmptyRow()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertRowBelow, Table, TableCaret, TableCaret);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual(
            "| Name | Age |\n" +
            "| ---- | --- |\n" +
            "| Ann  | 30  |\n" +
            "|      |     |\n", result.Text);
        // The caret lands in the first cell of the new row, ready to type.
        Assert.AreEqual(result.SelectionStart, result.SelectionEnd);
        Assert.AreEqual("| Name | Age |\n| ---- | --- |\n| Ann  | 30  |\n| ".Length, result.SelectionStart);
    }

    [TestMethod]
    public void TableInsertRowAboveShouldAddAnEmptyRowAboveTheCaretRow()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertRowAbove, Table, TableCaret, TableCaret);

        Assert.AreEqual(
            "| Name | Age |\n" +
            "| ---- | --- |\n" +
            "|      |     |\n" +
            "| Ann  | 30  |\n", result.Text);
    }

    [TestMethod]
    public void TableInsertRowAboveShouldAddTheFirstBodyRowFromTheHeader()
    {
        // Nothing can go above the header, so it adds the row right below it instead.
        var caret = Table.IndexOf("Name");
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertRowAbove, Table, caret, caret);

        Assert.AreEqual(
            "| Name | Age |\n" +
            "| ---- | --- |\n" +
            "|      |     |\n" +
            "| Ann  | 30  |\n", result.Text);
    }

    [TestMethod]
    public void TableDeleteRowShouldRemoveTheCaretRow()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableDeleteRow, Table, TableCaret, TableCaret);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual(
            "| Name | Age |\n" +
            "| ---- | --- |\n", result.Text);
    }

    [TestMethod]
    public void TableDeleteRowShouldRefuseToRemoveTheHeader()
    {
        var caret = Table.IndexOf("Name");

        Assert.IsFalse(BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableDeleteRow, Table, caret, caret).Handled);
    }

    [TestMethod]
    public void TableInsertColumnAfterShouldAddAColumnAndMoveTheCaretIntoItsHeader()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertColumnAfter, Table, TableCaret, TableCaret);

        Assert.AreEqual(
            "| Name |     | Age |\n" +
            "| ---- | --- | --- |\n" +
            "| Ann  |     | 30  |\n", result.Text);
        Assert.AreEqual("| Name | ".Length, result.SelectionStart);
    }

    [TestMethod]
    public void TableInsertColumnBeforeShouldAddAColumnInFrontOfTheCaretColumn()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertColumnBefore, Table, TableCaret, TableCaret);

        Assert.AreEqual(
            "|     | Name | Age |\n" +
            "| --- | ---- | --- |\n" +
            "|     | Ann  | 30  |\n", result.Text);
    }

    [TestMethod]
    public void TableDeleteColumnShouldRemoveTheCaretColumn()
    {
        var caret = Table.IndexOf("30");
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableDeleteColumn, Table, caret, caret);

        Assert.AreEqual(
            "| Name |\n" +
            "| ---- |\n" +
            "| Ann  |\n", result.Text);
    }

    [TestMethod]
    public void TableDeleteColumnShouldRefuseToRemoveTheLastOne()
    {
        var single = "| Name |\n| ---- |\n| Ann  |\n";
        var caret = single.IndexOf("Ann");

        Assert.IsFalse(BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableDeleteColumn, single, caret, caret).Handled);
    }

    [TestMethod]
    public void TableAlignShouldRewriteOnlyTheCaretColumnsDelimiter()
    {
        var centered = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableAlignCenter, Table, TableCaret, TableCaret);
        Assert.AreEqual(
            "| Name | Age |\n" +
            "| :--: | --- |\n" +
            "| Ann  | 30  |\n", centered.Text);

        var right = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableAlignRight, Table, TableCaret, TableCaret);
        Assert.AreEqual(
            "| Name | Age |\n" +
            "| ---: | --- |\n" +
            "| Ann  | 30  |\n", right.Text);

        var left = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableAlignLeft, Table, TableCaret, TableCaret);
        Assert.AreEqual(
            "| Name | Age |\n" +
            "| :--- | --- |\n" +
            "| Ann  | 30  |\n", left.Text);
    }

    [TestMethod]
    public void TableAlignShouldDoNothingWhenTheColumnAlreadyHasIt()
    {
        var aligned = "| Name | Age |\n| :--- | --- |\n| Ann  | 30  |\n";
        var caret = aligned.IndexOf("Ann");

        Assert.IsFalse(BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableAlignLeft, aligned, caret, caret).Handled);
    }

    [TestMethod]
    public void TableEditsShouldKeepAnExistingAlignmentAndRealignTheSource()
    {
        // Ragged input, a centered second column, and a row that is short of a cell.
        var ragged = "|Name|Age|\n|-|:-:|\n|Annabelle|\n";
        var caret = ragged.IndexOf("Annabelle");

        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertRowBelow, ragged, caret, caret);

        Assert.AreEqual(
            "| Name      | Age |\n" +
            "| --------- | :-: |\n" +
            "| Annabelle |     |\n" +
            "|           |     |\n", result.Text);
    }

    [TestMethod]
    public void TableCommandsShouldOnlyRewriteTheTableAroundTheCaret()
    {
        var document = "intro\n\n" + Table + "\noutro\n";
        var caret = document.IndexOf("Ann");

        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableDeleteRow, document, caret, caret);

        Assert.AreEqual("intro\n\n| Name | Age |\n| ---- | --- |\n\noutro\n", result.Text);
    }

    [TestMethod]
    public void TableCommandsShouldKeepAnEscapedPipeInsideACell()
    {
        var escaped = "| a \\| b | c |\n| ------ | - |\n| d      | e |\n";
        var caret = escaped.IndexOf("d");

        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableInsertRowBelow, escaped, caret, caret);

        Assert.Contains("a \\| b", result.Text);
        Assert.AreEqual(4, result.Text.Split('\n').Length - 1);
    }

    [TestMethod]
    public void TableNextCellShouldSelectTheCellToTheRight()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableNextCell, Table, TableCaret, TableCaret);

        Assert.IsTrue(result.Handled);
        Assert.AreEqual(Table, result.Text);
        // Selected, not merely reached, so typing replaces what is in the cell.
        Assert.AreEqual("30", result.Text[result.SelectionStart..result.SelectionEnd]);
    }

    [TestMethod]
    public void TableNextCellShouldWrapOntoTheNextRow()
    {
        var two = "| a | b |\n| - | - |\n| c | d |\n| e | f |\n";
        var caret = two.IndexOf("d");

        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableNextCell, two, caret, caret);

        Assert.AreEqual("e", result.Text[result.SelectionStart..result.SelectionEnd]);
    }

    [TestMethod]
    public void TableNextCellShouldAddARowPastTheLastCell()
    {
        var caret = Table.IndexOf("30");
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableNextCell, Table, caret, caret);

        Assert.AreEqual(
            "| Name | Age |\n" +
            "| ---- | --- |\n" +
            "| Ann  | 30  |\n" +
            "|      |     |\n", result.Text);
        Assert.AreEqual(result.SelectionStart, result.SelectionEnd);
    }

    [TestMethod]
    public void TableNextCellShouldStepFromTheHeaderIntoTheFirstBodyRow()
    {
        var caret = Table.IndexOf("Age");
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TableNextCell, Table, caret, caret);

        Assert.AreEqual("Ann", result.Text[result.SelectionStart..result.SelectionEnd]);
    }

    [TestMethod]
    public void TablePreviousCellShouldWalkBack()
    {
        var caret = Table.IndexOf("30");
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TablePreviousCell, Table, caret, caret);

        Assert.AreEqual("Ann", result.Text[result.SelectionStart..result.SelectionEnd]);
    }

    [TestMethod]
    public void TablePreviousCellShouldDoNothingAtTheVeryFirstCell()
    {
        var caret = Table.IndexOf("Name");

        Assert.IsFalse(BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.TablePreviousCell, Table, caret, caret).Handled);
    }

    [TestMethod]
    public void IndentAndOutdentShouldWalkTheCellsInsideATable()
    {
        var forward = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, Table, TableCaret, TableCaret);
        Assert.AreEqual("30", forward.Text[forward.SelectionStart..forward.SelectionEnd]);

        var caret = Table.IndexOf("30");
        var back = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Outdent, Table, caret, caret);
        Assert.AreEqual("Ann", back.Text[back.SelectionStart..back.SelectionEnd]);
    }

    [TestMethod]
    public void IndentShouldStillIndentOutsideATable()
    {
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, "- item", 3, 3);

        Assert.AreEqual("  - item", result.Text);
    }

    [TestMethod]
    public void IndentShouldStillIndentASelectionSpanningATable()
    {
        // A real selection is a block operation, whatever it happens to cover.
        var result = BitMarkdownEditorCommands.Apply(BitMarkdownEditorCommand.Indent, Table, 0, Table.Length);

        Assert.Contains("  | Name | Age |", result.Text);
    }
}
