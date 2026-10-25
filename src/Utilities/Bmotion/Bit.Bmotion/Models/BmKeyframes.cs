using System.Collections;
using System.Runtime.CompilerServices;

namespace Bit.Bmotion;

/// <summary>
/// A numeric animation value: a single target (<c>Opacity = 1</c>) or a keyframe
/// sequence (<c>Scale = [1, 1.4, 0.8, 1]</c>). Implicitly converts from <c>double</c>
/// and <c>double[]</c>, and supports C# collection expressions.
/// <para>
/// Use <see cref="Bm.Current"/> inside a keyframe sequence as a wildcard meaning
/// "the element's current value", e.g. <c>x: [Bm.Current, 100]</c>.
/// </para>
/// </summary>
[CollectionBuilder(typeof(BmKeyframes), nameof(Create))]
public sealed class BmKeyframes : IEnumerable<double>, IEquatable<BmKeyframes>
{
    private readonly double[] _frames;

    public BmKeyframes(params double[] frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Length == 0)
            throw new ArgumentException("At least one keyframe value is required.", nameof(frames));
        // Clone so later caller mutation of the source array can't change this instance.
        _frames = (double[])frames.Clone();
    }

    /// <summary>Collection-expression builder: <c>BmKeyframes k = [0, 100, 0];</c></summary>
    public static BmKeyframes Create(ReadOnlySpan<double> frames) => new(frames.ToArray());

    /// <summary>The keyframe values (at least one).</summary>
    public IReadOnlyList<double> Frames => _frames;

    /// <summary>Number of keyframes.</summary>
    public int Count => _frames.Length;

    /// <summary>True when this holds a single target value rather than a sequence.</summary>
    public bool IsSingle => _frames.Length == 1;

    /// <summary>The first keyframe (the value the animation starts from / initial CSS).</summary>
    public double First => _frames[0];

    /// <summary>The last keyframe (the value the animation settles on).</summary>
    public double Last => _frames[^1];

    public static implicit operator BmKeyframes(double value) => new(value);
    public static implicit operator BmKeyframes(double[] frames) => new(frames);

    /// <summary>
    /// The loosely-typed value the animation engine consumes: a boxed <c>double</c> for a
    /// single target, or a <c>double[]</c> for a keyframe sequence.
    /// </summary>
    internal object ToEngineValue() => _frames.Length == 1 ? _frames[0] : (double[])_frames.Clone();

    /// <summary>
    /// First-frame value usable in a CSS declaration, or <c>false</c> when it is the
    /// <see cref="Bm.Current"/> wildcard (whose value is only known at animation time).
    /// </summary>
    internal bool TryGetCssNumber(out double value)
    {
        value = _frames[0];
        return double.IsFinite(value);
    }

    public bool Equals(BmKeyframes? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        // EqualityComparer<double> treats NaN == NaN, so wildcard frames compare equal.
        return _frames.AsSpan().SequenceEqual(other._frames);
    }

    public override bool Equals(object? obj) => Equals(obj as BmKeyframes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var f in _frames) hash.Add(f);
        return hash.ToHashCode();
    }

    public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)_frames).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
