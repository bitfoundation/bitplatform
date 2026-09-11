using System.Globalization;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// BitTextShimmer is an animated text shimmer in which a bright gradient band sweeps across the text,
/// ideal for AI thinking/loading states or progressive reveals.
/// The effect is implemented in pure CSS (no JavaScript interop), so it works in every Blazor render mode, including static server-side rendering.
/// </summary>
/// <remarks>
/// The text is painted with a gradient that is clipped to its glyphs: a resting <see cref="BaseColor"/> with a band of
/// the <see cref="GradientColor"/> in the middle of it, moved across the text once per <see cref="Duration"/> in the
/// reading direction of the text. The band is off the text at both ends of every sweep, so the text rests in its base
/// color before the first sweep, between two of them - for as long as <see cref="RepeatDelay"/> asks - and after the
/// last one of <see cref="Iterations"/>.
/// <br />
/// The characters are still in the document: a copy, a find-in-page and a screen reader all get the text whatever is
/// painted into it. Where the shimmer cannot be painted - a forced-colors palette, a printout, a browser without the
/// text clip - or where it is not wanted - a reduced motion preference, a disabled component - the text is drawn in a
/// flat color instead, so it is never left invisible. A selected glyph is given a fill of its own as well.
/// <br />
/// The band is painted within the box of the element only, so the part of a glyph that reaches outside it - an
/// accent or a descender under a line height tighter than the font - is not painted; give the element the line
/// height, or the padding, its glyphs need.
/// <br />
/// Anything that is not a parameter is splatted onto the rendered tag, and the attributes the component builds itself
/// are merged with the splatted ones rather than replacing them - which is how a "role" of "status" or an "aria-live"
/// reaches the element when the shimmer is the message a screen reader should announce.
/// </remarks>
public partial class BitTextShimmer : BitComponentBase
{
    private const string DefaultElement = "p";

    // The sweep the stylesheet plays when no Duration is given, before the theme's loop factor retunes it.
    private const int DefaultDuration = 2000;



    /// <summary>
    /// Sweeps the band back and forth across the text instead of always in the same direction.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Every other sweep is played backwards, so the band crosses the text in one direction and comes back across it
    /// in the other. <see cref="Reversed"/> decides which of the two directions comes first.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Alternate { get; set; }

    /// <summary>
    /// The tilt of the band in degrees, measured from upright. When null, the band is upright.
    /// </summary>
    /// <remarks>
    /// A positive angle leans the top of the band towards the end of the text in its reading direction - to the right
    /// in left-to-right text and to the left in right-to-left text, where the whole shimmer is mirrored - and a
    /// negative one towards its start. A tilted band is wider on the page than an upright one, and it stops reading as
    /// a sweep altogether as it approaches a quarter turn, so keep it within about 45 degrees either way. A value that
    /// is not a finite number is ignored.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public double? Angle { get; set; }

    /// <summary>
    /// The resting/dim color of the text. When null, a theme-aware default color is used.
    /// </summary>
    /// <remarks>
    /// Any CSS color is accepted. It is the color the text is read in for most of the time - before, between and after
    /// the sweeps, and whenever the shimmer is not animated at all - so it is the one to check for contrast.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? BaseColor { get; set; }

    /// <summary>
    /// The content to shimmer, which takes precedence over the Text parameter.
    /// </summary>
    /// <remarks>
    /// The length of the content cannot be measured, so the width of the band is scaled by <see cref="ContentLength"/>
    /// instead. A void element (such as "img" or "br") is defined to hold no content, so nothing is rendered into one
    /// where <see cref="Element"/> names it.
    /// <br />
    /// Every glyph of the content is painted by the shimmer, so a part of it that is painted on a layer of its own -
    /// one that is transformed, such as a spinning icon - is left out of the clip and its glyphs are not painted at
    /// all. Give such a part a fill of its own ("-webkit-text-fill-color: currentcolor"), which also keeps an emoji
    /// in its own colors rather than as a silhouette of the band.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The general color of the band that sweeps across the text.
    /// </summary>
    /// <remarks>
    /// The color is read from the theme, so it follows the preset and the color scheme of the page. An explicit
    /// <see cref="GradientColor"/> wins over it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The character count used to scale the shimmer band width when the content is supplied using ChildContent.
    /// <br />
    /// The default value is <strong>10</strong>.
    /// </summary>
    /// <remarks>
    /// It is also used when neither <see cref="ChildContent"/> nor <see cref="Text"/> is set. A negative value is
    /// treated as zero.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int ContentLength { get; set; } = 10;

