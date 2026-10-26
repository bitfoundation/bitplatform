using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiJsonView renders a JSON value as a collapsible tree.
/// </summary>
/// <remarks>
/// Used wherever the AI components have structured data and no better renderer: tool arguments, tool results,
/// structured outputs. Large containers start collapsed so a thousand-row result does not push the rest of the
/// conversation off screen. A node is identified by its path from the root, so toggling one array item, or one of two
/// same-named keys in different branches, never toggles the others.
/// </remarks>
public partial class BitAiJsonView : BitAiComponentBase
{
    // What the user toggled away from the default, by path. Kept apart rather than as one "open" set so a container
    // that has not been touched keeps following DefaultExpandDepth and LargeContainerSize when those change.
    private readonly HashSet<string> _collapsed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _expanded = new(StringComparer.Ordinal);



    /// <summary>
    /// Gets or sets the cascading parameters for the JSON view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple JSON view components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiJsonViewParams.ParamName)]
    public BitAiJsonViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the JSON view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiJsonViewClassStyles? Classes { get; set; }

    /// <summary>
    /// How many levels of the tree are expanded before the containers below them start collapsed.
    /// <br />
    /// The default value is <strong>2</strong>.
    /// </summary>
    [Parameter] public int DefaultExpandDepth { get; set; } = 2;

    /// <summary>
    /// The number of entries past which a container starts collapsed whatever its depth.
    /// <br />
    /// The default value is <strong>24</strong>.
    /// </summary>
    [Parameter] public int LargeContainerSize { get; set; } = 24;

    /// <summary>
    /// Custom CSS styles for different parts of the JSON view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiJsonViewClassStyles? Styles { get; set; }

    /// <summary>
    /// The JSON value to render. Nothing is rendered inside the view while it is null.
    /// </summary>
    [Parameter] public JsonElement? Value { get; set; }



    protected override string RootElementClass => "bit-aijsv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiJsonViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    // One node of the tree: a container is a row whose button toggles it - the chevron, its key and its opening brace,
    // or the brace with its entry count while it is collapsed - then its entries, indented as a group, and the row of
    // its closing brace; a scalar is a single row. The whole head of a container is the button, so the key is what
    // names the toggle for a screen reader, and aria-expanded is what says whether it is open. A container with no key
    // - the outermost one, an entry of an array - would be named by its brace alone, the same for every one of them,
    // so it is named after where it is (the root, its index) ahead of the brace it shows.
    private RenderFragment RenderNode(JsonElement value, string? name, int? index, string path, int depth) => builder =>
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
            case JsonValueKind.Array:
            {
                var isObject = value.ValueKind is JsonValueKind.Object;
                var count = isObject ? value.EnumerateObject().Count() : value.GetArrayLength();
                var open = IsOpen(path, depth, count);
                var (start, end) = isObject ? ("{", "}") : ("[", "]");
                var braces = open ? start : $"{start} {count} {end}";
                var disabled = Disabled;
                var ariaLabel = name is not null ? null : $"{(index is { } i ? Strings.JsonArrayItem(i) : Strings.JsonRoot)} {braces}";

                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "style", Styles?.Row);
                builder.AddAttribute(2, "class", Join("bit-aijsv-row", Classes?.Row));

                builder.OpenElement(3, "button");
                builder.AddAttribute(4, "type", "button");
                builder.AddAttribute(5, "style", Styles?.Toggle);
                builder.AddAttribute(6, "class", Join("bit-aijsv-tgl", Classes?.Toggle));
                builder.AddAttribute(7, "aria-label", ariaLabel);
                builder.AddAttribute(8, "aria-expanded", open ? "true" : "false");
                builder.AddAttribute(9, "disabled", disabled);
                builder.AddAttribute(10, "aria-disabled", disabled ? "true" : null);
                builder.AddAttribute(11, "onclick", EventCallback.Factory.Create(this, () => Toggle(path, open)));

                // A chevron pointing along the reading direction while collapsed (bit-aijsv-tcl), which the stylesheet
                // mirrors in a right-to-left tree, and turned down while open (bit-aijsv-top) - the one glyph of BitNav's, which the
                // core stylesheet always carries. A glyph of the toggle's own rather than a BitIcon, so it takes the
                // color of the text it sits in, the way the brace beside it does.
                builder.OpenElement(12, "i");
                builder.AddAttribute(13, "aria-hidden", "true");
                builder.AddAttribute(14, "style", Styles?.ToggleIcon);
                builder.AddAttribute(15, "class", Join(open ? "bit-aijsv-tic bit-aijsv-top bit-icon bit-icon--ChevronRight"
                                                            : "bit-aijsv-tic bit-aijsv-tcl bit-icon bit-icon--ChevronRight", Classes?.ToggleIcon));
                builder.CloseElement();

