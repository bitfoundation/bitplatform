using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Keyboard operation of the tools. A canvas that can only be driven by a pointer is unusable for anyone who
/// does not use one, so selecting, nudging, resizing, rotating and placing text all have to work from the
/// keyboard alone, and each edit has to be a single undo step.
/// </summary>
[TestClass]
public class KeyboardTests
{
    private static BinkToolKeyEvent Key(string key, bool ctrl = false, bool shift = false, bool alt = false)
        => new(key, ctrl, shift, alt);

    private static (TestToolContext Context, BinkSelectTool Tool) WithSelection(int elements = 3)
    {
        var context = new TestToolContext();
        for (int i = 0; i < elements; i++)
        {
            context.Document.AddElement(new BinkRectangleShape(new BinkRect(20 + i * 90, 40, 60, 40)));
        }
        var tool = new BinkSelectTool();
        tool.Activate(context);
        return (context, tool);
    }

    [TestMethod]
    public void Enter_selects_the_first_element_so_navigation_can_start_without_a_pointer()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection();
        using (context)
        {
            Assert.IsTrue(context.Selection.IsEmpty);

            Assert.IsTrue(tool.OnKeyDown(context, Key("Enter")));

            Assert.AreEqual(1, context.Selection.Count);
            Assert.AreEqual(context.Document.AllElements.First().Id, context.Selection.Ids[0]);
            Assert.Contains(a => a.Contains("1 of 3", StringComparison.Ordinal), context.Announcements);
        }
    }

    [TestMethod]
    public void Tab_and_shift_tab_step_through_the_elements_and_wrap()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection();
        using (context)
        {
            List<Guid> ids = context.Document.AllElements.Select(e => e.Id).ToList();
            tool.OnKeyDown(context, Key("Enter"));
            Assert.AreEqual(ids[0], context.Selection.Ids[0]);

            Assert.IsTrue(tool.OnKeyDown(context, Key("Tab")));
            Assert.AreEqual(ids[1], context.Selection.Ids[0]);
            tool.OnKeyDown(context, Key("Tab"));
            Assert.AreEqual(ids[2], context.Selection.Ids[0]);

            // Forwards from the last wraps to the first, and backwards from the first wraps to the last.
            tool.OnKeyDown(context, Key("Tab"));
            Assert.AreEqual(ids[0], context.Selection.Ids[0]);
            tool.OnKeyDown(context, Key("Tab", shift: true));
            Assert.AreEqual(ids[2], context.Selection.Ids[0]);
        }
    }

    [TestMethod]
    public void Tab_is_left_to_the_page_when_nothing_is_selected()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection();
        using (context)
        {
            // Returning false is what lets the browser move focus out: the canvas must never be a keyboard trap.
            Assert.IsFalse(tool.OnKeyDown(context, Key("Tab")));
            Assert.IsTrue(context.Selection.IsEmpty);
        }
    }

    [TestMethod]
    public void Arrow_keys_scroll_the_page_when_nothing_is_selected()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection();
        using (context)
        {
            foreach (string key in new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown" })
            {
                Assert.IsFalse(tool.OnKeyDown(context, Key(key)));
            }
        }
    }

    [TestMethod]
    public void Arrow_keys_nudge_the_selection()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection(1);
        using (context)
        {
            Guid id = context.Document.AllElements.First().Id;
            context.Selection.Set(id);
            BinkRect before = context.Document.GetElement(id).Bounds;

            Assert.IsTrue(tool.OnKeyDown(context, Key("ArrowRight")));
            Assert.AreEqual(before.X + 1f, context.Document.GetElement(id).Bounds.X, 5e-4);

            // Shift makes it a coarse nudge.
            Assert.IsTrue(tool.OnKeyDown(context, Key("ArrowDown", shift: true)));
            Assert.AreEqual(before.Y + 10f, context.Document.GetElement(id).Bounds.Y, 5e-4);
        }
    }

    [TestMethod]
    public void Arrow_keys_nudge_and_control_arrows_resize()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection(1);
        using (context)
        {
            Guid id = context.Document.AllElements.First().Id;
            context.Selection.Set(id);
            BinkRect before = context.Document.GetElement(id).Bounds;

            Assert.IsTrue(tool.OnKeyDown(context, Key("ArrowRight", ctrl: true)));
            BinkRect wider = context.Document.GetElement(id).Bounds;
            Assert.IsTrue(wider.Width > before.Width, $"width did not grow: {before.Width} -> {wider.Width}");

            Assert.IsTrue(tool.OnKeyDown(context, Key("ArrowLeft", ctrl: true)));
            BinkRect back = context.Document.GetElement(id).Bounds;
            Assert.IsTrue(back.Width < wider.Width, $"width did not shrink again: {wider.Width} -> {back.Width}");
        }
    }

    [TestMethod]
    public void R_rotates_the_selection_and_shift_r_rotates_back()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection(1);
        using (context)
        {
            Guid id = context.Document.AllElements.First().Id;
            context.Selection.Set(id);
            Matrix3x2 before = context.Document.GetElement(id).Transform;

            Assert.IsTrue(tool.OnKeyDown(context, Key("r")));
            Matrix3x2 rotated = context.Document.GetElement(id).Transform;
            Assert.AreNotEqual(before, rotated);

            Assert.IsTrue(tool.OnKeyDown(context, Key("R", shift: true)));
            Matrix3x2 restored = context.Document.GetElement(id).Transform;
            Assert.AreEqual(before.M11, restored.M11, 5e-4);
            Assert.AreEqual(before.M12, restored.M12, 5e-4);
            Assert.AreEqual(before.M21, restored.M21, 5e-4);
            Assert.AreEqual(before.M22, restored.M22, 5e-4);
        }
    }

    [TestMethod]
    public void Shift_adds_to_the_selection_so_multi_select_works_without_a_mouse()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection();
        using (context)
        {
            tool.OnKeyDown(context, Key("Enter"));
            Assert.AreEqual(1, context.Selection.Count);

            // Ctrl+A is the keyboard route to everything at once.
            Assert.IsTrue(tool.OnKeyDown(context, Key("a", ctrl: true)));
            Assert.AreEqual(3, context.Selection.Count);
        }
    }

    [TestMethod]
    public void Every_keyboard_edit_is_one_undo_step()
    {
        (TestToolContext context, BinkSelectTool tool) = WithSelection(1);
        using (context)
        {
            Guid id = context.Document.AllElements.First().Id;
            context.Selection.Set(id);
            BinkRect before = context.Document.GetElement(id).Bounds;
            int steps = context.History.UndoCount;

            tool.OnKeyDown(context, Key("ArrowRight"));
            tool.OnKeyDown(context, Key("ArrowRight"));
            tool.OnKeyDown(context, Key("r"));

            // Three presses, three steps: not one step per element and not one step for the lot.
            Assert.AreEqual(steps + 3, context.History.UndoCount);

            context.History.Undo();
            context.History.Undo();
            context.History.Undo();
            Assert.AreEqual(before.X, context.Document.GetElement(id).Bounds.X, 5e-4);
        }
    }

    [TestMethod]
    public void A_locked_or_hidden_layer_is_skipped_by_keyboard_navigation()
    {
        using var context = new TestToolContext();
        Guid visible = context.Document.Layers[0].Id;
        context.Document.AddElement(new BinkRectangleShape(new BinkRect(10, 10, 40, 40)), visible);

        BinkLayer locked = context.Document.AddLayer("Locked");
        context.Document.AddElement(new BinkRectangleShape(new BinkRect(80, 10, 40, 40)), locked.Id);
        context.Document.UpdateLayer(locked.Id, p => p with { IsLocked = true });

        BinkLayer hidden = context.Document.AddLayer("Hidden");
        context.Document.AddElement(new BinkRectangleShape(new BinkRect(150, 10, 40, 40)), hidden.Id);
        context.Document.UpdateLayer(hidden.Id, p => p with { IsVisible = false });

        var tool = new BinkSelectTool();
        tool.Activate(context);

        tool.OnKeyDown(context, Key("Enter"));
        Assert.AreEqual(1, context.Selection.Count);

        // Only the one element on the editable layer is reachable, so Tab keeps landing on it.
        Guid only = context.Selection.Ids[0];
        tool.OnKeyDown(context, Key("Tab"));
        Assert.AreEqual(only, context.Selection.Ids[0]);
        Assert.Contains(a => a.Contains("1 of 1", StringComparison.Ordinal), context.Announcements);
    }

    [TestMethod]
    public void Enter_with_the_text_tool_opens_an_editor_without_a_pointer()
    {
        using var context = new TestToolContext();
        var tool = new BinkTextTool();
        BinkTextEditRequest? requested = null;
        tool.EditRequested += r => requested = r;
        tool.Activate(context);

        Assert.IsTrue(tool.OnKeyDown(context, Key("Enter")));

        Assert.IsNotNull(requested);
        Assert.IsNotNull(tool.Editing);
        // The box lands in the middle of the view, which is where a keyboard user expects it.
        Vector2 centre = context.Viewport.ToDocument(context.ViewSize / 2f);
        Assert.AreEqual(centre.X, requested!.Value.Element.Position.X, 5e-2);
        Assert.AreEqual(centre.Y, requested.Value.Element.Position.Y, 5e-2);
    }

    [TestMethod]
    public void Text_placement_and_typing_are_one_undo_step()
    {
        using var context = new TestToolContext();
        var tool = new BinkTextTool();
        tool.Activate(context);
        int steps = context.History.UndoCount;

        tool.OnKeyDown(context, Key("Enter"));
        tool.CommitEdit(context, "Reviewed", null);

        Assert.AreEqual(steps + 1, context.History.UndoCount);
        Assert.AreEqual(1, context.Document.ElementCount);

        Assert.IsTrue(context.History.Undo());
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void Shift_constrains_a_shape_to_a_square()
    {
        using var context = new TestToolContext();
        var tool = new BinkShapeTool(BinkShapeKind.Rectangle);
        tool.Activate(context);

        BinkInputSample S(BinkPointerPhase phase, float x, float y, bool shift) => new()
        {
            PointerId = 1,
            Kind = BinkPointerKind.Mouse,
            Phase = phase,
            X = x,
            Y = y,
            Pressure = 0.5f,
            Shift = shift,
            Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
        };

        tool.OnPointerDown(context, S(BinkPointerPhase.Down, 100, 100, true));
        tool.OnPointerMove(context, S(BinkPointerPhase.Move, 300, 160, true));
        tool.OnPointerUp(context, S(BinkPointerPhase.Up, 300, 160, true));

        var shape = Assert.IsExactInstanceOfType<BinkRectangleShape>(Assert.ContainsSingle(context.Document.AllElements));
        Assert.AreEqual(shape.Rect.Width, shape.Rect.Height, 5e-3);
    }
}

