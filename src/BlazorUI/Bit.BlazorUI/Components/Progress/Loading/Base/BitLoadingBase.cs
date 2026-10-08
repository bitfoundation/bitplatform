using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The original loading css came from https://loading.io/css/
/// </summary>
public abstract class BitLoadingBase : BitComponentBase
{
    private int _delayInEffect;
    private CancellationTokenSource? _delayCts;

    // The parameters of this class are taken out of the ParameterView below before it reaches
    // BitComponentBase, so the set that BitComponentBase tracks never sees them, and the loaders
    // themselves declare no parameters for the source generator to track. A cascade filling in what a
    // consumer left unset therefore needs this set of its own to tell the two apart, or it would overwrite
    // a Color or a Size that was written on the loader by hand.
    private readonly HashSet<string> _assignedLoadingParameters = [];



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

    /// <summary>
    /// Whether the component is still inside its <see cref="Delay"/> window, and therefore holds its content
    /// back, leaving the root an empty live region.
    /// </summary>
    internal bool IsDelayed;



    /// <summary>
    /// Gets or sets the cascading parameters for the loading components.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to every loading component
    /// under the same <see cref="BitParams"/> component - one <see cref="BitLoadingParams"/> reaches all eighteen
    /// of them, since they share this one API.
    /// </remarks>
    [CascadingParameter(Name = BitLoadingParams.ParamName)]
    public BitLoadingParams? CascadingParameters { get; set; }



    /// <summary>
    /// Gets or sets how insistently the live region of the loading component announces itself.
    /// <br />
    /// The default value is <strong>"polite"</strong> for the default "status" role, and none for any other.
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
    /// <remarks>
    /// The --bit-Loading-color CSS variable, where one is set, wins over it.
    /// </remarks>
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
    /// The whole drawing scales with it, and the label follows within the readable range of the type ramp. It
    /// only applies while <see cref="Size"/> is left unset. Zero and negative values are ignored. The
    /// --bit-Loading-size CSS variable, where one is set, wins over it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder] public int? CustomSize { get; set; }

    /// <summary>
    /// Gets or sets how long, in milliseconds, the loading component waits before it shows anything.
    /// <br />
    /// The default value is <strong>0</strong>, which renders it immediately.
    /// </summary>
    /// <remarks>
    /// Work that finishes in a few hundred milliseconds reads as instant, and a loader that flashes up and
    /// vanishes again inside that window is more distracting than no loader at all. A delay holds the content
    /// back for that long: if the work finishes first, the component is removed before the delay elapses and
    /// nothing was ever shown; if it does not, the loader appears as usual. Only the content is held back -
    /// the root stays in the document as an empty live region, so the announcement described on
    /// <see cref="Role"/> still lands reliably when the delay elapses.
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
    /// inside a sentence, a button, a table cell or a heading without pushing anything onto a new line. Every
    /// element of a loader is a span, so it is valid markup inside a paragraph or a button either way.
    /// <br />
    /// Unless <see cref="Size"/> or <see cref="CustomSize"/> says otherwise, it is drawn at the size of the
    /// surrounding text (1em) and its label takes the text size too, so it fits the line it sits in. A Size or a
    /// CustomSize handed down by a <see cref="BitParams"/> cascade is not applied to a loader that sets Inline itself.
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
    /// <br />
    /// The default value is <strong>Top</strong>, or <strong>End</strong> for an <see cref="Inline"/> loader, which keeps
    /// its label on the line it sits in.
    /// </summary>
    /// <remarks>
    /// The Start and End positions follow the direction of the writing and swap sides in a right-to-left
    /// layout, while Top and Bottom stay where they are.
    /// <br />
    /// Only Top, Bottom, Start and End are meaningful here; the physical pair and the two combined values
    /// leave the layout as it is with this unset.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPlacement? LabelPlacement { get; set; }

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
    /// "progressbar" makes it an indeterminate progress bar instead, which a screen reader lists as a control
    /// but does not announce when it appears. Everything inside a progressbar is presentational, so the root is
    /// named directly - with <see cref="BitComponentBase.AriaLabel"/>, then <see cref="Label"/>, then the
    /// fallback text; give one drawn with a <see cref="LabelTemplate"/> an AriaLabel that says what its template
    /// shows, or it is named by the fallback text alone. Pass "none" for a purely
    /// decorative loader whose surroundings already report the wait. A role passed straight through as a
    /// 'role' HTML attribute is honored while this parameter is left unset.
    /// </remarks>
    [Parameter] public string? Role { get; set; }

