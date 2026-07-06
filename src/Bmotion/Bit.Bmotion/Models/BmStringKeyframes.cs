using System.Collections;
using System.Runtime.CompilerServices;

namespace Bit.Bmotion;

/// <summary>
/// A string-valued animation value (colors, dimensions, shadows): a single target
/// (<c>BackgroundColor = "#f00"</c>) or a keyframe sequence
/// (<c>BackgroundColor = ["#f00", "#0f0", "#f00"]</c>). Implicitly converts from
/// <c>string</c>, <c>string[]</c> and <c>double</c> (bare numbers get a <c>px</c> unit,
/// so <c>width: 200</c> means <c>200px</c>), and supports C# collection expressions.
/// </summary>
/// <remarks>
/// <b>Security:</b> values are written verbatim into the element's inline style. They are
/// intended for developer-authored values; binding untrusted end-user input risks CSS injection.
/// </remarks>
[CollectionBuilder(typeof(BmStringKeyframes), nameof(Create))]
public sealed class BmStringKeyframes : IEnumerable<string>, IEquatable<BmStringKeyframes>
{
    private readonly string[] _frames;

    public BmStringKeyframes(params string[] frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Length == 0)
            throw new ArgumentException("At least one keyframe value is required.", nameof(frames));
        foreach (var f in frames)
            if (f is null)
                throw new ArgumentException("Keyframe values must not be null.", nameof(frames));
        _frames = (string[])frames.Clone();
    }

    /// <summary>Collection-expression builder: <c>BmStringKeyframes k = ["#f00", "#0f0"];</c></summary>
    public static BmStringKeyframes Create(ReadOnlySpan<string> frames) => new(frames.ToArray());

    /// <summary>The keyframe values (at least one).</summary>
    public IReadOnlyList<string> Frames => _frames;

    /// <summary>Number of keyframes.</summary>
    public int Count => _frames.Length;

    /// <summary>True when this holds a single target value rather than a sequence.</summary>
    public bool IsSingle => _frames.Length == 1;

    /// <summary>The first keyframe (the value the animation starts from / initial CSS).</summary>
    public string First => _frames[0];

    /// <summary>The last keyframe (the value the animation settles on).</summary>
    public string Last => _frames[^1];

    public static implicit operator BmStringKeyframes(string value) => new(value);
    public static implicit operator BmStringKeyframes(string[] frames) => new(frames);
    /// <summary>Bare numbers default to a <c>px</c> unit, matching motion.dev.</summary>
    public static implicit operator BmStringKeyframes(double value) => new(BmotionCssFormat.Num(value) + "px");

    /// <summary>
    /// The loosely-typed value the animation engine consumes: a <c>string</c> for a single
    /// target, or a <c>string[]</c> for a keyframe sequence.
    /// </summary>
    internal object ToEngineValue() => _frames.Length == 1 ? _frames[0] : (string[])_frames.Clone();

    public bool Equals(BmStringKeyframes? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _frames.AsSpan().SequenceEqual(other._frames);
    }

    public override bool Equals(object? obj) => Equals(obj as BmStringKeyframes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var f in _frames) hash.Add(f, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_frames).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