    /// <summary>
    /// The delay before the first shimmer sweep starts in ms.
    /// </summary>
    /// <remarks>
    /// The text rests in its <see cref="BaseColor"/> until then, and the sweeps that follow the first one run back to
    /// back. Giving each of several shimmers a slightly later start than the one before it makes them read as one
    /// wave running down a list rather than as many blinking at once. A negative value is treated as zero.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Delay { get; set; }

    /// <summary>
    /// The animation duration of one full shimmer sweep in ms.
    /// </summary>
    /// <remarks>
    /// One sweep is the band crossing the text and the pause before it enters again, which take about half of it each.
    /// When null, a two-second sweep is used, which the looping motion factor of the theme may retune. A negative
    /// value is treated as zero, which leaves the text at rest.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Duration { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "p".
    /// </summary>
    /// <remarks>
    /// Use a heading tag for a shimmering heading, or a "span" for a shimmer inside a sentence, a button or any other
    /// place that only accepts phrasing content, where a "p" is invalid markup.
    /// <br />
    /// An empty or whitespace value falls back to the default tag, and so does a value that is not a name a tag can
    /// have: one that does not begin with an ASCII letter, or that carries anything but letters, digits and the "-",
    /// "_", "." and ":" that join them - a whitespace or a "&lt;" would end the tag and write markup of its own.
    /// </remarks>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// The bright highlight color that sweeps across the text. When null, a theme-aware default color is used.
    /// </summary>
    /// <remarks>
    /// Any CSS color is accepted, and it wins over <see cref="Color"/>.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? GradientColor { get; set; }

    /// <summary>
    /// The number of shimmer sweeps to play before the text comes to rest. When null, the shimmer sweeps forever.
    /// </summary>
    /// <remarks>
    /// A single sweep is a progressive reveal - an entrance for a heading or a freshly arrived answer - rather than
    /// the signal that something is still in progress. The text rests in its <see cref="BaseColor"/> once the sweeps
    /// are over. A value below one is ignored, and the shimmer sweeps forever.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? Iterations { get; set; }

    /// <summary>
    /// Holds the shimmer where it is instead of sweeping.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The band stops at whichever point of the sweep it had reached - possibly in the middle of the text - and carries
    /// on from there once this is turned off again, so the text never jumps. A shimmer paused before its first sweep
    /// is simply the text in its <see cref="BaseColor"/>. Use <see cref="Static"/> to take the band away altogether.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Paused { get; set; }

    /// <summary>
    /// Holds the shimmer where it is while the pointer is over it or the focus is inside it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A sweep that keeps running under the pointer makes the text harder to read for exactly the reader who is
    /// trying to read it, so this gives them a way to stop it. The sweep carries on from where it was once the pointer
    /// or the focus leaves.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool PauseOnHover { get; set; }

    /// <summary>
    /// An extra pause between two shimmer sweeps in ms.
    /// </summary>
    /// <remarks>
    /// Each sweep already ends with the band leaving the text and entering it again, which takes about half of the
    /// <see cref="Duration"/>; this adds to that rest without changing the speed of the band. A calm pause between the
    /// sweeps is what keeps a long-running "thinking" label from reading as a blinking one. Without a
    /// <see cref="Duration"/> the pause is retuned by the looping motion factor of the theme along with the sweep it
    /// follows. A negative value is treated as zero.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? RepeatDelay { get; set; }

