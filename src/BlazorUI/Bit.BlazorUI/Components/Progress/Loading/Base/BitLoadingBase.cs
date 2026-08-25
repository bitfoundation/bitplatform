using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The original loading css came from https://loading.io/css/
/// </summary>
public abstract class BitLoadingBase : BitComponentBase
{
    /// <summary>
    /// The text a screen reader announces for a loading component that shows no visible label.
    /// </summary>
    /// <remarks>
    /// A spinning shape carries no text of its own, so a loader that shows nothing but the animation would
    /// be silent to assistive technology. It is announced through the live region on the root element - see
    /// <see cref="Role"/> - and is replaced by the visible label as soon as <see cref="Label"/> or
    /// <see cref="LabelTemplate"/> gives the component one, so the same wait is never announced twice.
    /// </remarks>
    internal const string DefaultLoadingText = "Loading";



    private int _delayInEffect;
    private CancellationTokenSource? _delayCts;

    /// <summary>
    /// Whether the component is still inside its <see cref="Delay"/> window, and therefore renders nothing at all.
    /// </summary>
    internal bool _IsDelayed;



    /// <summary>
    /// Gets or sets how insistently the live region of the loading component announces itself.
    /// <br />
    /// The default value is <strong>"polite"</strong>.
    /// </summary>
    /// <remarks>
    /// This is rendered as the 'aria-live' attribute of the root element. "polite" waits for the screen reader
    /// to finish what it is saying, which is what a loading indicator wants; "assertive" interrupts it, and is
    /// only appropriate where the wait itself is the thing the user has to hear about right now.
    /// <br />
    /// Set it to "off" to silence the region without giving up the <see cref="Role"/>. A politeness passed
    /// straight through as an 'aria-live' HTML attribute is honored while this parameter is left unset.
    /// </remarks>
    [Parameter] public string? AriaLive { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the loading component.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitLoadingClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the loading component.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom css color of the loading component.
    /// </summary>
    /// <remarks>
    /// Any valid CSS color works here, <c>currentColor</c> included, which is what lets a loader take the color
    /// of the text around it. It only applies while <see cref="Color"/> is left unset - a theme role always
    /// wins over a literal color.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public string? CustomColor { get; set; }

    /// <summary>
    /// The custom size of the loading component in px.
    /// </summary>
    /// <remarks>
    /// The whole drawing scales with it, the label included. It only applies while <see cref="Size"/> is left unset.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public int? CustomSize { get; set; }

    /// <summary>
    /// Gets or sets how long, in milliseconds, the loading component waits before it renders anything at all.
    /// <br />
    /// The default value is <strong>0</strong>, which renders it immediately.
    /// </summary>
    /// <remarks>
    /// Work that finishes in a few hundred milliseconds reads as instant, and a loader that flashes up and
    /// vanishes again inside that window is more distracting than no loader at all. A delay holds the component
    /// back for that long: if the work finishes first, the component is removed before the delay elapses and
    /// nothing was ever shown; if it does not, the loader appears as usual.
    /// <br />
    /// Changing the value opens the window again from the new length, and setting it back to zero lets the
    /// component through at once, so a loader kept in the document across several waits can be held back for
    /// each of them without being re-created.
    /// </remarks>
    [Parameter] public int Delay { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the loading component flows with the text around it instead of
    /// sitting on a line of its own.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// An inline loader is laid out as an inline box aligned to the middle of the current line, so it can sit
    /// inside a sentence, a button, a table cell or a heading without pushing anything onto a new line.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool Inline { get; set; }

    /// <summary>
    /// The text content of the label of the loading component.
    /// </summary>
    /// <remarks>
    /// A label is what turns a spinning shape into a status message, so prefer a short, specific phrase
    /// ("Saving changes...") over a bare "Loading". It is also what assistive technology announces, which is
    /// why it replaces the fallback text described on <see cref="Role"/>.
    /// </remarks>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The position of the label of the loading component.
    /// </summary>
    /// <remarks>
    /// The Start and End positions follow the direction of the writing and swap sides in a right-to-left
    /// layout, while Top and Bottom stay where they are.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// The custom content of the label of the loading component.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the animation of the loading component is held where it is
    /// instead of running.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The drawing keeps its shape and its place in the layout, so pausing and resuming never makes the
    /// surface around it jump; it is the movement alone that stops, at whichever frame it had reached.
    /// <br />
    /// A paused loader still says that work is under way, so reach for this only where that remains true and
    /// the movement is what has to stop - a wait that has stalled behind a retry, a page whose animations are
    /// held while it is captured - and remove the component outright once the work is over.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool Paused { get; set; }

    /// <summary>
    /// Gets or sets the ARIA role of the root element of the loading component.
    /// <br />
    /// The default value is <strong>"status"</strong>.
    /// </summary>
    /// <remarks>
    /// A "status" role turns the root into a live region, which is what makes a screen reader announce the
    /// label - or the fallback text, see <see cref="DefaultLoadingText"/> - when the loader appears.
    /// <br />
    /// Prefer "progressbar" only where the wait is measured and reported through the aria-value attributes,
    /// and be aware that everything inside a progressbar is presentational, so a visible label is no longer
    /// announced from within it. Pass "none" for a purely decorative loader whose surroundings already report
    /// the wait. A role passed straight through as a 'role' HTML attribute is honored while this parameter is
    /// left unset.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// The Size of the loading component.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Gets or sets how fast the animation of the loading component runs, as a multiplier of its normal speed.
    /// <br />
    /// The default value is <strong>null</strong>, which runs it at its normal speed.
    /// </summary>
    /// <remarks>
    /// 2 runs the animation twice as fast, 0.5 half as fast. Every duration and every delay of the loader is
    /// scaled together, so the phase offsets that stagger its parts against each other survive.
    /// <br />
    /// The multiplier composes with the reduced-motion preference rather than overriding it: a loader in a
    /// reduced-motion environment still turns at the calmer speed the theme picks for it, only scaled by this
    /// value. Zero and negative values are ignored.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public double? Speed { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the loading component.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitLoadingClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the thickness, in px, of the stroke the loading component is drawn with.
    /// <br />
    /// The default value is <strong>null</strong>, which keeps the thickness the drawing was authored with.
    /// </summary>
    /// <remarks>
    /// Only the loaders drawn with a stroke read it - <see cref="BitRingLoading"/>,
    /// <see cref="BitDualRingLoading"/>, <see cref="BitRippleLoading"/>, <see cref="BitXboxLoading"/> and
    /// <see cref="BitSpinnerLoading"/> - and the rest, whose shapes are filled rather than stroked, are left
    /// as they are. Every one of them draws the stroke inside its own outline, so a thicker one never grows
    /// the footprint of the component past the size it was given.
    /// <br />
    /// It is a literal number of pixels rather than a ratio, so it does not scale with <see cref="Size"/> or
    /// <see cref="CustomSize"/> - a hairline stays a hairline whatever the loader is sized at. Zero and
    /// negative values are ignored.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public int? Thickness { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        var parametersDictionary = (ParametersCache ??= parameters.ToDictionary() as Dictionary<string, object?>);

        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(AriaLive):
                    AriaLive = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Classes):
                    var classes = (BitLoadingClassStyles?)parameter.Value;
                    if (Classes != classes) ClassBuilder.Reset();
                    Classes = classes;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Color):
                    var color = (BitColor?)parameter.Value;
                    if (Color != color) StyleBuilder.Reset();
                    Color = color;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(CustomColor):
                    var customColor = (string?)parameter.Value;
                    if (CustomColor != customColor) StyleBuilder.Reset();
                    CustomColor = customColor;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(CustomSize):
                    var customSize = (int?)parameter.Value;
                    if (CustomSize != customSize) StyleBuilder.Reset();
                    CustomSize = customSize;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Delay):
                    Delay = (int)parameter.Value!;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Inline):
                    var inline = (bool)parameter.Value!;
                    if (Inline != inline) ClassBuilder.Reset();
                    Inline = inline;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Label):
                    var label = (string?)parameter.Value;
                    Label = label;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(LabelPosition):
                    var labelPosition = (BitLabelPosition?)parameter.Value;
                    if (LabelPosition != labelPosition) ClassBuilder.Reset();
                    LabelPosition = labelPosition;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(LabelTemplate):
                    var labelTemplate = (RenderFragment?)parameter.Value;
                    LabelTemplate = labelTemplate;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Paused):
                    var paused = (bool)parameter.Value!;
                    if (Paused != paused) ClassBuilder.Reset();
                    Paused = paused;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Role):
                    Role = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Size):
                    var size = (BitSize?)parameter.Value;
                    if (Size != size) StyleBuilder.Reset();
                    Size = size;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Speed):
                    var speed = (double?)parameter.Value;
                    if (Speed != speed) StyleBuilder.Reset();
                    Speed = speed;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Styles):
                    var styles = (BitLoadingClassStyles?)parameter.Value;
                    if (Styles != styles) StyleBuilder.Reset();
                    Styles = styles;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Thickness):
                    var thickness = (int?)parameter.Value;
                    if (Thickness != thickness) StyleBuilder.Reset();
                    Thickness = thickness;
                    parametersDictionary.Remove(parameter.Key);
                    break;
            }
        }

        // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
        return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary!));
    }



    internal new ElementClassBuilder ClassBuilder => base.ClassBuilder;

    internal new ElementStyleBuilder StyleBuilder => base.StyleBuilder;

    /// <summary>
    /// The role the root element ends up with: the parameter where it was given one, then a plain 'role'
    /// HTML attribute passed through the splat, and the "status" default when neither was supplied.
    /// </summary>
    /// <remarks>
    /// The passed-through value has to be resolved here rather than left to the splat: an attribute written
    /// after '@attributes' wins over it, and one that renders as null removes it from the element outright.
    /// </remarks>
    internal string? _Role => Role ?? PassedThrough("role") ?? "status";

    /// <summary>
    /// The politeness of the live region, resolved the same way as <see cref="_Role"/> - except that a
    /// decorative loader is given none at all, since aria-live makes a live region of an element whatever
    /// its role, and the whole point of the decorative case is that it announces nothing.
    /// </summary>
    /// <remarks>
    /// The decorative case wins over a politeness that was asked for explicitly, as a parameter or as a
    /// passed-through attribute: the two contradict each other, and the role is the one that says what the
    /// loader is for. It is the same call <see cref="_ScreenReaderText"/> makes about the fallback text.
    /// </remarks>
    internal string? _AriaLive => _IsDecorative ? null : (AriaLive ?? PassedThrough("aria-live") ?? "polite");

    /// <summary>The writing direction of the root element, resolved the same way as <see cref="_Role"/>.</summary>
    internal string? _Dir => Dir?.ToString().ToLower() ?? PassedThrough("dir");

    /// <summary>Whether the component shows a label of its own, which is then what is announced.</summary>
    internal bool _HasVisibleLabel => LabelTemplate is not null || Label is not null;

    /// <summary>
    /// The accessible name of the root element, which is only needed while the component carries a visible
    /// label: without one, the same text is rendered inside the live region as <see cref="_ScreenReaderText"/>
    /// instead, so that a screen reader is never handed the one text twice.
    /// </summary>
    internal string? _AriaLabel => (_HasVisibleLabel ? AriaLabel : null) ?? PassedThrough("aria-label");

    /// <summary>
    /// Whether the loader was declared purely decorative, and so announces nothing of its own: the wait it
    /// draws is already reported by whatever surrounds it.
    /// </summary>
    internal bool _IsDecorative => _Role is "none" or "presentation";

    /// <summary>The text a labelless loader announces - see <see cref="DefaultLoadingText"/>.</summary>
    /// <remarks>
    /// It stands down for a passed-through 'aria-label' as well as for a visible one: that attribute stays on
    /// the root as the accessible name of the live region and is what a screen reader reads there, so the
    /// hidden text underneath it would never be reached anyway.
    /// </remarks>
    internal string? _ScreenReaderText => (_HasVisibleLabel || _IsDecorative || PassedThrough("aria-label") is not null)
                                          ? null
                                          : (AriaLabel ?? DefaultLoadingText);

    private string? PassedThrough(string attribute)
    {
        return HtmlAttributes.TryGetValue(attribute, out var value) ? value?.ToString() : null;
    }



    /// <summary>
    /// The size, in pixels, the drawing of this loader was authored at.
    /// </summary>
    /// <remarks>
    /// Every offset inside a loader is a literal pixel value taken from the original artwork, and
    /// <see cref="Convert"/> rescales those values against this so that the whole drawing lands inside the
    /// box the current size asks for. A derived component only overrides it when its artwork was drawn at
    /// something other than the 80px the family shares.
    /// </remarks>
    protected virtual int OriginalSize => 80;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Held back rather than hidden: a loader that is not in the document cannot flash up and vanish again
        // for work that turned out to be quick. See Delay.
        if (Delay == _delayInEffect) return;

        _delayInEffect = Delay;

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = null;

        // A window that is opened again starts over from the new length, and one that is taken away lets the
        // component through at once rather than leaving it stuck behind a delay it no longer has.
        if (Delay > 0)
        {
            _IsDelayed = true;
            _delayCts = new CancellationTokenSource();
            _ = WaitOutDelayAsync(_delayCts.Token);
        }
        else
        {
            _IsDelayed = false;
        }
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => "bit-ldn");

        ClassBuilder.Register(() => Inline ? "bit-ldn-inl" : string.Empty);

        ClassBuilder.Register(() => Paused ? "bit-ldn-pau" : string.Empty);

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => "bit-ldn-ltp",
            BitLabelPosition.Bottom => "bit-ldn-lbm",
            BitLabelPosition.Start => "bit-ldn-lst",
            BitLabelPosition.End => "bit-ldn-led",
            _ => "bit-ldn-ltp"
        });

        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() =>
        {
            var color = Color switch
            {
                BitColor.Primary => "var(--bit-clr-pri)",
                BitColor.Secondary => "var(--bit-clr-sec)",
                BitColor.Tertiary => "var(--bit-clr-ter)",
                BitColor.Info => "var(--bit-clr-inf)",
                BitColor.Success => "var(--bit-clr-suc)",
                BitColor.Warning => "var(--bit-clr-wrn)",
                BitColor.SevereWarning => "var(--bit-clr-swr)",
                BitColor.Error => "var(--bit-clr-err)",
                BitColor.PrimaryBackground => "var(--bit-clr-bg-pri)",
                BitColor.SecondaryBackground => "var(--bit-clr-bg-sec)",
                BitColor.TertiaryBackground => "var(--bit-clr-bg-ter)",
                BitColor.PrimaryForeground => "var(--bit-clr-fg-pri)",
                BitColor.SecondaryForeground => "var(--bit-clr-fg-sec)",
                BitColor.TertiaryForeground => "var(--bit-clr-fg-ter)",
                BitColor.PrimaryBorder => "var(--bit-clr-brd-pri)",
                BitColor.SecondaryBorder => "var(--bit-clr-brd-sec)",
                BitColor.TertiaryBorder => "var(--bit-clr-brd-ter)",
                // Color is nullable, so this also covers the unset case, where CustomColor applies.
                _ => CustomColor ?? "var(--bit-clr-pri)"
            };

            return $"--bit-ldn-color: {color}";
        });

        StyleBuilder.Register(() => $"--bit-ldn-size:{GetSize()}px");
        StyleBuilder.Register(() => $"--bit-ldn-font-size:{Format(GetFontSize())}px");

        // Left unset rather than given the authored value, so that every stroke keeps reading its own
        // fallback - the one the drawing was measured at, which is neither shared between the loaders nor
        // the same at every size.
        StyleBuilder.Register(() => Thickness > 0 ? $"--bit-ldn-stroke:{Thickness}px" : null);

        // Scales the loop factor the stylesheet already carries rather than replacing it, so the calmer speed
        // a reduced-motion environment asks for - and the full speed ForceAnimation puts back - both survive
        // the multiplier instead of being overwritten by it.
        StyleBuilder.Register(() => Speed > 0
                                    ? $"--bit-ldn-mot-factor:calc(var(--bit-mot-loop-factor, 1) / {Format(Speed.Value)})"
                                    : null);

        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _delayCts?.Cancel();
            _delayCts?.Dispose();
            _delayCts = null;
        }

        await base.DisposeAsync(disposing);
    }

    /// <summary>
    /// Rescales one of the drawing's authored offsets to the size the component is currently rendered at,
    /// and formats it for a CSS custom property.
    /// </summary>
    /// <param name="value">The offset as it was measured against <see cref="OriginalSize"/>.</param>
    protected string Convert(double value)
    {
        // Rounded before it is written out: the ratio is rarely exact, and an unrounded double turns a 6.4px
        // offset into 6.400000000000001px in the style attribute.
        return Format(Math.Round(value * GetSize() / OriginalSize, 4));
    }



    private async Task WaitOutDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(Delay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (IsDisposed || token.IsCancellationRequested) return;

        _IsDelayed = false;

        await InvokeAsync(StateHasChanged);
    }

    private static string Format(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private int GetSize()
    {
        return Size switch
        {
            BitSize.Small => 40,
            BitSize.Medium => 64,
            BitSize.Large => 88,
            _ => CustomSize ?? 64
        };
    }

    private double GetFontSize()
    {
        return Size switch
        {
            BitSize.Small => 10,
            BitSize.Medium => 14,
            BitSize.Large => 18,
            // The label scales with a custom size against the same 64px/14px anchor the medium size uses.
            // This used to be an integer division by 64, which floored every custom size below 64px to a 0px -
            // invisible - label, and every size between 64px and 128px back to the medium 14px.
            _ => CustomSize.HasValue ? Math.Round(CustomSize.Value * 14d / 64, 2) : 14
        };
    }
}