                if (name is not null)
                {
                    builder.OpenElement(16, "span");
                    builder.AddAttribute(17, "style", Styles?.Key);
                    builder.AddAttribute(18, "class", Join("bit-aijsv-key", Classes?.Key));
                    builder.AddContent(19, Quote(name));
                    builder.CloseElement();
                    builder.AddContent(20, ": ");
                }

                builder.AddContent(21, braces);
                builder.CloseElement();
                builder.CloseElement();

                if (open)
                {
                    builder.OpenElement(22, "span");
                    builder.AddAttribute(23, "style", Styles?.Group);
                    builder.AddAttribute(24, "class", Join("bit-aijsv-grp", Classes?.Group));

                    if (isObject)
                    {
                        foreach (var property in value.EnumerateObject())
                        {
                            builder.AddContent(25, RenderNode(property.Value, property.Name, null, $"{path}.{property.Name.Length}:{property.Name}", depth + 1));
                        }
                    }
                    else
                    {
                        var position = 0;
                        foreach (var item in value.EnumerateArray())
                        {
                            builder.AddContent(26, RenderNode(item, null, position, $"{path}[{position}]", depth + 1));
                            position++;
                        }
                    }

                    builder.CloseElement();

                    builder.OpenElement(27, "span");
                    builder.AddAttribute(28, "style", Styles?.Row);
                    builder.AddAttribute(29, "class", Join("bit-aijsv-row", Classes?.Row));
                    builder.AddContent(30, end);
                    builder.CloseElement();
                }

                break;
            }

            default:
            {
                builder.OpenElement(31, "span");
                builder.AddAttribute(32, "style", Styles?.Row);
                builder.AddAttribute(33, "class", Join("bit-aijsv-row", Classes?.Row));

                if (name is not null)
                {
                    builder.OpenElement(34, "span");
                    builder.AddAttribute(35, "style", Styles?.Key);
                    builder.AddAttribute(36, "class", Join("bit-aijsv-key", Classes?.Key));
                    builder.AddContent(37, Quote(name));
                    builder.CloseElement();
                    builder.AddContent(38, ": ");
                }

                var (scalarClass, scalarStyle) = ScalarPart(value.ValueKind);

                builder.OpenElement(39, "span");
                builder.AddAttribute(40, "style", scalarStyle);
                builder.AddAttribute(41, "class", scalarClass);
                builder.AddContent(42, Scalar(value));
                builder.CloseElement();

                builder.CloseElement();
                break;
            }
        }
    };

    private bool IsOpen(string path, int depth, int count)
    {
        if (_collapsed.Contains(path)) return false;
        if (_expanded.Contains(path)) return true;

        return depth < DefaultExpandDepth && count <= LargeContainerSize;
    }

    private void Toggle(string path, bool wasOpen)
    {
        if (Disabled) return;

        if (wasOpen)
        {
            _expanded.Remove(path);
            _collapsed.Add(path);
        }
        else
        {
            _collapsed.Remove(path);
            _expanded.Add(path);
        }
    }

    private (string Class, string? Style) ScalarPart(JsonValueKind kind) => kind switch
    {
        JsonValueKind.String => (Join("bit-aijsv-str", Classes?.String), Styles?.String),
        JsonValueKind.Number => (Join("bit-aijsv-num", Classes?.Number), Styles?.Number),
        JsonValueKind.True or JsonValueKind.False => (Join("bit-aijsv-bln", Classes?.Boolean), Styles?.Boolean),
        _ => (Join("bit-aijsv-nul", Classes?.Null), Styles?.Null),
    };

    private static string Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => Quote(value.GetString()),
        JsonValueKind.Null or JsonValueKind.Undefined => "null",
        // A number or a boolean as the JSON it was written as: JsonElement.ToString() spells true as "True".
        _ => value.GetRawText(),
    };

    // A key or a string drawn the way JSON writes it: between quotes, with a quote, a backslash and a control character
    // escaped, so a value holding a quote still reads as one string and a line break in it does not break its row. What
    // else it holds - letters of any script included - is drawn as itself rather than as the \u escapes a serializer
    // writes for it.
    private static string Quote(string? text)
    {
        var builder = new System.Text.StringBuilder((text?.Length ?? 0) + 2);
        builder.Append('"');

        foreach (var c in text ?? string.Empty)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                default:
                    // The other control characters, and the line and paragraph separators that break a row the way a
                    // line feed does.
                    if (char.IsControl(c) || c is '\u2028' or '\u2029')
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }

        return builder.Append('"').ToString();
    }

    private static string Join(string own, string? custom) => custom.HasValue() ? $"{own} {custom}" : own;
}
