using System.Globalization;
using System.Numerics;

namespace Bit.Bmage.Operations.Adjustments;

/// <summary>
/// A three-dimensional colour lookup table: a cube of output colours sampled over every input colour, the form film
/// emulations, colour grades and camera looks are exchanged in. Read one from an Adobe/Resolve <c>.cube</c> file
/// with <see cref="ParseCube"/>, or build one from a function with <see cref="Create"/>, and apply it with
/// <see cref="ColorLutAdjustment"/>.
/// </summary>
/// <remarks>
/// The table is immutable. Inputs are the encoded sRGB values in [0,1], as every adjustment sees them; a table made
/// for log or linear footage needs that transfer applied first, which a <c>.cube</c> file does not record.
/// </remarks>
public sealed class ColorLookupTable
{
    /// <summary>The fewest samples along each axis.</summary>
    public const int MinSize = 2;

    /// <summary>The most samples along each axis: 65 is the largest size in common use.</summary>
    public const int MaxSize = 65;

    private readonly float[] _values;

    private ColorLookupTable(int size, float[] values, string? title)
    {
        Size = size;
        _values = values;
        Title = title;
    }

    /// <summary>Samples along each axis.</summary>
    public int Size { get; }

    /// <summary>The table's title, from a <c>.cube</c> file's <c>TITLE</c> line, if it had one.</summary>
    public string? Title { get; }

    /// <summary>
    /// The output colours, three values (red, green, blue) per entry, red changing fastest, then green, then blue: the
    /// order of a <c>.cube</c> file.
    /// </summary>
    public ReadOnlySpan<float> Values => _values;

    /// <summary>Builds a table of <paramref name="size"/> samples along each axis by evaluating <paramref name="map"/> at each.</summary>
    /// <param name="size">Samples along each axis, between 2 and 65; 33 is plenty for smooth grades.</param>
    /// <param name="map">The colour for an input colour, both with channels in [0,1].</param>
    /// <param name="title">An optional title.</param>
    public static ColorLookupTable Create(int size, Func<Vector3, Vector3> map, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        CheckSize(size);
        var values = new float[size * size * size * 3];
        var step = 1f / (size - 1);
        var i = 0;
        for (var b = 0; b < size; b++)
            for (var g = 0; g < size; g++)
                for (var r = 0; r < size; r++)
                {
                    var c = map(new Vector3(r * step, g * step, b * step));
                    values[i++] = Finite(c.X);
                    values[i++] = Finite(c.Y);
                    values[i++] = Finite(c.Z);
                }
        return new ColorLookupTable(size, values, title);
    }

    /// <summary>Wraps output values laid out as <see cref="Values"/> describes; the array is copied.</summary>
    public static ColorLookupTable FromValues(int size, ReadOnlySpan<float> values, string? title = null)
    {
        CheckSize(size);
        if (values.Length != size * size * size * 3)
            throw new ArgumentException($"A table of size {size} needs {size * size * size * 3} values, not {values.Length}.", nameof(values));
        var copy = values.ToArray();
        foreach (var v in copy)
            if (!float.IsFinite(v)) throw new ArgumentException("Every value must be a finite number.", nameof(values));
        return new ColorLookupTable(size, copy, title);
    }

