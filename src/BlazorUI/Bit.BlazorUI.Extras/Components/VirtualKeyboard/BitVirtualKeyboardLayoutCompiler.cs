using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

// Compiles a BitVirtualKeyboardLayout for a purpose, a mode and a culture: merges every key of its rows with its bag entry
// and its built-in key, leaves out what the purpose and the mode leave out, derives the shift and caps layers a layout
// with those keys lacks, places the keys on one grid shared by every layer, and resolves their labels. A layout is data
// from the page, so a mistake in it is an ArgumentException naming the layout, the layer, the row and the key.
internal static class BitVirtualKeyboardLayoutCompiler
{
    private const int MaxCacheEntries = 256;

    // Compiled layouts by content, not by reference: a page that builds its layout (or a `with` copy of a built-in one)
    // on every render compiles it once.
    private static readonly ConcurrentDictionary<string, BitVirtualKeyboardCompiledLayout> _cache = new(StringComparer.Ordinal);

    private sealed class Resolved
    {
        public required BitVirtualKeyboardKey Key { get; init; }
        public string? BuiltIn { get; init; }
        public string? BuiltInLabel { get; init; }
        public required string RowText { get; init; }
    }

    public static BitVirtualKeyboardCompiledLayout Compile(BitVirtualKeyboardLayout layout, BitVirtualKeyboardCompileOptions options)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ScrambleSeed.HasValue) return CompileCore(layout, options);

        var cacheKey = string.Join('\u001F', layout.ToJson(), options.Purpose, options.Mode, options.LanguagesCount,
                                   options.NextIndicator, options.Culture.Name, options.AutoShift, options.Digits, options.NumberTarget);

        if (_cache.TryGetValue(cacheKey, out var cached)) return cached;

        var compiled = CompileCore(layout, options);

        if (_cache.Count >= MaxCacheEntries) _cache.Clear();
        _cache[cacheKey] = compiled;

        return compiled;
    }



    private static BitVirtualKeyboardCompiledLayout CompileCore(BitVirtualKeyboardLayout layout, BitVirtualKeyboardCompileOptions options)
    {
        // 1. the layout itself
        if (string.IsNullOrWhiteSpace(layout.Id)) throw new ArgumentException("A virtual keyboard layout needs an Id.");
        if (string.IsNullOrWhiteSpace(layout.Name)) throw Error(layout, "needs a Name");
        if (layout.Layers is null || layout.Layers.Count == 0) throw Error(layout, "needs at least one layer");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var layer in layout.Layers)
        {
            if (string.IsNullOrWhiteSpace(layer.Id)) throw Error(layout, "has a layer with no Id");
            if (ids.Add(layer.Id) is false) throw Error(layout, $"has two layers with the Id \"{layer.Id}\"");
            if (layer.Rows is null || layer.Rows.Count == 0) throw Error(layout, $"has no rows in the layer \"{layer.Id}\"");
            for (var r = 0; r < layer.Rows.Count; r++)
            {
                if (layer.Rows[r]?.Keys is not { Count: > 0 }) throw Error(layout, $"has an empty row {r + 1} in the layer \"{layer.Id}\"");
            }
        }

        var isPad = layout.Id.StartsWith("x-", StringComparison.Ordinal);
        var caseCulture = GetCulture(layout.Culture) ?? options.Culture;
        var hasAltGr = layout.Layers.Any(l => (l.Modifiers & BitVirtualKeyboardModifiers.AltGr) != 0);

        // 2-4. every key merged, filtered and varied for the purpose, the mode and the environment
        var layers = new List<(BitVirtualKeyboardLayer Layer, List<List<Resolved>> Rows)>();
        foreach (var layer in layout.Layers)
        {
            var rows = new List<List<Resolved>>();
            for (var r = 0; r < layer.Rows.Count; r++)
            {
                var row = layer.Rows[r];
                var resolvedRow = new List<Resolved>();
                foreach (var rowKey in row.Keys)
                {
                    var resolved = Resolve(layout, layer, r, rowKey, options);
                    var key = resolved.Key;

                    var dropped = key.Purposes is not null && key.Purposes.Contains(options.Purpose) is false;
                    if (dropped is false && key.Variations is not null && key.Variations.TryGetValue(options.Purpose, out var variation))
                    {
                        key = Merge(key, variation);
                    }

                    var action = key.Action ?? BitVirtualKeyboardKeyAction.Character;
                    dropped |= action is BitVirtualKeyboardKeyAction.Hide && options.Mode is BitVirtualKeyboardMode.Inline;
                    dropped |= action is BitVirtualKeyboardKeyAction.NextLayout && options.LanguagesCount < 2;
                    dropped |= action is BitVirtualKeyboardKeyAction.AltGr && hasAltGr is false;

                    if (dropped)
                    {
                        // a pad keeps its grid rectangular; a language layout's stretching key takes the room
                        if (isPad)
                        {
                            resolvedRow.Add(new() { Key = new() { Id = "gap", Gap = true, Width = key.Width, Height = key.Height }, BuiltIn = "gap", RowText = resolved.RowText });
                        }
                        continue;
                    }

                    // 6. the digits of a purpose pad
                    if (isPad && options.NumberTarget is false && options.Digits is not BitVirtualKeyboardDigits.Latin
                        && key.Output is [>= '0' and <= '9'] digit)
                    {
                        var zero = options.Digits is BitVirtualKeyboardDigits.Persian ? 0x06F0 : 0x0660;
                        key = key with { Output = ((char)(zero + digit[0] - '0')).ToString() };
                    }

                    resolvedRow.Add(new() { Key = key, BuiltIn = resolved.BuiltIn, BuiltInLabel = resolved.BuiltInLabel, RowText = resolved.RowText });
                }
                rows.Add(resolvedRow);
            }
            layers.Add((layer, rows));
        }

        // 5. the shift and caps layers a layout with Shift or Caps Lock keys lacks
        var allKeys = layers.SelectMany(l => l.Rows.SelectMany(r => r)).Select(r => r.Key.Action).ToList();
        var shiftLayer = layout.Layers.FirstOrDefault(l => l.Modifiers == BitVirtualKeyboardModifiers.Shift)?.Id;
        var capsLayer = layout.Layers.FirstOrDefault(l => l.Modifiers == BitVirtualKeyboardModifiers.Caps)?.Id;

        if (shiftLayer is null && options.AutoShift && allKeys.Contains(BitVirtualKeyboardKeyAction.Shift) && ids.Contains("shift") is false)
        {
            layers.Add((new() { Id = "shift", Modifiers = BitVirtualKeyboardModifiers.Shift, Rows = layout.Layers[0].Rows }, Derive(layout, layers[0].Rows, caseCulture)));
            ids.Add("shift");
            shiftLayer = "shift";
        }

        if (capsLayer is null && allKeys.Contains(BitVirtualKeyboardKeyAction.CapsLock) && ids.Contains("caps") is false)
        {
            layers.Add((new() { Id = "caps", Modifiers = BitVirtualKeyboardModifiers.Caps, Rows = layout.Layers[0].Rows }, Derive(layout, layers[0].Rows, caseCulture)));
            ids.Add("caps");
            capsLayer = "caps";
        }

        // the keys, every value settled
        var baseLayer = layout.Layers[0].Id;
        var keyed = new List<(BitVirtualKeyboardLayer Layer, List<List<BitVirtualKeyboardCompiledKey>> Rows)>();
        foreach (var (layer, rows) in layers)
        {
            var compiledRows = new List<List<BitVirtualKeyboardCompiledKey>>();
            for (var r = 0; r < rows.Count; r++)
            {
                compiledRows.Add([.. rows[r].Select(resolved => Settle(layout, layer, r, resolved, baseLayer, ids))]);
            }
            keyed.Add((layer, compiledRows));
        }

        // 8. the grid
        var columns = Place(layout, keyed);

        // 7. the scramble, which moves keys between slots and leaves every slot as it is
        if (options.ScrambleSeed.HasValue)
        {
            var random = new Random(options.ScrambleSeed.Value);
            foreach (var (_, rows) in keyed)
            {
                var keys = rows.SelectMany(r => r).Where(k => k.Scramble && k.Gap is false && k.Blank is false).ToList();
                var slots = keys.Select(k => (k.Row, k.Column, k.RowSpan, k.ColumnSpan)).ToList();
                for (var i = slots.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (slots[i], slots[j]) = (slots[j], slots[i]);
                }
                for (var i = 0; i < keys.Count; i++)
                {
                    (keys[i].Row, keys[i].Column, keys[i].RowSpan, keys[i].ColumnSpan) = slots[i];
                }
            }
        }

        var index = 0;
        var compiledLayers = new List<BitVirtualKeyboardCompiledLayer>();
        foreach (var (layer, rows) in keyed)
        {
            var keys = new List<BitVirtualKeyboardCompiledKey>();
            foreach (var key in rows.SelectMany(r => r))
            {
                key.Index = index++;
                keys.Add(key);
            }
            compiledLayers.Add(new() { Id = layer.Id, Name = layer.Name, Modifiers = layer.Modifiers, Rows = rows.Count, Keys = keys });
        }

        return new()
        {
            Id = layout.Id,
            Name = layout.Name,
            Indicator = layout.Indicator,
            Direction = layout.Direction,
            Form = layout.Form,
            Columns = columns,
            Layers = compiledLayers,
            DeadKeys = layout.DeadKeys ?? [],
            ShiftLayer = shiftLayer,
            CapsLayer = capsLayer,
            AltGrLayer = layout.Layers.FirstOrDefault(l => l.Modifiers == BitVirtualKeyboardModifiers.AltGr)?.Id,
            ShiftAltGrLayer = layout.Layers.FirstOrDefault(l => l.Modifiers == (BitVirtualKeyboardModifiers.Shift | BitVirtualKeyboardModifiers.AltGr))?.Id,
            Source = layout,
        };
    }

    private static Resolved Resolve(BitVirtualKeyboardLayout layout, BitVirtualKeyboardLayer layer, int row, BitVirtualKeyboardKey rowKey, BitVirtualKeyboardCompileOptions options)
    {
        var rowText = $"row {row + 1} of the layer \"{layer.Id}\"";

        if (rowKey is null) throw Error(layout, $"has a null key in {rowText}");

        if (rowKey.Id is { Length: > 0 } id)
        {
            var colon = id.IndexOf(':');
            var name = colon < 0 ? id : id[..colon];
            var argument = colon < 0 ? null : id[(colon + 1)..];

            var bag = layout.Keys?.GetValueOrDefault(id) ?? (colon < 0 ? null : layout.Keys?.GetValueOrDefault(name));
            var builtIn = BitVirtualKeyboardBuiltInKeys.Get(name, argument, options);

            if (bag is null && builtIn is null) throw Error(layout, $"names the key \"{{{id}}}\" in {rowText}, which is neither in its Keys nor a built-in key");

            // the label of a built-in key ranks below the Displays of the layout, so it is kept apart from the merge
            var merged = Merge(Merge(builtIn is null ? null : builtIn with { Label = null }, bag), rowKey with { Id = id });
            return new() { Key = merged, BuiltIn = builtIn is null ? null : name, BuiltInLabel = builtIn?.Label, RowText = rowText };
        }

        if (rowKey.Output is { Length: > 0 } output && layout.Keys?.GetValueOrDefault(output) is { } entry)
        {
            return new() { Key = Merge(entry with { Output = entry.Output ?? output }, rowKey with { Output = null }), RowText = rowText };
        }

        return new() { Key = rowKey, RowText = rowText };
    }

    private static BitVirtualKeyboardCompiledKey Settle(BitVirtualKeyboardLayout layout, BitVirtualKeyboardLayer layer, int row, Resolved resolved,
                                                        string baseLayer, HashSet<string> layers)
    {
        var key = resolved.Key;
        var where = $"the key \"{Describe(key)}\" in {resolved.RowText}";
        var action = key.Action ?? BitVirtualKeyboardKeyAction.Character;
        var gap = key.Gap ?? false;
        var blank = key.Blank ?? false;
        var width = key.Width ?? 1;
        var height = key.Height ?? 1;
        var offset = key.Offset ?? 0;

        if (width <= 0 || IsQuarter(width) is false) throw Error(layout, $"has {where} {Num(width)} units wide; a width is a positive multiple of 0.25");
        if (offset < 0 || IsQuarter(offset) is false) throw Error(layout, $"has {where} with an offset of {Num(offset)}; an offset is a multiple of 0.25, 0 or more");
        if (height < 1) throw Error(layout, $"has {where} {height} rows high; a key is at least one row high");

        var layerTarget = key.Layer ?? (resolved.BuiltIn == "abc" ? baseLayer : null);

        if (gap is false && blank is false)
        {
            switch (action)
            {
                case BitVirtualKeyboardKeyAction.Character when string.IsNullOrEmpty(key.Output):
                    throw Error(layout, $"has {where}, which types nothing: give it an Output or an Action");
                case BitVirtualKeyboardKeyAction.SwitchLayer when layerTarget is null || layers.Contains(layerTarget) is false:
                    throw Error(layout, $"has {where}, which switches to the layer \"{layerTarget}\" it does not have");
                case BitVirtualKeyboardKeyAction.DeadKey when key.DeadKey is null || layout.DeadKeys?.ContainsKey(key.DeadKey) is not true:
                    throw Error(layout, $"has {where}, whose dead key \"{key.DeadKey}\" is not in its DeadKeys");
                case BitVirtualKeyboardKeyAction.Custom when string.IsNullOrEmpty(key.Command):
                    throw Error(layout, $"has {where}, a Custom key with no Command");
            }

            if (key.NextLayer is not null && layers.Contains(key.NextLayer) is false)
            {
                throw Error(layout, $"has {where}, which returns to the layer \"{key.NextLayer}\" it does not have");
            }
        }

        var longPress = key.LongPress
                        ?? (key.Output is not null && layout.LongPress?.GetValueOrDefault(key.Output) is { } list
                            ? [.. list.Split(' ', StringSplitOptions.RemoveEmptyEntries)]
                            : []);
        var multiTap = key.MultiTap ?? [];
        if (longPress.Count == 0 && multiTap.Count > 0) longPress = [.. multiTap];

        var builtIn = resolved.BuiltIn ?? BuiltInOf(action);

        return new()
        {
            KeyId = key.Id ?? key.Output ?? builtIn ?? "",
            BuiltIn = builtIn,
            Action = action,
            Kind = KindOf(action),
            Output = key.Output,
            Label = gap ? null : LabelOf(layout, key, resolved.BuiltIn, resolved.BuiltInLabel),
            Hint = key.Hint,
            Icon = BitIconInfo.From(key.Icon, key.IconName),
            AriaLabel = key.AriaLabel,
            Layer = layerTarget,
            NextLayer = key.NextLayer,
            DeadKey = key.DeadKey,
            Command = key.Command,
            ColumnSpan = Q(width),
            RowSpan = height,
            Column = Q(offset), // the offset, until the key is placed
            Gap = gap,
            Blank = blank,
            LongPress = longPress,
            LongPressDefault = key.LongPressDefault,
            MultiTap = multiTap,
            Repeat = longPress.Count == 0 && (key.Repeat ?? action is BitVirtualKeyboardKeyAction.Backspace or BitVirtualKeyboardKeyAction.Delete
                                                                      or BitVirtualKeyboardKeyAction.Space or BitVirtualKeyboardKeyAction.ArrowLeft
                                                                      or BitVirtualKeyboardKeyAction.ArrowRight or BitVirtualKeyboardKeyAction.ArrowUp
                                                                      or BitVirtualKeyboardKeyAction.ArrowDown),
            Preview = gap is false && blank is false && (key.Preview ?? action is BitVirtualKeyboardKeyAction.Character),
            Scramble = gap is false && blank is false && (key.Scramble ?? (action is BitVirtualKeyboardKeyAction.Character && IsSingleDigit(key.Output))),
            Class = key.Class,
            Style = key.Style,
            Source = key,
        };
    }

    // Places every key on one grid of quarter key units, as wide as the widest row of any layer so the keys keep their
    // size when the layer changes; returns its width.
    private static int Place(BitVirtualKeyboardLayout layout, List<(BitVirtualKeyboardLayer Layer, List<List<BitVirtualKeyboardCompiledKey>> Rows)> layers)
    {
        var naturals = new List<int[]>();
        var helds = new List<int[]>();

        foreach (var (layer, rows) in layers)
        {
            var natural = new int[rows.Count];
            var held = new int[rows.Count];
            for (var r = 0; r < rows.Count; r++)
            {
                foreach (var key in rows[r])
                {
                    natural[r] += key.Column + key.ColumnSpan;
                    if (r + key.RowSpan > rows.Count)
                    {
                        throw Error(layout, $"has the key \"{key.KeyId}\" in row {r + 1} of the layer \"{layer.Id}\" {key.RowSpan} rows high, past the last row");
                    }
                    for (var below = r + 1; below < r + key.RowSpan; below++)
                    {
                        held[below] += key.ColumnSpan;
                    }
                }
            }
            for (var r = 0; r < rows.Count; r++) natural[r] += held[r];
            naturals.Add(natural);
            helds.Add(held);
        }

        var columns = Math.Max(4, naturals.SelectMany(n => n).DefaultIfEmpty(0).Max());

        for (var l = 0; l < layers.Count; l++)
        {
            var (layer, rows) = layers[l];
            var occupied = new bool[rows.Count, columns];

            for (var r = 0; r < rows.Count; r++)
            {
                var leftover = columns - naturals[l][r];
                var stretching = rows[r].Where(k => k.Source.Stretch is true).ToList();
                var start = 0;

                if (stretching.Count > 0)
                {
                    var share = leftover / stretching.Count;
                    for (var i = 0; i < stretching.Count; i++)
                    {
                        stretching[i].ColumnSpan += share + (i == stretching.Count - 1 ? leftover - share * stretching.Count : 0);
                    }
                }
                else if (helds[l][r] == 0)
                {
                    var align = layer.Rows[r].Align;
                    start = align switch
                    {
                        BitVirtualKeyboardRowAlign.Start => 0,
                        BitVirtualKeyboardRowAlign.End => leftover,
                        _ => leftover / 2,
                    };
                }

                var cursor = start;
                foreach (var key in rows[r])
                {
                    cursor += key.Column; // the offset

                    while (Fits(occupied, r, cursor, key.ColumnSpan, key.RowSpan, columns) is false)
                    {
                        if (cursor + key.ColumnSpan > columns)
                        {
                            throw Error(layout, $"has no room for the key \"{key.KeyId}\" in row {r + 1} of the layer \"{layer.Id}\"");
                        }
                        cursor++;
                    }

                    for (var rr = r; rr < r + key.RowSpan; rr++)
                    {
                        for (var c = cursor; c < cursor + key.ColumnSpan; c++) occupied[rr, c] = true;
                    }

                    key.Row = r;
                    key.Column = cursor;
                    cursor += key.ColumnSpan;
                }
            }
        }

        return columns;
    }

    private static bool Fits(bool[,] occupied, int row, int column, int width, int height, int columns)
    {
        if (column + width > columns) return false;

        for (var r = row; r < row + height; r++)
        {
            for (var c = column; c < column + width; c++)
            {
                if (occupied[r, c]) return false;
            }
        }

        return true;
    }

    // A layer derived from the base one, its single letters upper-cased in the culture of the layout.
    private static List<List<Resolved>> Derive(BitVirtualKeyboardLayout layout, List<List<Resolved>> baseRows, CultureInfo culture)
    {
        return [.. baseRows.Select(row => row.Select(resolved =>
        {
            var key = resolved.Key;
            if ((key.Action ?? BitVirtualKeyboardKeyAction.Character) is not BitVirtualKeyboardKeyAction.Character
                || key.Output is not { } output
                || IsSingleLetter(output) is false)
            {
                return resolved;
            }

            var upper = ToUpper(output, culture);
            if (upper == output) return resolved;

            var longPress = key.LongPress?.Select(a => ToUpper(a, culture)).ToList();
            if (longPress is null && layout.LongPress?.ContainsKey(upper) is not true
                && layout.LongPress?.GetValueOrDefault(output) is { } list)
            {
                longPress = [.. list.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(a => ToUpper(a, culture))];
            }

            return new Resolved
            {
                Key = key with { Output = upper, Label = key.Label is null ? null : ToUpper(key.Label, culture), LongPress = longPress },
                BuiltIn = resolved.BuiltIn,
                BuiltInLabel = resolved.BuiltInLabel,
                RowText = resolved.RowText,
            };
        }).ToList())];
    }

    // Upper case in a culture, with the Turkish and Azerbaijani dotted and dotless i set down here: Blazor WebAssembly
    // loads no Turkish data for an app of another culture, and invariant globalization has no Turkish casing at all.
    internal static string ToUpper(string text, CultureInfo culture)
    {
        if (culture.TwoLetterISOLanguageName is "tr" or "az")
        {
            text = text.Replace('i', '\x0130').Replace('\x0131', 'I');
        }

        return text.ToUpper(culture);
    }

    private static string? LabelOf(BitVirtualKeyboardLayout layout, BitVirtualKeyboardKey key, string? builtIn, string? builtInLabel)
    {
        var label = key.Label
                    ?? (key.Id is not null ? layout.Displays?.GetValueOrDefault(key.Id) : null)
                    ?? (key.Output is not null ? layout.Displays?.GetValueOrDefault(key.Output) : null)
                    ?? builtInLabel;

        if (label is null)
        {
            if (builtIn is "layer" && key.Layer is not null)
            {
                label = layout.Layers.FirstOrDefault(l => l.Id == key.Layer)?.Name;
            }
            else if (builtIn is "dead" && key.DeadKey is not null)
            {
                label = layout.DeadKeys?.GetValueOrDefault(key.DeadKey)?.GetValueOrDefault(" ");
            }
            else if (builtIn is not null && (BitVirtualKeyboardBuiltInKeys.Iconed.Contains(builtIn) || BitVirtualKeyboardBuiltInKeys.StringsLabeled.Contains(builtIn)))
            {
                label = null;
            }
            else if ((key.Action ?? BitVirtualKeyboardKeyAction.Character) is BitVirtualKeyboardKeyAction.Character)
            {
                label = key.Output;
            }
        }

        // A combining mark on its own is drawn on a dotted circle (U+25CC), as Unicode shows it.
        if (label is { Length: > 0 } && label.All(c => CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark))
        {
            label = "\U000025CC" + label;
        }

        return label;
    }

    private static BitVirtualKeyboardKeyKind KindOf(BitVirtualKeyboardKeyAction action) => action switch
    {
        BitVirtualKeyboardKeyAction.Character or BitVirtualKeyboardKeyAction.Space or BitVirtualKeyboardKeyAction.DeadKey => BitVirtualKeyboardKeyKind.Character,
        BitVirtualKeyboardKeyAction.Enter => BitVirtualKeyboardKeyKind.Primary,
        BitVirtualKeyboardKeyAction.Shift or BitVirtualKeyboardKeyAction.CapsLock or BitVirtualKeyboardKeyAction.AltGr => BitVirtualKeyboardKeyKind.Modifier,
        _ => BitVirtualKeyboardKeyKind.Function,
    };

    // The built-in key whose icon and words a key of a bag takes by its action (a bag "iso-enter" is drawn as Enter).
    private static string? BuiltInOf(BitVirtualKeyboardKeyAction action) => action switch
    {
        BitVirtualKeyboardKeyAction.Backspace => "bksp",
        BitVirtualKeyboardKeyAction.Delete => "del",
        BitVirtualKeyboardKeyAction.Enter => "enter",
        BitVirtualKeyboardKeyAction.Space => "space",
        BitVirtualKeyboardKeyAction.Tab => "tab",
        BitVirtualKeyboardKeyAction.ShiftTab => "shifttab",
        BitVirtualKeyboardKeyAction.Shift => "shift",
        BitVirtualKeyboardKeyAction.CapsLock => "caps",
        BitVirtualKeyboardKeyAction.AltGr => "altgr",
        BitVirtualKeyboardKeyAction.NextLayout => "lang",
        BitVirtualKeyboardKeyAction.Hide => "hide",
        BitVirtualKeyboardKeyAction.Clear => "clear",
        BitVirtualKeyboardKeyAction.Accept => "accept",
        BitVirtualKeyboardKeyAction.Cancel => "cancel",
        BitVirtualKeyboardKeyAction.ArrowLeft => "left",
        BitVirtualKeyboardKeyAction.ArrowRight => "right",
        BitVirtualKeyboardKeyAction.ArrowUp => "up",
        BitVirtualKeyboardKeyAction.ArrowDown => "down",
        BitVirtualKeyboardKeyAction.Home => "home",
        BitVirtualKeyboardKeyAction.End => "end",
        _ => null,
    };

    internal static BitVirtualKeyboardKey Merge(BitVirtualKeyboardKey? under, BitVirtualKeyboardKey? over)
    {
        if (under is null) return over ?? new();
        if (over is null) return under;

        return new()
        {
            Id = over.Id ?? under.Id,
            Output = over.Output ?? under.Output,
            Label = over.Label ?? under.Label,
            Hint = over.Hint ?? under.Hint,
            Icon = over.Icon ?? under.Icon,
            IconName = over.IconName ?? under.IconName,
            AriaLabel = over.AriaLabel ?? under.AriaLabel,
            Action = over.Action ?? under.Action,
            Layer = over.Layer ?? under.Layer,
            NextLayer = over.NextLayer ?? under.NextLayer,
            DeadKey = over.DeadKey ?? under.DeadKey,
            Command = over.Command ?? under.Command,
            Width = over.Width ?? under.Width,
            Height = over.Height ?? under.Height,
            Offset = over.Offset ?? under.Offset,
            Stretch = over.Stretch ?? under.Stretch,
            Gap = over.Gap ?? under.Gap,
            Blank = over.Blank ?? under.Blank,
            LongPress = over.LongPress ?? under.LongPress,
            LongPressDefault = over.LongPressDefault ?? under.LongPressDefault,
            MultiTap = over.MultiTap ?? under.MultiTap,
            Repeat = over.Repeat ?? under.Repeat,
            Preview = over.Preview ?? under.Preview,
            Purposes = over.Purposes ?? under.Purposes,
            Variations = over.Variations ?? under.Variations,
            Scramble = over.Scramble ?? under.Scramble,
            Class = over.Class ?? under.Class,
            Style = over.Style ?? under.Style,
        };
    }

    internal static bool IsSingleLetter(string text)
    {
        if (string.IsNullOrEmpty(text) || new StringInfo(text).LengthInTextElements != 1) return false;

        return Rune.TryGetRuneAt(text, 0, out var rune) && Rune.IsLetter(rune);
    }

    private static bool IsSingleDigit(string? text)
    {
        if (string.IsNullOrEmpty(text) || Rune.DecodeFromUtf16(text, out var rune, out var length) is not System.Buffers.OperationStatus.Done) return false;

        return length == text.Length && Rune.IsDigit(rune);
    }

    private static CultureInfo? GetCulture(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static bool IsQuarter(double value) => Math.Abs(value * 4 - Math.Round(value * 4)) < 1e-9;

    private static int Q(double value) => (int)Math.Round(value * 4);

    private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Describe(BitVirtualKeyboardKey key) => key.Id is not null ? $"{{{key.Id}}}" : key.Output ?? "";

    private static ArgumentException Error(BitVirtualKeyboardLayout layout, string problem) => new($"The virtual keyboard layout \"{layout.Id}\" {problem}.");
}