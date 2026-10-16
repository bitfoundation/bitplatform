using System.Numerics;

namespace Bit.Bink;

/// <summary>Raised when the text tool wants the host to show a text editor.</summary>
/// <param name="Element">The text element being edited (already in the document).</param>
/// <param name="ViewRect">Where to place the editor, in view (CSS pixel) coordinates.</param>
public readonly record struct BinkTextEditRequest(BinkTextElement Element, BinkRect ViewRect);

/// <summary>
/// Places and edits text annotations. The tool creates the element and asks the host to show a real HTML text
/// input over it, because an accessible, IME-capable text editor cannot be built on a canvas.
/// </summary>
public sealed class BinkTextTool : BinkToolBase
{
    /// <inheritdoc />
    public override string Id => "text";

    /// <inheritdoc />
    public override string DisplayName => "Text";

    /// <inheritdoc />
    public override string Cursor => "text";

    /// <summary>Text style for new text elements.</summary>
    public BinkTextStyle Style { get; set; } = BinkTextStyle.Default;

    /// <summary>Default width of a new text box in document units, used to size the editor.</summary>
    public float DefaultWidth { get; set; } = 200f;

    /// <summary>The element currently being edited, if any.</summary>
    public BinkTextElement? Editing { get; private set; }

    private IDisposable? _creationGroup;
    private Guid? _createdElementId;

    /// <summary>Raised when the host should show or move a text editor.</summary>
    public event Action<BinkTextEditRequest>? EditRequested;

    /// <summary>Raised when editing finished and the editor should be hidden.</summary>
    public event Action? EditFinished;

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        Vector2 doc = context.Viewport.ToDocument(sample.Position);

        // Editing an existing text element takes priority over creating a new one.
        BinkElement? hit = BinkDocumentQuery.HitTest(context.Document, doc, 4f / context.Viewport.Zoom);
        if (hit is BinkTextElement existing)
        {
            BeginEdit(context, existing);
            return true;
        }
        return CreateAt(context, doc);
    }

    /// <summary>
    /// Creates an empty text element and opens the editor over it. Exposed so a host can start text entry from
    /// something other than a pointer, which is what makes text reachable from the keyboard.
    /// </summary>
    /// <param name="context">The tool context.</param>
    /// <param name="documentPoint">Where the text box starts, in document coordinates.</param>
    /// <returns>True when an element was created and the editor was requested.</returns>
    public bool CreateAt(IBinkToolContext context, Vector2 documentPoint)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Editing is not null) return false;
        if (context.Document.TryGetLayer(context.ActiveLayerId, out BinkLayer? layer) && (layer.IsLocked || !layer.IsVisible)) return false;

        var element = new BinkTextElement(documentPoint, string.Empty)
        {
            Style = Style with { Color = context.ShapeStyle.StrokeColor },
            Size = new Vector2(DefaultWidth, Style.FontSize * Style.LineHeight),
        };
        // Placing the box and typing into it is one action to the user, so it is one undo step. The group opens
        // before the element is added and stays open until the edit is committed or cancelled.
        _creationGroup = context.History.BeginGroup("Add text");
        try
        {
            context.Document.AddElement(element, context.ActiveLayerId);
        }
        catch (InvalidOperationException ex)
        {
            context.Announce("Text could not be added: " + ex.Message);
            EndCreationGroup();
            return false;
        }

        _createdElementId = element.Id;
        BeginEdit(context, element);
        return true;
    }

    /// <summary>Starts editing an element and asks the host for an editor.</summary>
    public void BeginEdit(IBinkToolContext context, BinkTextElement element)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(element);
        Editing = element;
        EditRequested?.Invoke(new BinkTextEditRequest(element, context.Viewport.ToView(element.Bounds)));
        context.InvalidateOverlay();
    }

    /// <summary>
    /// Applies edited text. An empty result removes the element, so an abandoned text box does not leave an
    /// invisible object behind.
    /// </summary>
    /// <param name="context">Tool context.</param>
    /// <param name="text">The new text.</param>
    /// <param name="measuredSize">Size measured by the host in document units, or null to estimate.</param>
    public void CommitEdit(IBinkToolContext context, string text, Vector2? measuredSize = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        BinkTextElement? element = Editing;
        Editing = null;
        EditFinished?.Invoke();
        if (element is null)
        {
            EndCreationGroup();
            return;
        }
        if (!context.Document.ContainsElement(element.Id))
        {
            EndCreationGroup();
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                context.Document.RemoveElement(element.Id);
                // A box placed and then left blank never happened as far as undo cares, exactly as when the edit
                // is cancelled; recording it would leave an undo step that does nothing.
                if (_createdElementId == element.Id) context.History.CancelGroup();
                context.InvalidateDocument(element.Bounds);
                return;
            }
            var updated = element with { Text = text, Size = measuredSize };
            context.Document.ReplaceElement(updated);
            context.InvalidateDocument(updated.Bounds.Union(element.Bounds));
            context.Announce("Text updated.");
        }
        catch (InvalidOperationException ex)
        {
            context.Announce("The text could not be saved: " + ex.Message);
        }
        finally
        {
            EndCreationGroup();
        }
    }

    /// <summary>Cancels editing, removing the element when it was newly created and is still empty.</summary>
    public void CancelEdit(IBinkToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        BinkTextElement? element = Editing;
        Editing = null;
        EditFinished?.Invoke();
        if (element is not null && element.Text.Length == 0 && context.Document.ContainsElement(element.Id))
        {
            context.Document.RemoveElement(element.Id);
            // The box was created and removed within this gesture, so it never happened as far as undo cares.
            if (_createdElementId == element.Id) context.History.CancelGroup();
        }
        EndCreationGroup();
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        CancelEdit(context);
        EndCreationGroup();
    }

    /// <summary>Closes the undo group that spans placing a text box and typing into it.</summary>
    private void EndCreationGroup()
    {
        _creationGroup?.Dispose();
        _creationGroup = null;
        _createdElementId = null;
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(render);
        if (Editing is null) return;
        // The HTML editor covers the element; draw only a focus outline around it.
        float zoom = MathF.Max(context.Viewport.Zoom, 0.001f);
        render.StrokePath(new BinkPathBuilder(6).AddRect(Editing.Bounds).ToGeometry(),
            new BinkStrokePaint(new BinkColor(37, 99, 235), 1f / zoom, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter, [3f / zoom, 3f / zoom]));
    }

    /// <inheritdoc />
    public override bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (key.Key == "Escape" && Editing is not null)
        {
            CancelEdit(context);
            return true;
        }
        // Without this the one interaction that is properly accessible, a real text field, could only be reached
        // with a pointer. Enter opens it in the middle of the view.
        if (key.Key == "Enter" && Editing is null)
        {
            Vector2 centre = context.Viewport.ToDocument(context.ViewSize / 2f);
            if (!CreateAt(context, centre)) return false;
            context.Announce("Text box added. Type, then press Enter to keep it or Escape to discard it.");
            return true;
        }
        return false;
    }
}
