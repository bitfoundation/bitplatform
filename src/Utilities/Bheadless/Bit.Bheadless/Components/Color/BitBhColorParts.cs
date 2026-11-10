using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A slider for one channel of a color (React Aria / Ark ColorSlider): a <see cref="BitBhSlider"/> whose value is the
/// <see cref="Channel"/> of <see cref="Value"/> - hue in degrees, saturation, lightness and alpha in percent, RGB channels
/// 0–255 - announced as such ("120°", "40%"). Put a <see cref="BitBhSliderTrack"/> and a <see cref="BitBhSliderThumb"/> in it.
///
/// <para><b>Styling hooks:</b> the slider gets <c>--bit-bh-color-slider-gradient</c> - a <c>linear-gradient()</c> through the
/// channel's range (to the right; set your own angle by using the stops) - and <c>--bit-bh-color</c>, the color.</para>
///
/// <code>
/// &lt;BitBhColorSlider @bind-Value="color" Channel="BitBhColorChannel.Hue" class="hue"&gt;
///     &lt;BitBhSliderTrack class="hue-track" /&gt;
///     &lt;BitBhSliderThumb class="hue-thumb" aria-label="Hue" /&gt;
/// &lt;/BitBhColorSlider&gt;
/// </code>
/// </summary>
public class BitBhColorSlider : BitBhComponentBase
{
    private BitBhColor _value = BitBhColor.FromRgb(255, 0, 0);

    /// <summary>The color. Supplying it makes the slider controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public BitBhColor Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<BitBhColor> ValueChanged { get; set; }

    /// <summary>Initial color when uncontrolled. Default: red.</summary>
    [Parameter]
    public BitBhColor? DefaultValue { get; set; }

    /// <summary>Fires when the user finishes a change (pointer up, key press).</summary>
    [Parameter]
    public EventCallback<BitBhColor> OnValueCommit { get; set; }

    /// <summary>The channel the slider changes. Default: <see cref="BitBhColorChannel.Hue"/>.</summary>
    [Parameter]
    public BitBhColorChannel Channel { get; set; }

    /// <summary>Horizontal (default) or vertical.</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; }

    /// <summary>Disables the slider.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The track and the thumb.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    /// <summary>The current color.</summary>
    public BitBhColor CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    protected override void OnInitialized()
    {
        if (DefaultValue is { } initial) _value = initial;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var color = CurrentValue;
        var (min, max, step) = BitBhColor.RangeOf(Channel);
        var attributes = new Dictionary<string, object>(AdditionalAttributes ?? new(), StringComparer.OrdinalIgnoreCase);
        var style = $"--bit-bh-color-slider-gradient:{Gradient(color, Channel)};--bit-bh-color:{color.ToHex()}";
        // Handed to the slider as its consumer's: a style attribute of its own in strict CSP mode, else joined
        // ahead of the consumer's style so theirs wins.
        var styleAttribute = StyleAttribute;
        attributes[styleAttribute] = attributes.TryGetValue(styleAttribute, out var own) ? BitBhAttributeUtilities.JoinStyles(style, own?.ToString()) : style;
        // The slider takes it as the consumer's attribute, not a style of its own: it would not ask for the bridge.
        if (Options.StrictCsp) RequestStrictStyles();
        attributes["data-channel"] = Channel.ToString().ToLowerInvariant();

        builder.OpenComponent<BitBhSlider>(0);
        builder.AddMultipleAttributes(1, attributes);
        if (Id is not null) builder.AddComponentParameter(2, nameof(BitBhSlider.Id), Id);
        if (As is not null) builder.AddComponentParameter(3, nameof(BitBhSlider.As), As);
        builder.AddComponentParameter(4, nameof(BitBhSlider.Value), Math.Round(color.Get(Channel)));
        builder.AddComponentParameter(5, nameof(BitBhSlider.ValueChanged), EventCallback.Factory.Create<double>(this, v => SetAsync(color.With(Channel, v), commit: false)));
        builder.AddComponentParameter(6, nameof(BitBhSlider.OnValueCommit), EventCallback.Factory.Create<IReadOnlyList<double>>(this, values => OnValueCommit.InvokeAsync(
            // From the slider's values: a controlled Value only catches up when the parent renders again.
            values.Count > 0 ? CurrentValue.With(Channel, values[0]) : CurrentValue)));
        builder.AddComponentParameter(7, nameof(BitBhSlider.Min), min);
        builder.AddComponentParameter(8, nameof(BitBhSlider.Max), max);
        builder.AddComponentParameter(9, nameof(BitBhSlider.Step), step);
        builder.AddComponentParameter(10, nameof(BitBhSlider.LargeStep), Channel == BitBhColorChannel.Hue ? 15d : 10d);
        builder.AddComponentParameter(11, nameof(BitBhSlider.Orientation), Orientation);
        builder.AddComponentParameter(12, nameof(BitBhSlider.Disabled), Disabled);
        builder.AddComponentParameter(13, nameof(BitBhSlider.ValueText), (Func<double, string>)(v => BitBhColorAreaThumb.FormatChannel(Channel, v)));
        builder.AddComponentParameter(14, nameof(BitBhSlider.ChildContent), ChildContent);
        if (Ref is not null) builder.AddComponentParameter(15, nameof(BitBhSlider.Ref), Ref);
        builder.CloseComponent();
    }

