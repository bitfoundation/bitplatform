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
}