    /// <summary>
    /// The Size of the loading component: 40px, 64px or 88px, with the label on the matching step of the type ramp.
    /// </summary>
    /// <remarks>
    /// The --bit-Loading-size CSS variable, where one is set, wins over it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
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
    /// value. Zero and negative values are ignored. The --bit-Loading-speed CSS variable, where one is set,
    /// wins over it.
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
    /// negative values are ignored. The --bit-Loading-thickness CSS variable, where one is set, wins over it.
    /// </remarks>
    [Parameter, ResetStyleBuilder] public int? Thickness { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        _assignedLoadingParameters.Clear();

        // A cascade that no longer reaches the loader is absent from the ParameterView rather than passed as null.
        CascadingParameters = null;

        var parametersDictionary = (ParametersCache ??= parameters.ToDictionary() as Dictionary<string, object?>);

        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(CascadingParameters):
                    CascadingParameters = (BitLoadingParams?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(AriaLive):
                    _assignedLoadingParameters.Add(nameof(AriaLive));
                    AriaLive = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Classes):
                    _assignedLoadingParameters.Add(nameof(Classes));
                    var classes = (BitLoadingClassStyles?)parameter.Value;
                    if (Classes != classes) ClassBuilder.Reset();
                    Classes = classes;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Color):
                    _assignedLoadingParameters.Add(nameof(Color));
                    var color = (BitColor?)parameter.Value;
                    if (Color != color) StyleBuilder.Reset();
                    Color = color;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(CustomColor):
                    _assignedLoadingParameters.Add(nameof(CustomColor));
                    var customColor = (string?)parameter.Value;
                    if (CustomColor != customColor) StyleBuilder.Reset();
                    CustomColor = customColor;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(CustomSize):
                    _assignedLoadingParameters.Add(nameof(CustomSize));
                    var customSize = (int?)parameter.Value;
                    if (CustomSize != customSize)
                    {
                        ClassBuilder.Reset();
                        StyleBuilder.Reset();
                    }
                    CustomSize = customSize;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Delay):
                    _assignedLoadingParameters.Add(nameof(Delay));
                    Delay = (int)parameter.Value!;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Inline):
                    _assignedLoadingParameters.Add(nameof(Inline));
                    var inline = (bool)parameter.Value!;
                    if (Inline != inline) ClassBuilder.Reset();
                    Inline = inline;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Label):
                    _assignedLoadingParameters.Add(nameof(Label));
                    Label = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(LabelPlacement):
                    _assignedLoadingParameters.Add(nameof(LabelPlacement));
                    var labelPlacement = (BitPlacement?)parameter.Value;
                    if (LabelPlacement != labelPlacement) ClassBuilder.Reset();
                    LabelPlacement = labelPlacement;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(LabelTemplate):
                    _assignedLoadingParameters.Add(nameof(LabelTemplate));
                    LabelTemplate = (RenderFragment?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Paused):
                    _assignedLoadingParameters.Add(nameof(Paused));
                    var paused = (bool)parameter.Value!;
                    if (Paused != paused) ClassBuilder.Reset();
                    Paused = paused;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Role):
                    _assignedLoadingParameters.Add(nameof(Role));
                    Role = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Size):
                    _assignedLoadingParameters.Add(nameof(Size));
                    var size = (BitSize?)parameter.Value;
                    if (Size != size)
                    {
                        ClassBuilder.Reset();
                        StyleBuilder.Reset();
                    }
                    Size = size;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Speed):
                    _assignedLoadingParameters.Add(nameof(Speed));
                    var speed = (double?)parameter.Value;
                    if (Speed != speed) StyleBuilder.Reset();
                    Speed = speed;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Styles):
                    _assignedLoadingParameters.Add(nameof(Styles));
                    var styles = (BitLoadingClassStyles?)parameter.Value;
                    if (Styles != styles) StyleBuilder.Reset();
                    Styles = styles;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Thickness):
                    _assignedLoadingParameters.Add(nameof(Thickness));
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

    private protected override bool IsSetByMarkup(string name) => _assignedLoadingParameters.Contains(name) || base.IsSetByMarkup(name);

    // Read straight off the property rather than through reflection, as the generated code of a component does.
    private protected override IBitComponentParams? CascadedParams => CascadingParameters;

    /// <summary>
    /// Fills in a parameter from a <see cref="BitLoadingParams"/> cascade. BitComponentBase remembers the value it
    /// held before the cascade first wrote it, and puts it back once the cascade stops setting it.
    /// </summary>
    /// <remarks>
    /// The class and style builders are only reset when the value actually changes, so a loader under a cascade
    /// that re-renders with the same parameters rebuilds neither string. The accessors are taken as static
    /// lambdas, so that applying a cascade on every render allocates nothing.
    /// </remarks>
    internal void Cascade<T>(T value,
                             Func<BitLoadingBase, T> get,
                             Action<BitLoadingBase, T> set,
                             bool resetClass = false,
                             bool resetStyle = false)
    {
        if (EqualityComparer<T>.Default.Equals(get(this), value)) return;

        set(this, value);

        if (resetClass) ClassBuilder.Reset();
        if (resetStyle) StyleBuilder.Reset();
    }

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
    /// <br />
    /// The "polite" default only restates what a status region already is, for the assistive technology
    /// that reads the attribute rather than the role. Any other role is left to its own politeness: an
    /// "alert" is assertive and a "progressbar" no live region at all, and a polite written onto either
    /// would change what it is.
    /// </remarks>
    internal string? _AriaLive => _IsDecorative ? null : (AriaLive ?? PassedThrough("aria-live") ?? (_Role is "status" ? "polite" : null));

    /// <summary>The writing direction of the root element, resolved the same way as <see cref="_Role"/>.</summary>
    internal string? _Dir => Dir?.ToString().ToLower() ?? PassedThrough("dir");

    /// <summary>Whether the component shows a label of its own, which is then what is announced.</summary>
    internal bool _HasVisibleLabel => LabelTemplate is not null || Label is not null;

    /// <summary>
    /// The accessible name of the root element, which is only needed while the component carries a visible
    /// label: without one, the same text is rendered inside the live region as <see cref="_ScreenReaderText"/>
    /// instead, so that a screen reader is never handed the one text twice.
    /// </summary>
    /// <remarks>
    /// A progressbar is the exception. Its children are presentational, so neither the label nor the hidden
    /// text inside it is ever read, and the role requires a name of its own: the root is named with the
    /// AriaLabel, then the text of the Label, then the fallback text. A LabelTemplate has no text to hand
    /// over, so a progressbar drawn with one skips straight to the fallback text - unless a passed-through
    /// 'aria-labelledby' already names it, and a second name beside that one would only compete with it.
    /// </remarks>
    internal string? _AriaLabel => _IsProgressBar
                                       ? AriaLabel ?? PassedThrough("aria-label") ?? (LabelTemplate is null
                                                                                          ? Label ?? DefaultLoadingText
                                                                                          : (PassedThrough("aria-labelledby") is null ? DefaultLoadingText : null))
                                       : (_HasVisibleLabel ? AriaLabel : null) ?? PassedThrough("aria-label");

    /// <summary>Whether the root is a progressbar, which is named on the root rather than by its content.</summary>
    internal bool _IsProgressBar => _Role is "progressbar";

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
    internal string? _ScreenReaderText => (_HasVisibleLabel || _IsDecorative || _IsProgressBar || PassedThrough("aria-label") is not null)
                                          ? null
                                          : (AriaLabel ?? DefaultLoadingText);

    private string? PassedThrough(string attribute) => GetSplattedAttribute(attribute);



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitLoadingParams))]
    protected override void OnParametersSet()
    {
        // Applied before anything below reads the parameters, so that a Delay handed down by a cascade opens
        // its window exactly as one written on the loader does.
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();

        // Held back rather than hidden: content that is not in the document cannot flash up and vanish again
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
            IsDelayed = true;
            _delayCts = new CancellationTokenSource();
            _ = WaitOutDelayAsync(_delayCts.Token);
        }
        else
        {
            IsDelayed = false;
        }
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => "bit-ldn");

        ClassBuilder.Register(() => Inline ? "bit-ldn-inl" : string.Empty);

        ClassBuilder.Register(() => Paused ? "bit-ldn-pau" : string.Empty);

        // A custom size takes no class: the drawing is sized by the inline variable, and the label scales from it.
        // An unsized inline loader is drawn at the size of the text around it rather than the medium default.
        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ldn-sm",
            BitSize.Medium => "bit-ldn-md",
            BitSize.Large => "bit-ldn-lg",
            _ => CustomSize > 0 ? string.Empty : Inline ? "bit-ldn-em" : "bit-ldn-md"
        });

        ClassBuilder.Register(() => LabelPlacement switch
        {
            BitPlacement.Top => "bit-ldn-ltp",
            BitPlacement.Bottom => "bit-ldn-lbm",
            BitPlacement.Start => "bit-ldn-lst",
            BitPlacement.End => "bit-ldn-led",
            // An inline loader keeps its label on the line it sits in rather than stacking the two.
            _ => Inline ? "bit-ldn-led" : "bit-ldn-ltp"
        });

        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        // Each of these is written only when its parameter asks for something, and under a name of its own that
        // the stylesheet resolves behind the matching --bit-Loading-* variable: a declaration in the style
        // attribute would outrank the stylesheet, and so the public variable, however it was set.
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
                _ => CustomColor.HasValue() ? CustomColor : null
            };

            return color is null ? null : $"--bit-ldn-clr:{color}";
        });

        StyleBuilder.Register(() => Size is null && CustomSize > 0 ? $"--bit-ldn-sz:{CustomSize}px" : null);

        // Left unset rather than given the authored value, so that every stroke keeps reading its own
        // fallback - the one the drawing was measured at, which is not shared between the loaders.
        StyleBuilder.Register(() => Thickness > 0 ? $"--bit-ldn-stroke:{Thickness}px" : null);

        // A multiplier the stylesheet divides the theme's loop factor by, rather than a factor of its own, so
        // the calmer speed a reduced-motion environment asks for - and the full speed ForceAnimation puts back -
        // both survive it.
        StyleBuilder.Register(() => Speed > 0 ? $"--bit-ldn-spd:{Speed.Value.ToString(CultureInfo.InvariantCulture)}" : null);

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

        IsDelayed = false;

        await InvokeAsync(StateHasChanged);
    }
}