    /// <summary>
    /// Sweeps the band against the reading direction.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The band follows the reading direction of the text by default: from left to right, and from right to left
    /// in right-to-left text - whether that comes from the <see cref="BitComponentBase.Dir"/> of the component or
    /// from a "dir" attribute on the page around it. This turns that around in both directions.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The shimmer band width multiplier. The effective spread of the band (px) is Spread times the character count,
    /// so longer text gets a proportionally wider shine.
    /// <br />
    /// The default value is <strong>2</strong>.
    /// </summary>
    /// <remarks>
    /// The effective width is the distance from the brightest point of the band to each of its edges. The character
    /// count is the length of <see cref="Text"/> in user-perceived characters, or <see cref="ContentLength"/> when
    /// the content is supplied using <see cref="ChildContent"/>. A negative value is treated as zero, which draws a
    /// hard edged band, and a value that is not a finite number is ignored. <see cref="SpreadLength"/> wins over it.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public double Spread { get; set; } = 2;

    /// <summary>
    /// An explicit CSS length for the spread of the band, which replaces the one computed from Spread and the character count.
    /// </summary>
    /// <remarks>
    /// The spread is the distance from the brightest point of the band to each of its edges. A font-relative length
    /// ("3em", "4ch") follows the size of the text without counting its characters, which is what content supplied
    /// through <see cref="ChildContent"/> needs, and what keeps a heading and a caption with the same text looking
    /// alike. Prefer an absolute or a font-relative length: a percentage is taken of the background the band is
    /// painted in, which is wider than the text.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? SpreadLength { get; set; }

    /// <summary>
    /// Renders the text at rest in its base color, without the shimmer.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Paused"/>, which holds the band wherever it happens to be, this takes the band away and
    /// leaves the plain text in its <see cref="BaseColor"/> - the finished state of a "thinking" label that stays on
    /// the page once the work is over. Turning it off again starts the sweeps from the beginning.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Static { get; set; }

    /// <summary>
    /// The text to display, that is also used to scale the shimmer band width based on its character count.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Text { get; set; }