    /// <summary>
    /// Reads a table in the <c>.cube</c> format (Adobe, DaVinci Resolve, most grading tools): <c>LUT_3D_SIZE</c>,
    /// optional <c>TITLE</c>, <c>DOMAIN_MIN</c> and <c>DOMAIN_MAX</c> (or Resolve's <c>LUT_3D_INPUT_RANGE min max</c>,
    /// one domain for all three channels), then one <c>r g b</c> line per entry.
    /// </summary>
    /// <remarks>
    /// A domain other than [0,1] is folded into the values, so the table always takes inputs in [0,1]. One-dimensional
    /// tables (<c>LUT_1D_SIZE</c>) are refused: express them as a <see cref="CurveOperation"/> instead.
    /// </remarks>
    /// <exception cref="FormatException">The text is not a valid three-dimensional <c>.cube</c> table.</exception>
    public static ColorLookupTable ParseCube(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var size = 0;
        string? title = null;
        Vector3 domainMin = Vector3.Zero, domainMax = Vector3.One;
        float[]? values = null;
        var count = 0;
        var lineNumber = 0;
        Span<float> inputRange = stackalloc float[2];
        foreach (var raw in text.AsSpan().EnumerateLines())
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.IsEmpty || line[0] == '#') continue;
            if (char.IsAsciiLetter(line[0]))
            {
                var space = line.IndexOfAny(' ', '\t');
                var keyword = space < 0 ? line : line[..space];
                var rest = space < 0 ? [] : line[(space + 1)..].Trim();
                if (keyword.SequenceEqual("TITLE"))
                {
                    title = rest.Trim('"').ToString();
                }
                else if (keyword.SequenceEqual("LUT_3D_SIZE"))
                {
                    if (values is not null) throw Error(lineNumber, "LUT_3D_SIZE appears twice.");
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out size) || size < MinSize || size > MaxSize)
                        throw Error(lineNumber, $"LUT_3D_SIZE must be between {MinSize} and {MaxSize}.");
                    values = new float[size * size * size * 3];
                }
                else if (keyword.SequenceEqual("LUT_1D_SIZE"))
                {
                    throw Error(lineNumber, "One-dimensional tables are not supported; use a curve for them.");
                }
                else if (keyword.SequenceEqual("DOMAIN_MIN"))
                {
                    domainMin = ReadTriple(rest, lineNumber);
                }
                else if (keyword.SequenceEqual("DOMAIN_MAX"))
                {
                    domainMax = ReadTriple(rest, lineNumber);
                }
                else if (keyword.SequenceEqual("LUT_3D_INPUT_RANGE"))
                {
                    // Resolve's spelling of the domain: one range for every channel.
                    ReadNumbers(rest, lineNumber, inputRange);
                    domainMin = new Vector3(inputRange[0]);
                    domainMax = new Vector3(inputRange[1]);
                }
                // Other keywords (vendor extensions) do not change the table's values.
                continue;
            }
            if (values is null) throw Error(lineNumber, "Values come before LUT_3D_SIZE.");
            if (count == values.Length) throw Error(lineNumber, $"More than the {size * size * size} entries LUT_3D_SIZE {size} declares.");
            var entry = ReadTriple(line, lineNumber);
            values[count++] = entry.X;
            values[count++] = entry.Y;
            values[count++] = entry.Z;
        }
        if (values is null) throw new FormatException("The text has no LUT_3D_SIZE line, so it is not a three-dimensional .cube table.");
        if (count != values.Length) throw new FormatException($"LUT_3D_SIZE {size} declares {size * size * size} entries but the text has {count / 3}.");
        if (domainMin.X >= domainMax.X || domainMin.Y >= domainMax.Y || domainMin.Z >= domainMax.Z)
            throw new FormatException("DOMAIN_MIN must be below DOMAIN_MAX for every channel.");

        var table = new ColorLookupTable(size, values, title);
        if (domainMin == Vector3.Zero && domainMax == Vector3.One) return table;
        // Inputs in [0,1] stand for the domain's range: sample the declared table where each lands in it.
        return Create(size, c => table.Lookup(Vector3.Clamp((c - domainMin) / (domainMax - domainMin), Vector3.Zero, Vector3.One)), title);
    }

    /// <summary>Writes the table in the <c>.cube</c> format.</summary>
    public string ToCube()
    {
        var sb = new System.Text.StringBuilder();
        if (Title is { Length: > 0 }) sb.Append("TITLE \"").Append(Title.Replace('"', '\'')).Append("\"\n");
        sb.Append("LUT_3D_SIZE ").Append(Size.ToString(CultureInfo.InvariantCulture)).Append('\n');
        for (var i = 0; i < _values.Length; i += 3)
            sb.Append(_values[i].ToString("R", CultureInfo.InvariantCulture)).Append(' ')
              .Append(_values[i + 1].ToString("R", CultureInfo.InvariantCulture)).Append(' ')
              .Append(_values[i + 2].ToString("R", CultureInfo.InvariantCulture)).Append('\n');
        return sb.ToString();
    }

    /// <summary>
    /// The table's colour for <paramref name="color"/> (channels clamped to [0,1]) by tetrahedral interpolation between
    /// the four entries around it: smoother along the grey axis than trilinear interpolation, and exact at every entry.
    /// </summary>
    public Vector3 Lookup(Vector3 color)
    {
        var n = Size - 1;
        var c = Vector3.Clamp(color, Vector3.Zero, Vector3.One) * n;
        int r0 = Math.Min((int)c.X, n - 1), g0 = Math.Min((int)c.Y, n - 1), b0 = Math.Min((int)c.Z, n - 1);
        float fr = c.X - r0, fg = c.Y - g0, fb = c.Z - b0;
        int sr = 3, sg = 3 * Size, sb = 3 * Size * Size;
        var origin = r0 * sr + g0 * sg + b0 * sb;
        var v = _values;
        var c000 = At(v, origin);
        var c111 = At(v, origin + sr + sg + sb);
        // The tetrahedron holding the point is picked by the order of its fractions.
        if (fr >= fg)
        {
            if (fg >= fb)
                return c000 + fr * (At(v, origin + sr) - c000) + fg * (At(v, origin + sr + sg) - At(v, origin + sr)) + fb * (c111 - At(v, origin + sr + sg));
            if (fr >= fb)
                return c000 + fr * (At(v, origin + sr) - c000) + fb * (At(v, origin + sr + sb) - At(v, origin + sr)) + fg * (c111 - At(v, origin + sr + sb));
            return c000 + fb * (At(v, origin + sb) - c000) + fr * (At(v, origin + sr + sb) - At(v, origin + sb)) + fg * (c111 - At(v, origin + sr + sb));
        }
        if (fb >= fg)
            return c000 + fb * (At(v, origin + sb) - c000) + fg * (At(v, origin + sg + sb) - At(v, origin + sb)) + fr * (c111 - At(v, origin + sg + sb));
        if (fb >= fr)
            return c000 + fg * (At(v, origin + sg) - c000) + fb * (At(v, origin + sg + sb) - At(v, origin + sg)) + fr * (c111 - At(v, origin + sg + sb));
        return c000 + fg * (At(v, origin + sg) - c000) + fr * (At(v, origin + sr + sg) - At(v, origin + sg)) + fb * (c111 - At(v, origin + sr + sg));
    }

    /// <summary>True when the two tables have the same size and values; the titles are not compared.</summary>
    public bool HasSameValues(ColorLookupTable? other) => other is not null && other.Size == Size && other._values.AsSpan().SequenceEqual(_values);

    private static Vector3 At(float[] values, int index) => new(values[index], values[index + 1], values[index + 2]);

    private static float Finite(float value) => float.IsFinite(value) ? value : throw new ArgumentException("The map returned a value that is not a finite number.");

    private static void CheckSize(int size)
    {
        if (size is < MinSize or > MaxSize) throw new ArgumentOutOfRangeException(nameof(size), size, $"The size must be between {MinSize} and {MaxSize}.");
    }

    private static Vector3 ReadTriple(ReadOnlySpan<char> text, int lineNumber)
    {
        Span<float> parts = stackalloc float[3];
        ReadNumbers(text, lineNumber, parts);
        return new Vector3(parts[0], parts[1], parts[2]);
    }

    /// <summary>Reads exactly <c>parts.Length</c> finite numbers from <paramref name="text"/> into <paramref name="parts"/>.</summary>
    private static void ReadNumbers(ReadOnlySpan<char> text, int lineNumber, Span<float> parts)
    {
        var found = 0;
        foreach (var range in text.SplitAny(' ', '\t'))
        {
            var part = text[range];
            if (part.IsEmpty) continue;
            if (found == parts.Length || !float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out parts[found]) || !float.IsFinite(parts[found]))
                throw Error(lineNumber, ExpectedNumbers(parts.Length));
            found++;
        }
        if (found != parts.Length) throw Error(lineNumber, ExpectedNumbers(parts.Length));
    }

    private static string ExpectedNumbers(int count) => count == 3 ? "Expected three numbers." : "Expected two numbers.";

    private static FormatException Error(int line, string message) => new($"Line {line} of the .cube text: {message}");
}

