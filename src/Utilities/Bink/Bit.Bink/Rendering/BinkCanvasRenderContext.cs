using System.Numerics;
using System.Runtime.InteropServices;

namespace Bit.Bink;

/// <summary>Opcodes of the canvas command buffer. Kept in sync with the <c>OP</c> table in <c>bit-bink.js</c>.</summary>
internal enum BinkCanvasCommand : byte
{
    BeginPath = 0,
    MoveTo = 1,
    LineTo = 2,
    QuadTo = 3,
    CubicTo = 4,
    ClosePath = 5,
    Fill = 6,
    Stroke = 7,
    Save = 8,
    Restore = 9,
    Transform = 10,
    ClipRect = 11,
    DrawImage = 12,
    DrawText = 13,
    PushGroup = 14,
    PopGroup = 15,
    Clear = 16,
    SetTransform = 17,
    SetDash = 18,
    ClearRect = 19,
    /// <summary>Slot: the path verbs that follow build a retained path instead of the current one.</summary>
    DefinePath = 20,
    /// <summary>Ends the retained path started by <see cref="DefinePath"/>.</summary>
    EndPath = 21,
    /// <summary>Slot: the next fill or stroke paints a retained path.</summary>
    UsePath = 22,
    /// <summary>Count, then that many path slots the browser can release.</summary>
    FreePaths = 23,
    /// <summary>Slot, string index: keeps a string of this frame's table under a slot.</summary>
    RetainString = 24,
    /// <summary>Count, then that many string slots the browser can release.</summary>
    FreeStrings = 25,
}

/// <summary>
/// An <see cref="IBinkRenderContext"/> that records drawing operations into a flat buffer of doubles, which the
/// browser replays onto a 2D canvas in a single interop call.
/// </summary>
/// <remarks>
/// <para>
/// Recording is allocation-free after warm-up: the buffer is reused between frames and grows only when a frame
/// needs more room than any frame before it. Strings (text and font specifications) go into a side table and are
/// referenced by index, so the numeric buffer stays uniform and can be handed to JavaScript without copying in
/// WebAssembly.
/// </para>
/// <para>
/// Every value is exact in a 32-bit float (colours travel as a 24-bit RGB value and an alpha byte), so the buffer can
/// cross a serialization boundary as packed floats rather than JSON numbers.
/// </para>
/// <para>
/// The canvas component records its document surface with a resource cache: a path is sent to the browser once,
/// kept there as a <c>Path2D</c>, and referred to by slot in every later frame. A context created with the public
/// constructor has no cache and records every path in full, which is what an export or a custom host wants.
/// </para>
/// <para>
/// This is the only rendering path the on-screen canvas uses. The engine never calls canvas APIs directly, which
/// is what allows the same drawing code to produce SVG and PNG output.
/// </para>
/// </remarks>
public sealed class BinkCanvasRenderContext : IBinkRenderContext, IBinkRenderContextCapabilities
{
    private double[] _buffer;
    private int _count;
    private readonly List<string> _strings = new();
    private readonly Dictionary<string, int> _stringIndex = new(StringComparer.Ordinal);
    private readonly BinkCanvasResourceCache? _resources;
    private readonly List<int> _freedPaths = new();
    private readonly List<int> _freedStrings = new();

    /// <summary>Creates a context with an initial capacity.</summary>
    public BinkCanvasRenderContext(int capacity = 4096)
    {
        _buffer = new double[Math.Max(capacity, 64)];
    }

    /// <summary>Creates a context that refers to paths and long strings the browser already holds.</summary>
    internal BinkCanvasRenderContext(int capacity, BinkCanvasResourceCache resources) : this(capacity)
    {
        _resources = resources;
    }

    /// <summary>The recorded commands.</summary>
    public ReadOnlySpan<double> Commands => _buffer.AsSpan(0, _count);

    /// <summary>The recorded commands as an array segment, for interop that needs an array.</summary>
    public ArraySegment<double> CommandSegment => new(_buffer, 0, _count);

    /// <summary>Number of recorded values.</summary>
    public int Count => _count;

    /// <summary>The string table referenced by text and image commands.</summary>
    public IReadOnlyList<string> Strings => _strings;

    /// <summary>True when nothing has been recorded.</summary>
    public bool IsEmpty => _count == 0;

    /// <inheritdoc />
    public bool SupportsText => true;

    /// <inheritdoc />
    public bool SupportsImages => true;

    /// <summary>Clears the buffer for the next frame while keeping its capacity.</summary>
    public void Reset()
    {
        _count = 0;
        _strings.Clear();
        _stringIndex.Clear();
        _resources?.BeginFrame();
    }