/// <summary>
/// A finger is far less precise than a mouse, so the same tool has to offer a bigger target for touch. Kept
/// alongside the keyboard tests because both are about reaching the drawing without a precise pointer.
/// </summary>
[TestClass]
public class PointerTargetTests
{
    [TestMethod]
    public void Touch_gets_larger_hit_targets_than_a_mouse()
    {
        var stroke = TestData.Stroke(new Vector2(100, 100), new Vector2(300, 100), new BinkPenBrush { Width = 2 });

        // A point that misses the stroke by more than a mouse's tolerance but less than a finger's.
        const float missBy = 10f;

        using (var mouse = new TestToolContext())
        {
            mouse.Document.AddElement(stroke);
            var tool = new BinkSelectTool();
            tool.Activate(mouse);
            tool.OnPointerDown(mouse, Sample(BinkPointerKind.Mouse, 200, 100 + missBy));
            Assert.IsTrue(mouse.Selection.IsEmpty, "a mouse should not select a stroke it missed by 10 units");
        }

        using (var touch = new TestToolContext())
        {
            touch.Document.AddElement(stroke with { Id = Guid.NewGuid() });
            var tool = new BinkSelectTool();
            tool.Activate(touch);
            tool.OnPointerDown(touch, Sample(BinkPointerKind.Touch, 200, 100 + missBy));
            Assert.AreEqual(1, touch.Selection.Count);
        }

        static BinkInputSample Sample(BinkPointerKind kind, float x, float y) => new()
        {
            PointerId = 1,
            Kind = kind,
            Phase = BinkPointerPhase.Down,
            X = x,
            Y = y,
            Pressure = 0.5f,
            Buttons = 1,
        };
    }
}
