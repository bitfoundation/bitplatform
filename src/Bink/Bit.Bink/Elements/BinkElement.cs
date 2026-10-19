using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bit.Bink;

/// <summary>
/// Base type of everything that lives in a layer: strokes, shapes, text, images and annotations.
/// </summary>
/// <remarks>
/// <para>
/// Elements are immutable records identified by <see cref="Id"/>. Editing an element means creating a modified copy
/// with a <c>with</c> expression and replacing it in the document (<see cref="BinkDocument.ReplaceElement"/>). This makes
/// undo/redo, diffing and collaboration simple: an edit is "element X went from version 3 to version 4".
/// Point data and cached geometry are shared between copies, so a <c>with</c> is cheap even for large strokes.
/// </para>
/// <para>
/// Equality is reference equality: two element instances are different versions even when their content matches.
/// </para>
/// <para>
/// Geometry is expressed in local coordinates; <see cref="Transform"/> maps local to document coordinates. Moving,
/// rotating or scaling an element only changes the transform, so the geometry does not have to be rebuilt.
/// </para>
/// </remarks>
public abstract record BinkElement
{
    /// <summary>Unique identifier, stable across versions and serialization.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Monotonically increasing version, bumped by the document on every replace. Used for last-writer-wins conflict handling.</summary>
    public int Version { get; init; }

    /// <summary>Maps local coordinates to document coordinates. Identity for freshly drawn elements.</summary>
    public Matrix3x2 Transform { get; init; } = Matrix3x2.Identity;

    /// <summary>
    /// Optional human readable description exposed to assistive technology and included in exports
    /// (for example "Signature of Jane Doe" or "Arrow pointing at the total").
    /// </summary>
    public string? Description { get; init; }

    /// <summary>Optional application-defined key/value metadata, preserved by serialization.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>Bounds in local coordinates, including stroke widths.</summary>
    public abstract BinkRect LocalBounds { get; }

    /// <summary>Axis-aligned bounds in document coordinates.</summary>
    public BinkRect Bounds => LocalBounds.Transform(Transform);

    /// <summary>A short type name for UI and accessibility ("Stroke", "Rectangle", ...).</summary>
    public abstract string TypeName { get; }

    /// <summary>Whether the element can be selected and manipulated by users. Eraser strokes, for example, are not.</summary>
    public virtual bool IsSelectable => true;

    /// <summary>Draws the element in local coordinates.</summary>
    protected abstract void DrawCore(IBinkRenderContext context);

    /// <summary>Draws the element into the context, applying <see cref="Transform"/>.</summary>
    public void Draw(IBinkRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Transform.IsIdentity)
        {
            DrawCore(context);
            return;
        }
        context.Save();
        context.Transform(Transform);
        DrawCore(context);
        context.Restore();
    }

    /// <summary>Hit test in local coordinates.</summary>
    /// <param name="localPoint">Point in local coordinates.</param>
    /// <param name="tolerance">Extra distance in local units that still counts as a hit.</param>
    protected abstract bool HitTestCore(Vector2 localPoint, float tolerance);

    /// <summary>True when the document-space point hits the element.</summary>
    /// <param name="point">Point in document coordinates.</param>
    /// <param name="tolerance">Extra distance in document units that still counts as a hit.</param>
    public bool HitTest(Vector2 point, float tolerance = 0f)
    {
        if (!Bounds.Inflate(tolerance).Contains(point)) return false;
        if (!TryToLocal(point, out Vector2 local, out float scale)) return false;
        return HitTestCore(local, tolerance / scale);
    }

    /// <summary>
    /// True when the document-space rectangle intersects the element's geometry (not just its bounds).
    /// The default implementation tests the transformed bounds; derived types refine it.
    /// </summary>
    public virtual bool IntersectsRect(BinkRect rect) => Bounds.Intersects(rect);

    /// <summary>Converts a document point to local coordinates. Returns false when the transform is not invertible.</summary>
    /// <param name="point">Document point.</param>
    /// <param name="local">Local point.</param>
    /// <param name="scale">Approximate scale factor of the transform (local units per document unit is 1/scale).</param>
    public bool TryToLocal(Vector2 point, out Vector2 local, out float scale)
    {
        if (Transform.IsIdentity)
        {
            local = point;
            scale = 1f;
            return true;
        }
        if (!Matrix3x2.Invert(Transform, out Matrix3x2 inverse))
        {
            local = default;
            scale = 1f;
            return false;
        }
        local = Vector2.Transform(point, inverse);
        scale = MathF.Sqrt(MathF.Abs(Transform.GetDeterminant()));
        if (scale < BinkGeometryMath.Epsilon) scale = 1f;
        return true;
    }

    /// <summary>Returns a copy with an additional document-space transform applied after the current one.</summary>
    public BinkElement TransformBy(Matrix3x2 matrix) => this with { Transform = Transform * matrix };

    /// <summary>Returns a copy translated by <paramref name="delta"/> in document units.</summary>
    public BinkElement Translate(Vector2 delta) => TransformBy(Matrix3x2.CreateTranslation(delta));

    /// <summary>Reference equality: each instance is a distinct version.</summary>
    public virtual bool Equals(BinkElement? other) => ReferenceEquals(this, other);

    /// <inheritdoc />
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    /// <summary>Human readable description used when <see cref="Description"/> is not set.</summary>
    public virtual string GetAccessibleDescription() => Description ?? TypeName;

    /// <inheritdoc />
    public override string ToString() => $"{TypeName} {Id:N} v{Version}";
}