    protected override string RootElementClass => "bit-tsh";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-tsh-pri",
            BitColor.Secondary => "bit-tsh-sec",
            BitColor.Tertiary => "bit-tsh-ter",
            BitColor.Info => "bit-tsh-inf",
            BitColor.Success => "bit-tsh-suc",
            BitColor.Warning => "bit-tsh-wrn",
            BitColor.SevereWarning => "bit-tsh-swr",
            BitColor.Error => "bit-tsh-err",
            BitColor.PrimaryBackground => "bit-tsh-pbg",
            BitColor.SecondaryBackground => "bit-tsh-sbg",
            BitColor.TertiaryBackground => "bit-tsh-tbg",
            BitColor.PrimaryForeground => "bit-tsh-pfg",
            BitColor.SecondaryForeground => "bit-tsh-sfg",
            BitColor.TertiaryForeground => "bit-tsh-tfg",
            BitColor.PrimaryBorder => "bit-tsh-pbr",
            BitColor.SecondaryBorder => "bit-tsh-sbr",
            BitColor.TertiaryBorder => "bit-tsh-tbr",
            _ => string.Empty
        });

        // The band follows the reading direction of its own accord - the stylesheet reads it off the element - so
        // this class only says whether it runs with that direction or against it.
        ClassBuilder.Register(() => Reversed ? "bit-tsh-rev" : string.Empty)
                    .Register(() => Alternate ? "bit-tsh-alt" : string.Empty)
                    .Register(() => Paused ? "bit-tsh-pau" : string.Empty)
                    .Register(() => PauseOnHover ? "bit-tsh-poh" : string.Empty)
                    .Register(() => Static ? "bit-tsh-sta" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() =>
        {
            if (SpreadLength.HasValue()) return $"--bit-tsh-spread:{SpreadLength!.Trim()}";

            var length = ChildContent is null && Text is not null ? new StringInfo(Text).LengthInTextElements : ContentLength;
            // A spread that is not a finite number would write a length the browser refuses, and a gradient with an
            // invalid stop is no gradient at all - so it falls back to the default multiplier instead.
            var spread = Math.Max(0, Math.Max(0, length) * (double.IsFinite(Spread) ? Spread : 2));
            return $"--bit-tsh-spread:{Format(spread)}px";
        });

        StyleBuilder.Register(() => Duration.HasValue ? $"--bit-tsh-duration:{Ms(Duration.Value)}" : string.Empty);
        StyleBuilder.Register(() => Delay.HasValue ? $"--bit-tsh-delay:{Ms(Delay.Value)}" : string.Empty);

        // The pause is written as the length of the whole cycle over the length of the sweep, which the stylesheet
        // lengthens the animation and the distance the band travels by alike, so the band crosses the text at the
        // same speed and simply spends longer outside it. The default duration is the one the ratio is taken of when
        // none is given, and since the stylesheet multiplies the duration by the ratio rather than adding the pause
        // to it, the loop factor of the theme retunes the pause along with the sweep it belongs to.
        StyleBuilder.Register(() =>
        {
            if (RepeatDelay is not > 0) return string.Empty;

            var duration = Math.Max(0, Duration ?? DefaultDuration);
            if (duration == 0) return string.Empty;

            var cycle = 1 + (double)RepeatDelay.Value / duration;
            return $"--bit-tsh-cycle:{cycle.ToString("0.######", CultureInfo.InvariantCulture)}";
        });

        StyleBuilder.Register(() => Iterations >= 1 ? $"--bit-tsh-iterations:{Iterations.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty);
        StyleBuilder.Register(() => Angle.HasValue && double.IsFinite(Angle.Value) ? $"--bit-tsh-angle:{Format(Angle.Value)}deg" : string.Empty);
        StyleBuilder.Register(() => BaseColor.HasValue() ? $"--bit-tsh-base-clr:{BaseColor}" : string.Empty);
        StyleBuilder.Register(() => GradientColor.HasValue() ? $"--bit-tsh-gradient-clr:{GradientColor}" : string.Empty);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var element = Element?.Trim();
        if (element.HasNoValue() || IsValidElement(element!) is false)
        {
            element = DefaultElement;
        }

        builder.OpenElement(0, element!);
        // The splatted attributes come first so everything the component builds itself is written over them. The
        // values the component would otherwise write as null are resolved against them below, since a null written
        // over a splatted attribute does not leave that attribute alone - it removes it.
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", Id.HasValue() ? Id : (GetSplattedAttribute("id") ?? _Id));
        builder.AddAttribute(3, "style", JoinStyles(GetSplattedAttribute("style"), StyleBuilder.Value));
        builder.AddAttribute(4, "class", JoinClasses(ClassBuilder.Value, GetSplattedAttribute("class")));
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLowerInvariant() ?? GetSplattedAttribute("dir"));
        builder.AddAttribute(6, "aria-label", AriaLabel ?? GetSplattedAttribute("aria-label"));
        builder.AddAttribute(7, "tabindex", TabIndex ?? GetSplattedAttribute("tabindex"));
        builder.AddElementReferenceCapture(8, v => RootElement = v);
        // A void element is defined to hold no content: the static renderer writes it self-closed, so anything put
        // inside it would either be dropped or end up as a sibling of the element in the rendered markup.
        if (IsVoidElement(element!) is false)
        {
            if (ChildContent is not null)
            {
                builder.AddContent(9, ChildContent);
            }
            else
            {
                builder.AddContent(10, Text);
            }
        }
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }



    private static string Ms(int value) => $"{Math.Max(0, value).ToString(CultureInfo.InvariantCulture)}ms";

    // Written without an exponent and in the invariant culture, since a "1,5" or a "1E-05" is not a number a style
    // attribute reads the way it was meant.
    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
