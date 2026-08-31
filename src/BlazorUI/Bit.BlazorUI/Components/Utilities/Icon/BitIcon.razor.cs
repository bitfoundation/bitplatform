using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A component for displaying icons that enhance visual communication and user experience.
/// Supports both built-in Fluent UI icons and external icon libraries.
/// </summary>
public partial class BitIcon : BitComponentBase
{
    private BitIconInfo? _icon;
    private bool _spacePressed;
    private bool _preventKeysRegistered;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the icon component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple icon components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitIconParams.ParamName)]
    public BitIconParams? CascadingParameters { get; set; }



    /// <summary>
    /// Specifies a looping animation to play on the icon.
    /// </summary>
    /// <remarks>
    /// An animation is drawn with a transform of its own, and <see cref="Rotate"/>, <see cref="RotateAngle"/>
    /// and <see cref="Flip"/> are drawn with the individual rotate and scale properties beside it, so the
    /// two compose: a mirrored arrow still spins, and it spins around the angle it was turned to. Only a
    /// browser too old for those properties (before 2022) falls back to a single transform, where the
    /// animation wins over the static turn.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitIconAnimation? Animation { get; set; }

    /// <summary>
    /// Overrides how long one cycle of the <see cref="Animation"/> takes, as any CSS time.
    /// </summary>
    /// <remarks>
    /// Each animation comes with a duration of its own - the turning ones with the duration every other
    /// loader in the library turns at - and this replaces it for the icon it is given to, which is what
    /// lets a spinner tick slowly in the corner of a page or a bell shake twice as fast. The reduced
    /// motion factor still multiplies it, so an animation asked to run fast still slows down for a
    /// reader who asked for less motion.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? AnimationDuration { get; set; }

    /// <summary>
    /// Waits this long before the <see cref="Animation"/> starts, as any CSS time.
    /// </summary>
    /// <remarks>
    /// One animation waiting is not worth much; a row of them each waiting a little longer than the one
    /// before it is the loader written as three dots rather than as a spinner. The wait is not slowed
    /// down under reduced motion the way the cycle is - a delay is not motion.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? AnimationDelay { get; set; }

    /// <summary>
    /// The content rendered inside the icon element, for an icon set that is neither a font nor a
    /// class - an inline svg, an image, a ligature of your own.
    /// </summary>
    /// <remarks>
    /// The color, the size and the variant still apply around it, so an inline svg drawn with
    /// <c>fill="currentColor"</c> follows the icon's own color the way a glyph would.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Draws the icon in a circle rather than in the rounded box of the design system.
    /// </summary>
    /// <remarks>
    /// The box is what the <see cref="BitVariant.Fill"/> and <see cref="BitVariant.Outline"/> variants
    /// paint, so this is what turns one of them into the round badge a status dot or an avatar
    /// placeholder is drawn as. The box is squared off at the same time, so a narrow glyph and a wide
    /// one are drawn in circles of the same size.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Circular { get; set; }

    /// <summary>
    /// Specifies the color theme of the icon.
    /// Default value is <see cref="BitColor.Primary"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Renders the icon in a box of a fixed width so that a column of icons of different widths lines up.
    /// </summary>
    /// <remarks>
    /// The glyphs of an icon font are not all the same width, which is what makes the labels of a menu
    /// or a nav sit at a different place on every row. This gives every icon the same box and centers
    /// the glyph inside it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FixedWidth { get; set; }

    /// <summary>
    /// Mirrors the icon on the horizontal axis, the vertical axis, or both.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconFlip? Flip { get; set; }

    /// <summary>
    /// Mirrors the icon horizontally when it is rendered in a right-to-left direction.
    /// </summary>
    /// <remarks>
    /// A glyph that points somewhere - an arrow, a chevron, a reply - points the other way in a
    /// right-to-left layout, while a clock, a checkmark or a media rewind button does not. Only the
    /// first kind should be given this. The direction is read off the rendered document, so it follows
    /// an ancestor's <c>dir</c> as well as this component's own <see cref="BitComponentBase.Dir"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FlipRtl { get; set; }

    /// <summary>
    /// Specifies the font size of the icon, as any CSS length or the <c>inherit</c> keyword.
    /// </summary>
    /// <remarks>
    /// This overrides <see cref="Size"/>, which is what an icon of one of the three sizes of the design
    /// system takes. Use it for the sizes in between, and for an icon that has to match whatever text
    /// it sits in (<c>FontSize="1em"</c> or <c>FontSize="inherit"</c>).
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? FontSize { get; set; }

    /// <summary>
    /// Specifies the icon configuration for rendering icons from external icon libraries using custom CSS classes.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property for external icon libraries such as FontAwesome, Bootstrap Icons, Material
    /// Icons or Material Symbols.
    /// For built-in Fluent UI icons, use the <see cref="IconName"/> property instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Specifies the name of the icon from the built-in Fluent UI icon library.
    /// This property is ignored when <see cref="Icon"/> is set.
    /// </summary>
    /// <remarks>
    /// The name is read as a name in another set when an <see cref="IconResolver"/> is given, here or
    /// through <see cref="BitIconParams"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? IconName { get; set; }

    /// <summary>
    /// Names the icon set that <see cref="IconName"/> is a name in.
    /// </summary>
    /// <remarks>
    /// A name on its own is a name in the built-in Fabric MDL2 set. An app that draws its icons from
    /// another set would otherwise have to write out a <see cref="BitIconInfo"/> at every single icon;
    /// this takes the name and answers with the icon it stands for - <c>name => BitIconInfo.Fa(name)</c>,
    /// <c>BitIconInfo.Ms</c>, a lookup table of your own - and cascades through
    /// <see cref="BitIconParams"/> to every icon of a subtree at once. An <see cref="Icon"/> that names
    /// a glyph still wins over it, and a resolver that answers with nothing leaves the name to the
    /// built-in set, so one resolver can cover the names it knows and fall through for the rest.
    /// <br />
    /// It is read by this component alone: the icon names the other components of the library take
    /// still mean what they always did, and a glyph from another set is given to those as an
    /// <c>Icon</c>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public Func<string, BitIconInfo?>? IconResolver { get; set; }

    /// <summary>
    /// Drops the icon slightly below the baseline so that it sits centered on the line of text it is
    /// written in.
    /// </summary>
    /// <remarks>
    /// A glyph of an icon font sits on the baseline the way a letter does and needs nothing; an inline
    /// svg or an image given through <see cref="ChildContent"/> is aligned by its bottom edge instead
    /// and rides high next to the text. This is the quarter of an em that puts it back.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Inline { get; set; }

    /// <summary>
    /// The callback for when the icon is clicked.
    /// </summary>
    /// <remarks>
    /// An icon with a click handler becomes a control: it takes the tab order, answers Enter and Space
    /// as well as the pointer, and is announced as a button. It is still an icon and not a
    /// <see cref="BitButton"/> - there is no pressed state, no busy state and no label of its own - so
    /// give it an <see cref="BitComponentBase.AriaLabel"/> or a <see cref="Title"/> saying what it
    /// does, and reach for a button whenever the action deserves one. Given neither, it is announced by
    /// the name of the glyph it draws rather than as an unnamed button - a fallback, not a name worth
    /// shipping.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Turns the icon by a quarter, a half, or three quarters of a turn.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconRotate? Rotate { get; set; }

    /// <summary>
    /// Turns the icon by an angle of your own, in degrees.
    /// </summary>
    /// <remarks>
    /// This is <see cref="Rotate"/> for everything that is not a quarter turn - the angle of a gauge
    /// needle, a chevron opened halfway - and it replaces <see cref="Rotate"/> when both are given. A
    /// negative angle turns counter-clockwise. Like the quarter turns, it composes with
    /// <see cref="Flip"/> and <see cref="FlipRtl"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public int? RotateAngle { get; set; }

    /// <summary>
    /// Specifies the size of the icon.
    /// Default value is <see cref="BitSize.Medium"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// The text shown in the native tooltip when the pointer rests on the icon.
    /// </summary>
    /// <remarks>
    /// A title also names the icon for assistive technology, so an icon that carries one is announced
    /// rather than skipped. Prefer <see cref="BitComponentBase.AriaLabel"/> when the name is meant to
    /// be read but not shown, and a <see cref="BitTooltip"/> when the text is meant to be seen - a
    /// native tooltip never appears for a keyboard or a touch user.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Specifies the visual styling variant of the icon.
    /// Default value is <see cref="BitVariant.Text"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-ico";

    // The icon the markup draws and the icon the class builder names have to be the same one, so it is
    // resolved once per set of parameters rather than by each of them: resolving it twice a render
    // would build a BitIconInfo twice for a value that did not change, and call an app's resolver
    // twice for a name it already answered.
    private void ResolveIcon()
    {
        if (Icon is not null && Icon.IsEmpty is false)
        {
            _icon = Icon;

            return;
        }

        if (IconName.HasNoValue())
        {
            _icon = Icon;

            return;
        }

        // A resolver that answers with nothing - no set of its own for this name, or a name it does not
        // know - leaves the name where it started, in the built-in set.
        var resolved = IconResolver?.Invoke(IconName!);

        _icon = resolved is not null && resolved.IsEmpty is false ? resolved : BitIconInfo.Bit(IconName!);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-ico-pri",
            BitColor.Secondary => "bit-ico-sec",
            BitColor.Tertiary => "bit-ico-ter",
            BitColor.Info => "bit-ico-inf",
            BitColor.Success => "bit-ico-suc",
            BitColor.Warning => "bit-ico-wrn",
            BitColor.SevereWarning => "bit-ico-swr",
            BitColor.Error => "bit-ico-err",
            BitColor.PrimaryBackground => "bit-ico-pbg",
            BitColor.SecondaryBackground => "bit-ico-sbg",
            BitColor.TertiaryBackground => "bit-ico-tbg",
            BitColor.PrimaryForeground => "bit-ico-pfg",
            BitColor.SecondaryForeground => "bit-ico-sfg",
            BitColor.TertiaryForeground => "bit-ico-tfg",
            BitColor.PrimaryBorder => "bit-ico-pbr",
            BitColor.SecondaryBorder => "bit-ico-sbr",
            BitColor.TertiaryBorder => "bit-ico-tbr",
            _ => "bit-ico-pri"
        });

        ClassBuilder.Register(() => _icon?.GetCssClasses());

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ico-sm",
            BitSize.Medium => "bit-ico-md",
            BitSize.Large => "bit-ico-lg",
            _ => "bit-ico-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-ico-fil",
            BitVariant.Outline => "bit-ico-out",
            BitVariant.Text => "bit-ico-txt",
            _ => "bit-ico-txt"
        });

        ClassBuilder.Register(() => FixedWidth ? "bit-ico-fxw" : string.Empty);

        ClassBuilder.Register(() => Circular ? "bit-ico-cir" : string.Empty);

        ClassBuilder.Register(() => Inline ? "bit-ico-inl" : string.Empty);

        ClassBuilder.Register(() => Rotate switch
        {
            BitIconRotate.Rotate90 => "bit-ico-rt90",
            BitIconRotate.Rotate180 => "bit-ico-rt180",
            BitIconRotate.Rotate270 => "bit-ico-rt270",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Flip switch
        {
            BitIconFlip.Horizontal => "bit-ico-flh",
            BitIconFlip.Vertical => "bit-ico-flv",
            BitIconFlip.Both => "bit-ico-flb",
            _ => string.Empty
        });

        ClassBuilder.Register(() => FlipRtl ? "bit-ico-frt" : string.Empty);

        // Rotation and mirroring are two halves of one transform, so the class that draws it is
        // registered once for whichever of them is in play - two rules each setting `transform`
        // would leave only the later one standing.
        ClassBuilder.Register(() => Rotate.HasValue || RotateAngle.HasValue || Flip.HasValue || FlipRtl ? "bit-ico-trn" : string.Empty);

        ClassBuilder.Register(() => Animation switch
        {
            BitIconAnimation.Spin => "bit-ico-spn",
            BitIconAnimation.SpinReverse => "bit-ico-spr",
            BitIconAnimation.Pulse => "bit-ico-pls",
            BitIconAnimation.Beat => "bit-ico-bet",
            BitIconAnimation.Fade => "bit-ico-fad",
            BitIconAnimation.Shake => "bit-ico-shk",
            BitIconAnimation.Bounce => "bit-ico-bnc",
            BitIconAnimation.BeatFade => "bit-ico-btf",
            _ => string.Empty
        });

        // The duration is a longhand written after every animation shorthand, so one class covers
        // whichever animation is playing - and it only exists while a duration was actually given,
        // since the rule would otherwise resolve to nothing and stop the animation altogether.
        ClassBuilder.Register(() => AnimationDuration.HasValue() ? "bit-ico-anm" : string.Empty);

        ClassBuilder.Register(() => AnimationDelay.HasValue() ? "bit-ico-dly" : string.Empty);

        ClassBuilder.Register(() => OnClick.HasDelegate ? "bit-ico-int" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => FontSize.HasValue() ? $"font-size:{FontSize}" : string.Empty);

        // An angle of one's own is the same custom property the quarter-turn classes set, written where
        // it wins over them: an inline style beats a class rule, so RotateAngle replaces Rotate for free.
        StyleBuilder.Register(() => RotateAngle.HasValue ? $"--bit-ico-rotate:{RotateAngle.Value.ToString(CultureInfo.InvariantCulture)}deg" : string.Empty);

        StyleBuilder.Register(() => AnimationDuration.HasValue() ? $"--bit-ico-anm-dur:{AnimationDuration}" : string.Empty);

        StyleBuilder.Register(() => AnimationDelay.HasValue() ? $"--bit-ico-anm-dly:{AnimationDelay}" : string.Empty);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitIconParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // After the cascaded parameters, since the resolver is one of the things they carry.
        ResolveIcon();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Space scrolls the page by default, and an icon that answers Space is a control the reader
        // pressed it on rather than a place to scroll from. A Blazor keydown handler cannot decide that
        // per key, so it is stopped on a listener of the browser's own - registered for an icon that has
        // a handler, and emptied again for one that has lost it, since the listener stays on the element
        // and reads the key list on every event.
        var interactive = OnClick.HasDelegate;

        if (interactive == _preventKeysRegistered) return;

        _preventKeysRegistered = interactive;

        try
        {
            await _js.BitUtilsRegisterPreventKeys(RootElement, interactive ? [" ", "Spacebar"] : []);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, nothing to register
        catch (JSException) { } // a JS-side failure here only costs the page-scroll prevention
    }



    /// <summary>
    /// Gives focus to the icon element.
    /// </summary>
    /// <remarks>
    /// Only an icon the browser can focus takes it: one with an <see cref="OnClick"/> handler, or one
    /// given a <see cref="BitComponentBase.TabIndex"/> of its own. A decorative icon is not a tab stop
    /// and this does nothing to it.
    /// </remarks>
    /// <returns>
    /// A ValueTask that represents the asynchronous focus operation.
    /// </returns>
    public ValueTask FocusAsync() => RootElement.FocusAsync();

    /// <summary>
    /// Gives focus to the icon element, optionally without scrolling it into view.
    /// </summary>
    /// <param name="preventScroll">
    /// True to leave the page scrolled where it is instead of bringing the icon into view.
    /// </param>
    /// <returns>
    /// A ValueTask that represents the asynchronous focus operation.
    /// </returns>
    public ValueTask FocusAsync(bool preventScroll) => RootElement.FocusAsync(preventScroll);



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    /// <summary>
    /// An icon that answers the pointer has to answer the keyboard as well: a native button is
    /// activated by Enter and by Space, and an element merely given the button role earns neither for
    /// free. Enter fires on the way down and Space on the way up, which is what a real button does and
    /// what lets a reader who pressed Space by mistake move off the icon before releasing it.
    /// </summary>
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        // The release is what activates the icon, so the press is remembered rather than acted on: a
        // Space released here that was never pressed here - the pointer moved the focus onto the icon
        // while the key was already down - is not an activation of it.
        if (IsSpace(e))
        {
            _spacePressed = true;

            return;
        }

        if (e.Key != "Enter") return;

        await OnClick.InvokeAsync(ToActivationArgs(e));
    }

    private async Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (IsSpace(e) is false) return;

        if (_spacePressed is false) return;

        _spacePressed = false;

        await OnClick.InvokeAsync(ToActivationArgs(e));
    }

    private static bool IsSpace(KeyboardEventArgs e) => e.Key is " " or "Spacebar" || e.Code is "Space";

    // A click that came from a key carries no pointer: a detail of 0 is how the platform itself marks
    // one, and the modifiers are the only thing the keystroke has to pass on.
    private static MouseEventArgs ToActivationArgs(KeyboardEventArgs e) => new()
    {
        Detail = 0,
        AltKey = e.AltKey,
        CtrlKey = e.CtrlKey,
        MetaKey = e.MetaKey,
        ShiftKey = e.ShiftKey
    };
}
