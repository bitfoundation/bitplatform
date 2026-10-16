using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// The mapping between document coordinates and view coordinates (CSS pixels of the canvas element).
/// Immutable; zooming and panning produce new viewports so the document's vector data is never touched.
/// </summary>
/// <remarks>
/// Coordinate spaces:
/// <list type="bullet">
/// <item><b>Document</b>: the units elements are stored in.</item>
/// <item><b>View</b>: CSS pixels relative to the canvas element's top-left. <c>view = document * Zoom + Offset</c>.</item>
/// <item><b>Device</b>: physical canvas pixels, <c>device = view * DevicePixelRatio</c>; handled by the renderer.</item>
/// </list>
/// </remarks>
public readonly record struct BinkViewport
{
    /// <summary>Identity viewport (zoom 1, no offset).</summary>
    public static readonly BinkViewport Identity = new(1f, Vector2.Zero);

    private readonly float _zoom;
    private readonly Vector2 _offset;

    /// <summary>Creates a viewport.</summary>
    public BinkViewport(float zoom, Vector2 offset)
    {
        Zoom = zoom;
        Offset = offset;
    }

    /// <summary>
    /// Scale factor (1 = one document unit per CSS pixel). Always positive and finite: a zero, negative or
    /// non-finite value reads back as 1. The guard lives on the property rather than only in the constructor
    /// because <c>default(Viewport)</c> and <c>viewport with { Zoom = ... }</c> both skip constructors, and a
    /// zoom of zero would turn every document coordinate into infinity.
    /// </summary>
    public float Zoom
    {
        get => _zoom > 0f && float.IsFinite(_zoom) ? _zoom : 1f;
        init => _zoom = value > 0f && float.IsFinite(value) ? value : 1f;
    }

    /// <summary>View position of the document origin, in CSS pixels. Non-finite components read back as zero.</summary>
    public Vector2 Offset
    {
        get => _offset;
        init => _offset = new Vector2(float.IsFinite(value.X) ? value.X : 0f, float.IsFinite(value.Y) ? value.Y : 0f);
    }

    /// <summary>Document to view transform.</summary>
    public Matrix3x2 DocumentToView => new(Zoom, 0, 0, Zoom, Offset.X, Offset.Y);

    /// <summary>View to document transform.</summary>
    public Matrix3x2 ViewToDocument => new(1f / Zoom, 0, 0, 1f / Zoom, -Offset.X / Zoom, -Offset.Y / Zoom);

    /// <summary>Converts a view point to document coordinates.</summary>
    public Vector2 ToDocument(Vector2 view) => (view - Offset) / Zoom;

    /// <summary>Converts a document point to view coordinates.</summary>
    public Vector2 ToView(Vector2 document) => document * Zoom + Offset;

    /// <summary>Converts a view rectangle to document coordinates.</summary>
    public BinkRect ToDocument(BinkRect view) => new((view.X - Offset.X) / Zoom, (view.Y - Offset.Y) / Zoom, view.Width / Zoom, view.Height / Zoom);

    /// <summary>Converts a document rectangle to view coordinates.</summary>
    public BinkRect ToView(BinkRect document) => new(document.X * Zoom + Offset.X, document.Y * Zoom + Offset.Y, document.Width * Zoom, document.Height * Zoom);

    /// <summary>The document rectangle visible in a view of the given size.</summary>
    public BinkRect VisibleRect(Vector2 viewSize) => ToDocument(new BinkRect(0, 0, viewSize.X, viewSize.Y));

    /// <summary>Returns a viewport translated by a view-space delta.</summary>
    public BinkViewport Pan(Vector2 viewDelta) => this with { Offset = Offset + viewDelta };

    /// <summary>Returns a viewport zoomed by a factor around a view-space anchor (the point under the cursor stays put).</summary>
    public BinkViewport ZoomBy(float factor, Vector2 viewAnchor, float minZoom = 0.05f, float maxZoom = 64f)
        => ZoomTo(Math.Clamp(Zoom * factor, minZoom, maxZoom), viewAnchor);

    /// <summary>Returns a viewport with an absolute zoom around a view-space anchor.</summary>
    public BinkViewport ZoomTo(float zoom, Vector2 viewAnchor)
    {
        if (!(zoom > 0) || !float.IsFinite(zoom)) return this;
        Vector2 docAnchor = ToDocument(viewAnchor);
        return new BinkViewport(zoom, viewAnchor - docAnchor * zoom);
    }

    /// <summary>Creates a viewport that fits a document rectangle inside a view size with padding.</summary>
    public static BinkViewport Fit(BinkRect documentRect, Vector2 viewSize, float padding = 16f, float maxZoom = 64f)
    {
        if (documentRect.IsEmpty || viewSize.X <= 0 || viewSize.Y <= 0) return Identity;
        float availW = MathF.Max(viewSize.X - 2 * padding, 1f);
        float availH = MathF.Max(viewSize.Y - 2 * padding, 1f);
        float zoom = MathF.Min(availW / documentRect.Width, availH / documentRect.Height);
        zoom = Math.Clamp(zoom, 0.01f, maxZoom);
        Vector2 offset = (viewSize - documentRect.Size * zoom) / 2f - documentRect.TopLeft * zoom;
        return new BinkViewport(zoom, offset);
    }
}
