using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Editing;

namespace Bit.Bmage.Editor.Tools;

/// <summary>Which tool is currently active in the editor.</summary>
public enum EditorToolKind
{
    /// <summary>Select, move and resize existing annotations.</summary>
    Select = 0,
    /// <summary>Pan the view.</summary>
    Pan,
    /// <summary>Define a crop rectangle.</summary>
    Crop,
    /// <summary>Freehand pen.</summary>
    Draw,
    /// <summary>Straight line.</summary>
    Line,
    /// <summary>Arrow with a head at the end.</summary>
    Arrow,
    /// <summary>Rectangle outline or fill.</summary>
    Rectangle,
    /// <summary>Ellipse outline or fill.</summary>
    Ellipse,
    /// <summary>Translucent highlight block.</summary>
    Highlight,
    /// <summary>Hide content with a solid block, pixelation or blur.</summary>
    Redact,
    /// <summary>Text label.</summary>
    Text,
    /// <summary>Numbered step marker.</summary>
    Step,
}

/// <summary>The current drawing style shared by the annotation tools.</summary>
public sealed record EditorToolSettings
{
    public Rgba32 StrokeColor { get; init; } = new(230, 40, 40);
    public Rgba32 FillColor { get; init; } = new(230, 40, 40, 60);
    public bool Filled { get; init; }
    public float StrokeWidth { get; init; } = 4f;
    public bool Dashed { get; init; }
    public float Opacity { get; init; } = 1f;
    public float FontSize { get; init; } = 24f;
    public string FontFamily { get; init; } = "system-ui, sans-serif";
    public bool Bold { get; init; }
    public RedactionMode RedactionMode { get; init; } = RedactionMode.Solid;
    public float RedactionStrength { get; init; } = 14f;
    public Rgba32 HighlightColor { get; init; } = new(255, 235, 59, 110);

    /// <summary>Builds the shape style used by the outline tools.</summary>
    public ShapeStyle ToShapeStyle() => new()
    {
        StrokeColor = StrokeColor,
        FillColor = Filled ? FillColor : null,
        Stroke = new StrokeStyle
        {
            Width = StrokeWidth,
            Cap = LineCap.Round,
            Join = LineJoin.Round,
            DashPattern = Dashed ? [StrokeWidth * 3, StrokeWidth * 2] : null,
        },
    };

    /// <summary>Builds the text style used by the text tools.</summary>
    public TextStyle ToTextStyle() => new()
    {
        FontFamily = FontFamily,
        FontSize = FontSize,
        Bold = Bold,
        FillColor = StrokeColor,
    };
}

/// <summary>The state of an in-progress drag on the canvas, in image coordinates.</summary>
public sealed class ToolDrag
{
    /// <summary>Where the drag began. Pan gestures rebase this on every move so the delta stays incremental.</summary>
    public required Vector2 Start { get; set; }

    /// <summary>The latest pointer position.</summary>
    public Vector2 Current { get; set; }
    /// <summary>The sampled path, for freehand strokes.</summary>
    public List<Vector2> Points { get; } = [];

    /// <summary>True when shift is held, which constrains shapes to squares and angles to 15 degree steps.</summary>
    public bool ShiftKey { get; set; }

    /// <summary>True when alt is held.</summary>
    public bool AltKey { get; set; }

    /// <summary>The rectangle between the start and current points, normalised so width and height are positive.</summary>
    public RectangleF Rectangle
    {
        get
        {
            var x = MathF.Min(Start.X, Current.X);
            var y = MathF.Min(Start.Y, Current.Y);
            var w = MathF.Abs(Current.X - Start.X);
            var h = MathF.Abs(Current.Y - Start.Y);
            if (ShiftKey)
            {
                // Constrain to a square, keeping the corner under the pointer.
                var size = MathF.Max(w, h);
                if (Current.X < Start.X) x = Start.X - size;
                if (Current.Y < Start.Y) y = Start.Y - size;
                w = h = size;
            }
            return new RectangleF(x, y, w, h);
        }
    }

