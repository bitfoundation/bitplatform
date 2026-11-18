using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>The built-in annotation tools.</summary>
public static class BmageEditorTools
{
    /// <summary>All built-in tool handlers, keyed by kind.</summary>
    public static IReadOnlyDictionary<BmageEditorToolKind, IBmageEditorTool> BuiltIn { get; } = new Dictionary<BmageEditorToolKind, IBmageEditorTool>
    {
        [BmageEditorToolKind.Draw] = new FreehandTool(),
        [BmageEditorToolKind.Line] = new LineTool(),
        [BmageEditorToolKind.Arrow] = new ArrowTool(),
        [BmageEditorToolKind.Rectangle] = new RectangleTool(),
        [BmageEditorToolKind.Ellipse] = new EllipseTool(),
        [BmageEditorToolKind.Highlight] = new HighlightTool(),
        [BmageEditorToolKind.Redact] = new RedactTool(),
    };

    /// <summary>Minimum drag distance, in image pixels, before a shape is considered intentional.</summary>
    public const float MinimumDragDistance = 3f;

    private sealed class FreehandTool : IBmageEditorTool
    {
        public BmageEditorToolKind Kind => BmageEditorToolKind.Draw;

        public BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings)
            => drag.Points.Count < 1 ? null : new BmageFreehandAnnotation
            {
                Points = drag.Points,
                Style = settings.ToShapeStyle() with { FillColor = null },
                Opacity = settings.Opacity,
            };

        public BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings) => BuildPreview(drag, settings);
    }

    private sealed class LineTool : IBmageEditorTool
    {
        public BmageEditorToolKind Kind => BmageEditorToolKind.Line;

        public BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings) => new BmageLineAnnotation
        {
            Start = drag.Start,
            End = drag.ConstrainedEnd,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings)
            => Vector2.Distance(drag.Start, drag.Current) < MinimumDragDistance ? null : BuildPreview(drag, settings);
    }

    private sealed class ArrowTool : IBmageEditorTool
    {
        public BmageEditorToolKind Kind => BmageEditorToolKind.Arrow;

        public BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings) => new BmageArrowAnnotation
        {
            Start = drag.Start,
            End = drag.ConstrainedEnd,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings)
            => Vector2.Distance(drag.Start, drag.Current) < MinimumDragDistance ? null : BuildPreview(drag, settings);
    }

    private sealed class RectangleTool : IBmageEditorTool
    {
        public BmageEditorToolKind Kind => BmageEditorToolKind.Rectangle;

        public BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings) => new BmageRectangleAnnotation
        {
            Rectangle = drag.Rectangle,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }

    private sealed class EllipseTool : IBmageEditorTool
    {
        public BmageEditorToolKind Kind => BmageEditorToolKind.Ellipse;

        public BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings) => new BmageEllipseAnnotation
        {
            Rectangle = drag.Rectangle,
            Style = settings.ToShapeStyle(),
            Opacity = settings.Opacity,
        };

        public BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }

    private sealed class HighlightTool : IBmageEditorTool
    {
        public BmageEditorToolKind Kind => BmageEditorToolKind.Highlight;

        public BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings) => new BmageHighlightAnnotation
        {
            Rectangle = drag.Rectangle,
            Color = settings.HighlightColor,
        };

        public BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }

    private sealed class RedactTool : IBmageEditorTool
    {
        public BmageEditorToolKind Kind => BmageEditorToolKind.Redact;

        public BmageAnnotation? BuildPreview(BmageToolDrag drag, BmageEditorToolSettings settings) => new BmageRedactionAnnotation
        {
            Rectangle = drag.Rectangle,
            Mode = settings.RedactionMode,
            Strength = settings.RedactionStrength,
            Color = BmageRgba32.Black,
        };

        public BmageAnnotation? BuildFinal(BmageToolDrag drag, BmageEditorToolSettings settings)
        {
            var r = drag.Rectangle;
            return r.Width < MinimumDragDistance || r.Height < MinimumDragDistance ? null : BuildPreview(drag, settings);
        }
    }
}