/// <summary>
/// Maps every colour through a <see cref="ColorLookupTable"/>: a film look, a grade exported from a colour tool, a
/// brand's house style. <see cref="Strength"/> blends the result with the original.
/// </summary>
public sealed class ColorLutAdjustment : PointOperation
{
    /// <param name="table">The table to apply.</param>
    /// <param name="strength">How much of the table's result to use, in [0,1]: 1 is the table as made, 0 the original.</param>
    public ColorLutAdjustment(ColorLookupTable table, float strength = 1f)
    {
        Table = table ?? throw new ArgumentNullException(nameof(table));
        if (!float.IsFinite(strength)) throw new ArgumentOutOfRangeException(nameof(strength), "The strength must be a finite number.");
        Strength = Math.Clamp(strength, 0f, 1f);
    }

    /// <summary>The table applied.</summary>
    public ColorLookupTable Table { get; }

    /// <summary>How much of the table's result is used, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => Table.Title is { Length: > 0 } title ? $"LUT: {title}" : "LUT";

    public override bool IsIdentity => Strength == 0f;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var table = Table;
        var strength = Strength;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var original = new Vector3(p.X, p.Y, p.Z);
            var mapped = table.Lookup(original);
            var c = strength >= 1f ? mapped : Vector3.Lerp(original, mapped, strength);
            pixels[i] = new Vector4(c, p.W);
        }
    }
}