    /// <summary>
    /// Ends a frame recorded with a resource cache: tells the browser which retained paths and strings it can
    /// release. Does nothing for a context without one.
    /// </summary>
    internal void EndFrame()
    {
        if (_resources is null) return;
        _freedPaths.Clear();
        _freedStrings.Clear();
        _resources.EndFrame(_freedPaths, _freedStrings);
        WriteSlots(BinkCanvasCommand.FreePaths, _freedPaths);
        WriteSlots(BinkCanvasCommand.FreeStrings, _freedStrings);
    }

    private void WriteSlots(BinkCanvasCommand command, List<int> slots)
    {
        if (slots.Count == 0) return;
        Write(command);
        Write(slots.Count);
        foreach (int slot in slots) Write(slot);
    }

    /// <summary>
    /// The recorded values packed as little-endian 32-bit floats: a quarter of the size of the same buffer as
    /// JSON numbers and nothing to parse, for the interop paths that serialize.
    /// </summary>
    internal byte[] ToSingleBytes()
    {
        var bytes = new byte[_count * sizeof(float)];
        Span<float> values = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
        ReadOnlySpan<double> source = _buffer.AsSpan(0, _count);
        for (int i = 0; i < source.Length; i++) values[i] = (float)source[i];
        if (!BitConverter.IsLittleEndian)
        {
            Span<int> words = MemoryMarshal.Cast<byte, int>(bytes.AsSpan());
            for (int i = 0; i < words.Length; i++) words[i] = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(words[i]);
        }
        return bytes;
    }

    /// <summary>Clears the whole surface to a color (transparent erases it).</summary>
    public void Clear(BinkColor color)
    {
        Write(BinkCanvasCommand.Clear);
        Write(color);
    }

    /// <summary>Clears a rectangle in the current coordinate space.</summary>
    public void ClearRect(BinkRect rect)
    {
        Write(BinkCanvasCommand.ClearRect);
        Write(rect.X);
        Write(rect.Y);
        Write(rect.Width);
        Write(rect.Height);
    }

    /// <summary>Replaces the current transform (used to apply the device pixel ratio and viewport).</summary>
    public void SetTransform(Matrix3x2 matrix)
    {
        Write(BinkCanvasCommand.SetTransform);
        Write(matrix.M11);
        Write(matrix.M12);
        Write(matrix.M21);
        Write(matrix.M22);
        Write(matrix.M31);
        Write(matrix.M32);
    }

    /// <inheritdoc />
    public void Save() => Write(BinkCanvasCommand.Save);

    /// <inheritdoc />
    public void Restore() => Write(BinkCanvasCommand.Restore);