    private async Task SetAsync(BitBhColor color, bool commit)
    {
        if (color == CurrentValue) return;
        if (!IsProvided(nameof(Value))) _value = color;
        await ValueChanged.InvokeAsync(color);
        if (commit) await OnValueCommit.InvokeAsync(color);
        StateHasChanged();
    }

    /// <summary>A <c>linear-gradient()</c> through a channel's range for <paramref name="color"/>.</summary>
    public static string Gradient(BitBhColor color, BitBhColorChannel channel, string direction = "to right")
    {
        if (channel == BitBhColorChannel.Hue)
            return $"linear-gradient({direction}, #ff0000, #ffff00, #00ff00, #00ffff, #0000ff, #ff00ff, #ff0000)";
        var (min, max, _) = BitBhColor.RangeOf(channel);
        var stops = channel == BitBhColorChannel.Lightness ? new[] { min, (min + max) / 2, max } : [min, max];
        var colors = stops.Select(v => channel == BitBhColorChannel.Alpha
            ? color.With(channel, v).ToHex(alpha: true)
            : (color with { Alpha = 1 }).With(channel, v).ToHex());
        return $"linear-gradient({direction}, {string.Join(", ", colors)})";
    }
}

/// <summary>
/// A swatch of a color (React Aria ColorSwatch): <c>role="img"</c> named by <see cref="Label"/> (default: the hex code), painted
/// with the color (<c>background-color</c> and <c>--bit-bh-color</c>).
/// </summary>
public class BitBhColorSwatch : BitBhComponentBase
{
    /// <summary>The color.</summary>
    [Parameter, EditorRequired]
    public BitBhColor Color { get; set; }

    /// <summary>The swatch's accessible name ("Ocean blue"). Default: the hex code.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Optional content (a checkered background for transparent colors …).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "img";
        attrs["aria-label"] = Label ?? Color.ToHex();
        // The swatch is the color: painting it is its job, not a style choice.
        attrs["style"] = $"background-color:{Color.ToRgbString()};--bit-bh-color:{Color.ToHex()}";
        attrs["data-color"] = Color.ToHex();
        return attrs;
    }
}

/// <summary>
/// A text input for a color (React Aria ColorField): shows the color as hex, and takes hex, <c>rgb()</c> or <c>hsl()</c> when it
/// is left or Enter is pressed; text that isn't a color reverts. Inside a <see cref="BitBhField"/> it takes the field's id.
/// </summary>
public class BitBhColorField : BitBhComponentBase
{
    private BitBhColor? _value;
    private string? _text;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>The color (null when empty). Supplying it makes the field controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public BitBhColor? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<BitBhColor?> ValueChanged { get; set; }

    /// <summary>Initial color when uncontrolled.</summary>
    [Parameter]
    public BitBhColor? DefaultValue { get; set; }

    /// <summary>Shows the alpha channel in the hex (<c>#rrggbbaa</c>) even when opaque. Default: only when not opaque.</summary>
    [Parameter]
    public bool ShowAlpha { get; set; }

    /// <summary>Disables the field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    protected override string DefaultTag => "input";

    /// <summary>The current color.</summary>
    public BitBhColor? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);

    protected override void OnInitialized() => _value = DefaultValue;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Id ?? FieldContext?.ControlId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "value", _text ?? CurrentValue?.ToHex(ShowAlpha ? true : null) ?? string.Empty);
        builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => { _text = e.Value?.ToString(); }));
        builder.SetUpdatesAttributeName("value");
        builder.AddAttribute(32, "onblur", On<FocusEventArgs>("onblur", CommitAsync));
        builder.AddAttribute(33, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.Key == "Enter" ? CommitAsync() : Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "text";
        attrs["autocomplete"] = "off";
        attrs["autocorrect"] = "off";
        attrs["spellcheck"] = "false";
        if (IsDisabled) attrs["disabled"] = true;
        if (FieldContext?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        if (CurrentValue is { } color) attrs["style"] = $"--bit-bh-color:{color.ToHex()}";
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private async Task CommitAsync()
    {
        if (_text is not { } text) return;
        _text = null;
        BitBhColor? next;
        if (string.IsNullOrWhiteSpace(text)) next = null;
        else if (BitBhColor.TryParse(text, out var parsed)) next = parsed;
        else
        {
            // Not a color: the input goes back to the color it had.
            StateHasChanged();
            return;
        }
        if (next == CurrentValue) { StateHasChanged(); return; }
        if (!IsProvided(nameof(Value))) _value = next;
        await ValueChanged.InvokeAsync(next);
        StateHasChanged();
    }
}
