using System.Numerics;

namespace Bit.Bink;

/// <summary>Which edge or centre <see cref="BinkDocument.AlignElements"/> lines elements up on.</summary>
public enum BinkAlignment : byte
{
    /// <summary>Left edges.</summary>
    Left = 0,
    /// <summary>Horizontal centres, on one vertical line.</summary>
    HorizontalCenter = 1,
    /// <summary>Right edges.</summary>
    Right = 2,
    /// <summary>Top edges.</summary>
    Top = 3,
    /// <summary>Vertical centres, on one horizontal line.</summary>
    VerticalCenter = 4,
    /// <summary>Bottom edges.</summary>
    Bottom = 5,
}

/// <summary>The axis along which <see cref="BinkDocument.DistributeElements"/> spaces elements out.</summary>
public enum BinkDistribution : byte
{
    /// <summary>Equal gaps from left to right.</summary>
    Horizontal = 0,
    /// <summary>Equal gaps from top to bottom.</summary>
    Vertical = 1,
}

/// <summary>Which way <see cref="BinkDocument.FlipElements"/> mirrors elements.</summary>
public enum BinkFlipDirection : byte
{
    /// <summary>Left becomes right, about a vertical line.</summary>
    Horizontal = 0,
    /// <summary>Top becomes bottom, about a horizontal line.</summary>
    Vertical = 1,
}

public sealed partial class BinkDocument
{
    /// <summary>
    /// Lines elements up on an edge or a centre, as one undoable step. A group moves as a whole, so its members keep
    /// their places relative to each other.
    /// </summary>
    /// <param name="elementIds">The elements, typically the selection; ids not in the document are ignored.</param>
    /// <param name="alignment">The edge or centre to line up.</param>
    /// <param name="reference">
    /// What to align to, in document units: the page, or one element's bounds, for example. Null aligns to the bounds
    /// of the elements themselves, which needs at least two separate elements or groups to do anything.
    /// </param>
    /// <returns>True when anything moved.</returns>
    public bool AlignElements(IEnumerable<Guid> elementIds, BinkAlignment alignment, BinkRect? reference = null)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        List<LayoutUnit> units = LayoutUnits(elementIds);
        if (units.Count == 0 || (reference is null && units.Count < 2)) return false;
        BinkRect target = reference ?? Union(units);

