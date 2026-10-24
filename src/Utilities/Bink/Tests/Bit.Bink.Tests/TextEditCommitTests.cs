using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Committing a text edit changes the text and its size, and nothing else: the editor stays open for as long as
/// the user types, and the element may change underneath it meanwhile.
/// </summary>
[TestClass]
public class TextEditCommitTests
{
    /// <summary>
    /// The commit rebuilt the element from the snapshot taken when editing began, so a move made while the editor
    /// was open (by a collaborator, or by the application) was silently undone.
    /// </summary>
    [TestMethod]
    public void A_change_made_while_editing_survives_the_commit()
    {
        using var context = new TestToolContext();
        var original = new BinkTextElement(new Vector2(40, 60), "Draft") { Size = new Vector2(80, 20) };
        context.Document.AddElement(original);
        var tool = new BinkTextTool();
        tool.BeginEdit(context, original);

        // Someone else moves and recolours the element while the editor is open.
        var moved = original with { Position = new Vector2(300, 200), Style = original.Style with { Color = new BinkColor(200, 0, 0) } };
        context.Document.ReplaceElement(moved);

        tool.CommitEdit(context, "Final", new Vector2(90, 20));

        var committed = (BinkTextElement)context.Document.GetElement(original.Id);
        Assert.AreEqual("Final", committed.Text);
        Assert.AreEqual(new Vector2(90, 20), committed.Size);
        Assert.AreEqual(new Vector2(300, 200), committed.Position);
        Assert.AreEqual(new BinkColor(200, 0, 0), committed.Style.Color);
    }

    /// <summary>Without a measurement the commit threw away the size the host had measured and fell back to the estimate.</summary>
    [TestMethod]
    public void Committing_without_a_measurement_keeps_the_measured_size()
    {
        using var context = new TestToolContext();
        var original = new BinkTextElement(new Vector2(40, 60), "Draft") { Size = new Vector2(120, 24) };
        context.Document.AddElement(original);
        var tool = new BinkTextTool();
        tool.BeginEdit(context, original);

        tool.CommitEdit(context, "Final");

        var committed = (BinkTextElement)context.Document.GetElement(original.Id);
        Assert.AreEqual("Final", committed.Text);
        Assert.AreEqual(new Vector2(120, 24), committed.Size);
    }

    [TestMethod]
    public void A_new_box_committed_without_a_measurement_is_estimated()
    {
        // The size a new box is placed with only sizes the editor; it was never a measurement of any text.
        using var context = new TestToolContext();
        var tool = new BinkTextTool();
        Assert.IsTrue(tool.CreateAt(context, new Vector2(40, 60)));

        tool.CommitEdit(context, "First line\nSecond line\nThird line");

        var committed = (BinkTextElement)Assert.ContainsSingle(context.Document.AllElements);
        Assert.IsNull(committed.Size);
        Assert.AreEqual(committed.EstimateSize(), committed.EffectiveSize);
    }

    [TestMethod]
    public void Blank_text_removes_the_element_where_it_now_is()
    {
        using var context = new TestToolContext();
        var original = new BinkTextElement(new Vector2(40, 60), "Draft");
        context.Document.AddElement(original);
        var tool = new BinkTextTool();
        tool.BeginEdit(context, original);
        context.Document.ReplaceElement(original with { Position = new Vector2(300, 200) });

        tool.CommitEdit(context, "  ");

        Assert.AreEqual(0, context.Document.ElementCount);
    }
}