    /// <inheritdoc />
    public void Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity) return;
        Write(BinkCanvasCommand.Transform);
        Write(matrix.M11);
        Write(matrix.M12);
        Write(matrix.M21);
        Write(matrix.M22);
        Write(matrix.M31);
        Write(matrix.M32);
    }

    /// <inheritdoc />
    public void ClipRect(BinkRect rect)
    {
        Write(BinkCanvasCommand.ClipRect);
        Write(rect.X);
        Write(rect.Y);
        Write(rect.Width);
        Write(rect.Height);
    }

    /// <inheritdoc />
    public void FillPath(BinkPathGeometry path, in BinkFillPaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty) return;
        float alpha = Math.Clamp(paint.Opacity, 0f, 1f);
        if (alpha <= 0 && paint.Blend != BinkBlendMode.DestinationOut) return;
        WritePath(path);
        Write(BinkCanvasCommand.Fill);
        Write(paint.Color);
        Write(alpha);
        Write((int)paint.Blend);
        Write(paint.Rule == BinkFillRule.EvenOdd ? 1 : 0);
    }

    /// <inheritdoc />
    public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty || paint.Width <= 0) return;
        float alpha = Math.Clamp(paint.Opacity, 0f, 1f);
        if (alpha <= 0 && paint.Blend != BinkBlendMode.DestinationOut) return;
        if (paint.Dash is { Length: > 0 } dash)
        {
            Write(BinkCanvasCommand.SetDash);
            Write(dash.Length);
            foreach (float d in dash) Write(MathF.Max(d, 0f));
        }
        WritePath(path);
        Write(BinkCanvasCommand.Stroke);
        Write(paint.Color);
        Write(paint.Width);
        Write(alpha);
        Write((int)paint.Blend);
        Write((int)paint.Cap);
        Write((int)paint.Join);
    }

    /// <inheritdoc />
    public void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (destination.IsEmpty || opacity <= 0 || image.IsOmitted) return;
        // Resolved before the opcode: keeping a long URI may emit a command of its own, which has to come first.
        int uri = StringReference(image.Uri);
        Write(BinkCanvasCommand.DrawImage);
        Write(uri);
        Write(destination.X);
        Write(destination.Y);
        Write(destination.Width);
        Write(destination.Height);
        BinkRect crop = sourceRect ?? BinkRect.Empty;
        Write(sourceRect is not null ? 1 : 0);
        Write(crop.X);
        Write(crop.Y);
        Write(crop.Width);
        Write(crop.Height);
        Write(Math.Clamp(opacity, 0f, 1f));
    }

    /// <inheritdoc />
    public void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity)
    {
        if (string.IsNullOrEmpty(text) || opacity <= 0) return;
        int textReference = StringReference(text);
        int fontReference = StringReference(ToCssFont(style));
        Write(BinkCanvasCommand.DrawText);
        Write(textReference);
        Write(fontReference);
        Write(origin.X);
        Write(origin.Y);
        Write(style.FontSize);
        Write(style.FontSize * style.LineHeight);
        Write(style.Color);
        Write(Math.Clamp(opacity, 0f, 1f));
        Write((int)style.Align);
    }

    /// <inheritdoc />
    public void PushGroup(float opacity, BinkBlendMode blend)
    {
        Write(BinkCanvasCommand.PushGroup);
        Write(Math.Clamp(opacity, 0f, 1f));
        Write((int)blend);
    }

    /// <inheritdoc />
    public void PopGroup() => Write(BinkCanvasCommand.PopGroup);

    /// <summary>Builds the CSS <c>font</c> shorthand for a text style.</summary>
    public static string ToCssFont(in BinkTextStyle style)
    {
        string size = style.FontSize.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        string weight = style.Bold ? "bold " : string.Empty;
        string italic = style.Italic ? "italic " : string.Empty;
        return $"{italic}{weight}{size}px {style.FontFamily}";
    }

    private void WritePath(BinkPathGeometry path)
    {
        if (_resources is { } resources)
        {
            if (!resources.TryUsePath(path, out int slot))
            {
                slot = resources.AddPath(path);
                Write(BinkCanvasCommand.DefinePath);
                Write(slot);
                WriteVerbs(path);
                Write(BinkCanvasCommand.EndPath);
            }
            Write(BinkCanvasCommand.UsePath);
            Write(slot);
            return;
        }
        Write(BinkCanvasCommand.BeginPath);
        WriteVerbs(path);
    }

    private void WriteVerbs(BinkPathGeometry path)
    {
        ReadOnlySpan<float> coords = path.Coordinates;
        int c = 0;
        foreach (BinkPathVerb verb in path.Verbs)
        {
            switch (verb)
            {
                case BinkPathVerb.MoveTo:
                    Write(BinkCanvasCommand.MoveTo);
                    Write(coords[c++]);
                    Write(coords[c++]);
                    break;
                case BinkPathVerb.LineTo:
                    Write(BinkCanvasCommand.LineTo);
                    Write(coords[c++]);
                    Write(coords[c++]);
                    break;
                case BinkPathVerb.QuadTo:
                    Write(BinkCanvasCommand.QuadTo);
                    Write(coords[c++]);
                    Write(coords[c++]);
                    Write(coords[c++]);
                    Write(coords[c++]);
                    break;
                case BinkPathVerb.CubicTo:
                    Write(BinkCanvasCommand.CubicTo);
                    for (int i = 0; i < 6; i++) Write(coords[c++]);
                    break;
                case BinkPathVerb.Close:
                    Write(BinkCanvasCommand.ClosePath);
                    break;
            }
        }
    }

    /// <summary>
    /// Returns how a command refers to a string: its index in this frame's table, or, for a long string the
    /// browser keeps, <c>-(slot + 1)</c>. An embedded image is a data URI of megabytes; without retaining it, it
    /// crossed the interop boundary again on every redraw of the page it sat on.
    /// </summary>
    private int StringReference(string value)
    {
        if (_resources is not { } resources || value.Length < BinkCanvasResourceCache.RetainedStringLength) return InternString(value);
        if (!resources.TryUseString(value, out int slot))
        {
            slot = resources.AddString(value);
            int index = InternString(value);
            Write(BinkCanvasCommand.RetainString);
            Write(slot);
            Write(index);
        }
        return -(slot + 1);
    }

    private int InternString(string value)
    {
        if (_stringIndex.TryGetValue(value, out int index)) return index;
        index = _strings.Count;
        _strings.Add(value);
        _stringIndex[value] = index;
        return index;
    }

    private void Write(BinkCanvasCommand command) => Write((double)(byte)command);

    // A colour is two values, 24-bit RGB and the alpha byte, because packed ARGB does not fit a 32-bit float exactly.
    private void Write(BinkColor color)
    {
        Write((double)(((uint)color.R << 16) | ((uint)color.G << 8) | color.B));
        Write((double)color.A);
    }

    private void Write(float value) => Write((double)value);

    private void Write(int value) => Write((double)value);

    private void Write(double value)
    {
        if (_count == _buffer.Length) Array.Resize(ref _buffer, _buffer.Length * 2);
        _buffer[_count++] = value;
    }
}