    /// <summary>The end point, snapped to 15° increments when shift is held.</summary>
    public Vector2 ConstrainedEnd
    {
        get
        {
            if (!ShiftKey) return Current;
            var delta = Current - Start;
            if (delta.LengthSquared() < 1e-6f) return Current;
            var angle = MathF.Atan2(delta.Y, delta.X);
            var step = MathF.PI / 12;
            var snapped = MathF.Round(angle / step) * step;
            var length = delta.Length();
            return Start + new Vector2(MathF.Cos(snapped), MathF.Sin(snapped)) * length;
        }
    }
}

/// <summary>
/// Turns pointer drags into annotations. Implementing this interface and registering it with the editor is how a
/// developer adds a custom tool without modifying the library.
/// </summary>
public interface IEditorTool
{
    /// <summary>The tool this handler implements.</summary>
    EditorToolKind Kind { get; }

    /// <summary>
    /// Builds a preview annotation for the drag in progress, or null when there is nothing to show yet. Called on every
    /// pointer move, so it must be cheap.
    /// </summary>
    Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings);

    /// <summary>
    /// Builds the annotation to commit when the drag ends, or null to commit nothing (for example a zero-size shape).
    /// </summary>
    Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings);
}

/// <summary>The built-in annotation tools.</summary>
public static class EditorTools
{
    /// <summary>All built-in tool handlers, keyed by kind.</summary>
    public static IReadOnlyDictionary<EditorToolKind, IEditorTool> BuiltIn { get; } = new Dictionary<EditorToolKind, IEditorTool>
    {
        [EditorToolKind.Draw] = new FreehandTool(),
        [EditorToolKind.Line] = new LineTool(),
        [EditorToolKind.Arrow] = new ArrowTool(),
        [EditorToolKind.Rectangle] = new RectangleTool(),
        [EditorToolKind.Ellipse] = new EllipseTool(),
        [EditorToolKind.Highlight] = new HighlightTool(),
        [EditorToolKind.Redact] = new RedactTool(),
    };

    /// <summary>Minimum drag distance, in image pixels, before a shape is considered intentional.</summary>
    public const float MinimumDragDistance = 3f;

    private sealed class FreehandTool : IEditorTool
    {
        public EditorToolKind Kind => EditorToolKind.Draw;

        public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings)
            => drag.Points.Count < 1 ? null : new FreehandAnnotation
            {
                Points = drag.Points,
                Style = settings.ToShapeStyle() with { FillColor = null },
                Opacity = settings.Opacity,
            };

        public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings) => BuildPreview(drag, settings);
    }

    private sealed class LineTool : IEditorTool
    {
        public EditorToolKind Kind => EditorToolKind.Line;

        public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings) => new LineAnnotation
        {
            Start = drag.Start,
            End = drag.ConstrainedEnd,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings)
            => Vector2.Distance(drag.Start, drag.Current) < MinimumDragDistance ? null : BuildPreview(drag, settings);
    }

    private sealed class ArrowTool : IEditorTool
    {
        public EditorToolKind Kind => EditorToolKind.Arrow;

        public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings) => new ArrowAnnotation
        {
            Start = drag.Start,
            End = drag.ConstrainedEnd,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings)
            => Vector2.Distance(drag.Start, drag.Current) < MinimumDragDistance ? null : BuildPreview(drag, settings);
    }

    private sealed class RectangleTool : IEditorTool
    {
        public EditorToolKind Kind => EditorToolKind.Rectangle;

        public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings) => new RectangleAnnotation
        {
            Rectangle = drag.Rectangle,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }

    private sealed class EllipseTool : IEditorTool
    {
        public EditorToolKind Kind => EditorToolKind.Ellipse;

        public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings) => new EllipseAnnotation
        {
            Rectangle = drag.Rectangle,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }

    private sealed class HighlightTool : IEditorTool
    {
        public EditorToolKind Kind => EditorToolKind.Highlight;

        public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings) => new HighlightAnnotation
        {
            Rectangle = drag.Rectangle,
            Color = settings.HighlightColor,
        };

        public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }

    private sealed class RedactTool : IEditorTool
    {
        public EditorToolKind Kind => EditorToolKind.Redact;

        public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings) => new RedactionAnnotation
        {
            Rectangle = drag.Rectangle,
            Mode = settings.RedactionMode,
            Strength = settings.RedactionStrength,
            Color = Rgba32.Black,
        };

        public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }
}