        var ops = new List<BinkDrawingOperation>();
        foreach (LayoutUnit unit in units)
        {
            BinkRect b = unit.Bounds;
            Vector2 delta = alignment switch
            {
                BinkAlignment.Left => new Vector2(target.Left - b.Left, 0),
                BinkAlignment.HorizontalCenter => new Vector2(target.Center.X - b.Center.X, 0),
                BinkAlignment.Right => new Vector2(target.Right - b.Right, 0),
                BinkAlignment.Top => new Vector2(0, target.Top - b.Top),
                BinkAlignment.VerticalCenter => new Vector2(0, target.Center.Y - b.Center.Y),
                _ => new Vector2(0, target.Bottom - b.Bottom),
            };
            Move(unit, delta, ops);
        }
        if (ops.Count == 0) return false;
        Apply(new BinkBatchOperation(ops, "Align"));
        return true;
    }

    /// <summary>
    /// Spaces elements out with equal gaps between them, as one undoable step. The two outermost elements (or
    /// groups) stay where they are and the others move between them, in the order of their centres. When they are
    /// wider than the space between the outermost two, the gaps come out negative and the elements overlap evenly.
    /// </summary>
    /// <param name="elementIds">The elements, typically the selection; ids not in the document are ignored.</param>
    /// <param name="axis">The direction to space them along.</param>
    /// <returns>True when anything moved; false for fewer than three separate elements or groups.</returns>
    public bool DistributeElements(IEnumerable<Guid> elementIds, BinkDistribution axis)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        List<LayoutUnit> units = LayoutUnits(elementIds);
        if (units.Count < 3) return false;
        bool horizontal = axis == BinkDistribution.Horizontal;
        units.Sort((a, b) => horizontal ? a.Bounds.Center.X.CompareTo(b.Bounds.Center.X) : a.Bounds.Center.Y.CompareTo(b.Bounds.Center.Y));

        float start = horizontal ? units[0].Bounds.Left : units[0].Bounds.Top;
        float end = horizontal ? units[^1].Bounds.Right : units[^1].Bounds.Bottom;
        float sizes = 0f;
        foreach (LayoutUnit unit in units) sizes += horizontal ? unit.Bounds.Width : unit.Bounds.Height;
        float gap = (end - start - sizes) / (units.Count - 1);

        var ops = new List<BinkDrawingOperation>();
        float cursor = start;
        foreach (LayoutUnit unit in units)
        {
            BinkRect b = unit.Bounds;
            Move(unit, horizontal ? new Vector2(cursor - b.Left, 0) : new Vector2(0, cursor - b.Top), ops);
            cursor += (horizontal ? b.Width : b.Height) + gap;
        }
        if (ops.Count == 0) return false;
        Apply(new BinkBatchOperation(ops, "Distribute"));
        return true;
    }

    /// <summary>
    /// Mirrors elements about the centre of their combined bounds, as one undoable step, so they swap sides as a
    /// whole and each is mirrored too.
    /// </summary>
    /// <remarks>
    /// Text stays readable: a text element moves to its mirrored place but its letters are not reversed. A callout
    /// moves its box and anchor to their mirrored places the same way. Everything else, ink included, is mirrored
    /// through its transform, so its points are never rewritten and flipping twice restores it exactly.
    /// </remarks>
    /// <param name="elementIds">The elements, typically the selection; ids not in the document are ignored.</param>
    /// <param name="direction">Which way to mirror.</param>
    /// <returns>True when anything changed.</returns>
    public bool FlipElements(IEnumerable<Guid> elementIds, BinkFlipDirection direction)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        List<LayoutUnit> units = LayoutUnits(elementIds);
        if (units.Count == 0) return false;
        Vector2 pivot = Union(units).Center;
        bool horizontal = direction == BinkFlipDirection.Horizontal;
        Matrix3x2 mirror = Matrix3x2.CreateScale(horizontal ? -1f : 1f, horizontal ? 1f : -1f, pivot);

        var ops = new List<BinkDrawingOperation>();
        foreach (LayoutUnit unit in units)
        {
            foreach (BinkElement element in unit.Members)
            {
                BinkElement flipped = Mirror(element, mirror);
                if (!ReferenceEquals(flipped, element)) ops.Add(new BinkReplaceElementOperation(flipped));
            }
        }
        // A lone text, which only ever moves, does not change at all, and that is not an undo step.
        if (ops.Count == 0) return false;
        Apply(new BinkBatchOperation(ops, horizontal ? "Flip horizontally" : "Flip vertically"));
        return true;
    }

    private static BinkElement Mirror(BinkElement element, Matrix3x2 mirror)
    {
        switch (element)
        {
            case BinkTextElement:
            {
                // Moved, not mirrored: reversed letters are not what anyone flipping a diagram wants. Flipped on its
                // own it stays where it is, and is returned unchanged.
                Vector2 center = element.Bounds.Center;
                return Shift(element, Vector2.Transform(center, mirror) - center);
            }
            case BinkCalloutElement callout when IsTranslation(callout.Transform):
            {
                // The box and the anchor swap sides in local units; the text inside stays readable.
                var offset = new Vector2(callout.Transform.M31, callout.Transform.M32);
                Matrix3x2 local = Matrix3x2.CreateTranslation(offset) * mirror * Matrix3x2.CreateTranslation(-offset);
                BinkRect box = callout.Box.Transform(local);
                return callout with { Box = box, Anchor = Vector2.Transform(callout.Anchor, local) };
            }
            case BinkCalloutElement:
            {
                Vector2 center = element.Bounds.Center;
                return Shift(element, Vector2.Transform(center, mirror) - center);
            }
            default:
            {
                // The second flip's pivot is measured from mirrored bounds, which rounding moves by a hair, so two
                // flips leave a residue of about 1e-5 instead of the identity they are. Snapped back, the element
                // regains the identity's fast paths and saves without a transform, exactly as before the flips.
                Matrix3x2 transform = element.Transform * mirror;
                return element with { Transform = IsNearlyIdentity(transform) ? Matrix3x2.Identity : transform };
            }
        }
    }

    private static bool IsNearlyIdentity(Matrix3x2 m)
        => MathF.Abs(m.M11 - 1f) < 1e-5f && MathF.Abs(m.M22 - 1f) < 1e-5f && MathF.Abs(m.M12) < 1e-5f && MathF.Abs(m.M21) < 1e-5f
           && MathF.Abs(m.M31) < 1e-3f && MathF.Abs(m.M32) < 1e-3f;

    private static bool IsTranslation(Matrix3x2 m) => m.M11 == 1f && m.M12 == 0f && m.M21 == 0f && m.M22 == 1f;

    private static bool IsNegligible(Vector2 delta) => MathF.Abs(delta.X) < 1e-4f && MathF.Abs(delta.Y) < 1e-4f;

    private static BinkElement Shift(BinkElement element, Vector2 delta) => IsNegligible(delta) ? element : element.Translate(delta);

    private static void Move(LayoutUnit unit, Vector2 delta, List<BinkDrawingOperation> ops)
    {
        if (IsNegligible(delta)) return;
        foreach (BinkElement element in unit.Members) ops.Add(new BinkReplaceElementOperation(element.Translate(delta)));
    }

    private static BinkRect Union(List<LayoutUnit> units)
    {
        BinkRect bounds = BinkRect.Empty;
        foreach (LayoutUnit unit in units) bounds = bounds.Union(unit.Bounds);
        return bounds;
    }

    /// <summary>One thing that moves as a whole when laying out: an element, or a group given in full.</summary>
    private sealed class LayoutUnit
    {
        public List<BinkElement> Members { get; } = [];

        public BinkRect Bounds { get; set; } = BinkRect.Empty;
    }

    /// <summary>
    /// Splits elements into what moves as a whole: each group whose members are all given is one unit, and every
    /// other element is a unit of its own. Eraser strokes are left out, as everywhere they cannot be selected.
    /// </summary>
    private List<LayoutUnit> LayoutUnits(IEnumerable<Guid> elementIds)
    {
        var ids = new HashSet<Guid>(elementIds);
        HashSet<Guid> whole = ContainedGroups(ids, selectableOnly: true);
        var units = new List<LayoutUnit>();
        var byKey = new Dictionary<Guid, LayoutUnit>();
        foreach (BinkLayer layer in _layers)
        {
            foreach (BinkElement element in layer.Elements)
            {
                if (!ids.Contains(element.Id) || element is BinkStroke { IsEraser: true } or BinkUnknownElement) continue;
                Guid key = element.Id;
                IReadOnlyList<Guid> chain = element.GroupIds;
                for (int i = chain.Count - 1; i >= 0; i--)
                {
                    if (!whole.Contains(chain[i])) continue;
                    key = chain[i];
                    break;
                }
                if (!byKey.TryGetValue(key, out LayoutUnit? unit))
                {
                    byKey[key] = unit = new LayoutUnit();
                    units.Add(unit);
                }
                unit.Members.Add(element);
                unit.Bounds = unit.Bounds.Union(element.Bounds);
            }
        }
        return units;
    }
}
