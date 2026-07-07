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
}
