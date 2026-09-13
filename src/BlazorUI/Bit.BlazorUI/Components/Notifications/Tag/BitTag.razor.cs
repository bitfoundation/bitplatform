using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Tag component provides a visual representation of an attribute, person, or asset.
/// </summary>
public partial class BitTag : BitComponentBase
{
    private string? _rel;
    private ElementReference _contentRef;
    private ElementReference _dismissRef;

    // A tag that leads somewhere is an anchor, and a tag that does something - acting on the page or
    // toggling itself - is a button. Anything else is a label, which is not a control and never takes focus.
    private bool _isLink => Href.HasValue();
    private bool _isToggle => SelectedChanged.HasDelegate || OnChange.HasDelegate || DefaultSelected.HasValue;
    private bool _isButton => _isLink is false && (OnClick.HasDelegate || _isToggle);
    private bool _isInteractive => _isLink || _isButton;

    // The dismiss button carries no text of its own, so the name it is given is the only thing a screen
    // reader has to announce it by. A row of them all called "Dismiss" names none of the tags they remove,
    // so with no name of its own the button is named after the text of the tag; a tag with no text either
    // falls back to a bare "Dismiss".
    private string _dismissLabel => DismissLabel
                                 ?? (Text.HasValue() ? Format(DismissLabelFormat ?? "Remove {0}", Text!) : "Dismiss");

    private static readonly Dictionary<BitNavAriaCurrent, string> _AriaCurrentMap = new()
    {
        [BitNavAriaCurrent.Page] = "page",
        [BitNavAriaCurrent.Step] = "step",
        [BitNavAriaCurrent.Location] = "location",
        [BitNavAriaCurrent.Time] = "time",
        [BitNavAriaCurrent.Date] = "date",
        [BitNavAriaCurrent.True] = "true"
    };



    /// <summary>
    /// Gets or sets the cascading parameters for the tag component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple tag components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitTagParams.ParamName)]
    public BitTagParams? CascadingParameters { get; set; }



    /// <summary>
    /// What a selected tag that is a link reports itself as through <c>aria-current</c>.
    /// <br />
    /// The default value is <strong>BitNavAriaCurrent.True</strong>.
    /// </summary>
    /// <remarks>
    /// It only ever reaches the anchor a tag becomes while <see cref="Href"/> is set, and only while
    /// <see cref="Selected"/> is true - a tag that is a button reports its selection through
    /// <c>aria-pressed</c> instead. Set it to <c>Page</c> for a tag standing for the page the reader is
    /// already on, which is what tells a screen reader that this one of a set of links is the destination
    /// rather than merely a highlighted one.
    /// </remarks>
    [Parameter] public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.True;

    /// <summary>
    /// The detailed description of the tag for the benefit of screen readers, rendered into a visually
    /// hidden element the tag points at with <c>aria-describedby</c>.
    /// </summary>
    /// <remarks>
    /// A description is read after the name of whatever carries it, so use it for what does not belong in
    /// the name itself - why a filter is unavailable, what dismissing the tag will do. It lands on the
    /// anchor or the button the tag becomes while it is a control, and on the root of the tag otherwise.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// Child content of component, the content that the tag will apply to.
    /// </summary>
    /// <remarks>
    /// It replaces <see cref="Text"/> and <see cref="SecondaryText"/> only; an <see cref="Icon"/>, an
    /// <see cref="IconUrl"/>, the checkmark of a selected tag and a <see cref="SecondaryIcon"/> all keep
    /// rendering around it, so a template does not have to reproduce them. Use
    /// <see cref="PrefixTemplate"/> and <see cref="SuffixTemplate"/> for markup that belongs beside the label
    /// rather than in place of it, which leaves the two lines of the label to <see cref="Text"/> and
    /// <see cref="SecondaryText"/>.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the tag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The initial value of <see cref="Selected"/> for a tag that keeps its own selection.
    /// </summary>
    /// <remarks>
    /// Setting it is the whole of what an uncontrolled filter chip needs: the tag becomes a toggle that flips
    /// and paints itself, starting from this value, without the app holding a field for it. Use
    /// <see cref="Selected"/> instead where the app owns the value - a tag whose <see cref="Selected"/> is set
    /// one way, without binding it, is one the tag itself is not allowed to change.
    /// </remarks>
    [Parameter] public bool? DefaultSelected { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the dismiss button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DismissIconName"/> when both are set.
    /// Defaults to the built-in Cancel icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom dismiss icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="DismissIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? DismissIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the dismiss button from the built-in Fluent UI icons.
    /// Defaults to <c>Cancel</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.ChromeClose</c>).
    /// <br />
    /// For external icon libraries, use <see cref="DismissIcon"/> instead.
    /// </remarks>
    [Parameter] public string? DismissIconName { get; set; }

    /// <summary>
    /// The accessible name and the tooltip of the dismiss button.
    /// </summary>
    /// <remarks>
    /// The button carries a glyph rather than words, so this is the only thing a screen reader has to
    /// announce it by. With no value here it is named after the <see cref="Text"/> of the tag through
    /// <see cref="DismissLabelFormat"/> - "Remove Design" - and falls back to a bare "Dismiss" on a tag that
    /// carries no text of its own; set this where neither reading is the right one.
    /// </remarks>
    [Parameter] public string? DismissLabel { get; set; }

    /// <summary>
    /// The format the dismiss button is named by while it has no <see cref="DismissLabel"/> of its own,
    /// where <c>{0}</c> is the <see cref="Text"/> of the tag.
    /// <br />
    /// The default value is <strong>Remove {0}</strong>.
    /// </summary>
    /// <remarks>
    /// This is what names a whole list of dismissible tags at once, and what translates that name: the
    /// fallback is English, and the word order of the language the app is in may not be the English one.
    /// </remarks>
    [Parameter] public string? DismissLabelFormat { get; set; }

    /// <summary>
    /// Prompts the browser to download the <see cref="Href"/> of the tag rather than to navigate to it,
    /// using the value as the suggested file name.
    /// </summary>
    /// <remarks>
    /// An empty string keeps the name the server suggests. It is the <c>download</c> attribute of the anchor
    /// the tag becomes, so it does nothing without an <see cref="Href"/> and only applies to same-origin,
    /// <c>blob:</c> and <c>data:</c> URLs.
    /// </remarks>
    [Parameter] public string? Download { get; set; }

    /// <summary>
    /// Stretches the tag to fill the width of whatever holds it, instead of shrinking to its content.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Use it for a tag standing on a line of its own - in a narrow side panel, or as a row of a stacked
    /// list - where a chip hugging its label leaves the line looking unfinished.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Hides the checkmark a selected tag shows in front of its content.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The checkmark is what makes a selected tag readable as picked rather than as merely a different
    /// color; hide it only where the selection is already unmistakable without it.
    /// </remarks>
    [Parameter] public bool HideSelectedIcon { get; set; }

    /// <summary>
    /// The URL the tag navigates to, which also turns the tag into a link.
    /// </summary>
    /// <remarks>
    /// A tag that leads somewhere is a real anchor: it is focusable, it is activated with the Enter key, it
    /// offers the context menu and the middle click every link of the page offers, and a screen reader
    /// announces it as a link. Use it for a tag that opens what it stands for - a category, a person, a
    /// document - and <see cref="OnClick"/> for one that acts on the page it is already on. The two can be
    /// set together, in which case the handler runs and the navigation still happens.
    /// <br />
    /// While <c>IsEnabled</c> is false the href is dropped and the tag is taken out of the tab order, so a
    /// disabled link cannot be followed by either the pointer or the keyboard.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The text alternative of the <see cref="IconUrl"/> picture.
    /// </summary>
    /// <remarks>
    /// The picture sits next to a label that already says what the tag is, so it is decorative by default
    /// and renders with an empty alt. Set this only where the picture says something the label does not.
    /// <br />
    /// It reaches nothing but that picture, which only renders while no <see cref="Icon"/> and no
    /// <see cref="IconName"/> has taken its place - a tag showing a glyph is named by its label or by its
    /// <c>AriaLabel</c>, never by this.
    /// </remarks>
    [Parameter] public string? IconAlt { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.AddFriend</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// The value is case-sensitive and must match a valid icon identifier.
    /// If not set or set to <c>null</c>, no icon will be rendered.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The URL of a picture to show in place of the icon, for example the avatar of the person the tag stands for.
    /// </summary>
    /// <remarks>
    /// It is rendered only while neither <see cref="Icon"/> nor <see cref="IconName"/> is set, since the two
    /// occupy the same place at the head of the tag. The picture is cropped to a circle the height of the
    /// label, so a portrait of any aspect ratio reads as an avatar.
    /// </remarks>
    [Parameter] public string? IconUrl { get; set; }

    /// <summary>
    /// Keeps the content of the tag on a single line and ends it with an ellipsis where it does not fit.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A tag wraps onto as many lines as its content needs by default, which is what keeps a long label
    /// readable. Turn this on where the layout has a width of its own to protect - a row of filters, a cell
    /// of a grid - and pair it with a <see cref="Title"/> so the part that was cut off is still reachable.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// Callback for when the <see cref="Selected"/> value of the tag has changed.
    /// </summary>
    /// <remarks>
    /// Setting it - or binding <see cref="Selected"/>, or giving a <see cref="DefaultSelected"/> - is what
    /// turns the tag into a toggle: it becomes a button that flips its own selection on every activation and
    /// reports that state through <c>aria-pressed</c>.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Callback invoked before the <see cref="Selected"/> value of the tag changes, letting the change be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitTagChangeArgs"/> to keep the current selection. Since
    /// the callback is awaited, it can also run asynchronous work first - a confirmation, a request that has
    /// to succeed before the filter is applied.
    /// </remarks>
    [Parameter] public EventCallback<BitTagChangeArgs> OnChanging { get; set; }

    /// <summary>
    /// Click event handler of the tag, which also turns the tag into a button.
    /// </summary>
    /// <remarks>
    /// While it is set the tag is focusable and can be activated with the Enter and the Space keys, and it
    /// stops being so as soon as <c>IsEnabled</c> is false. A tag with no handler, no <see cref="Href"/> and
    /// no selection to toggle never takes focus: it is a label rather than a control.
    /// <br />
    /// A control needs a name, so a tag that carries no text of its own - an icon-only one, or one built out
    /// of a <see cref="ChildContent"/> that renders no words - should be given an <c>AriaLabel</c> as soon as
    /// it becomes one.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Dismiss button click event, if set the dismiss icon will show up.
    /// </summary>
    /// <remarks>
    /// The button is a control of its own next to the content of the tag, so a click on it never reaches
    /// <see cref="OnClick"/>. It can also be triggered from the keyboard with the Delete and the Backspace
    /// keys while the focus is on any control the tag renders - the button itself, and the anchor or button
    /// the tag becomes while it is one - in which case the callback is invoked with an empty
    /// <see cref="MouseEventArgs"/>.
    /// <br />
    /// With no name given to the button through <see cref="DismissLabel"/>, it is named after the
    /// <see cref="Text"/> of the tag - "Remove Design" - so a row of them names the tag each one removes
    /// rather than announcing "Dismiss" over and over.
    /// <br />
    /// The component does not remove itself: what the handler does with the dismissal - taking the tag out of
    /// a list, clearing a filter - is up to the app.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// Custom markup rendered at the head of the tag, ahead of the icon or the picture.
    /// </summary>
    /// <remarks>
    /// It is added to the head of the tag rather than put in place of anything, so an <see cref="Icon"/>, an
    /// <see cref="IconUrl"/> and the checkmark of a selected tag all keep rendering alongside it. Use it for
    /// what the leading slot needs that a glyph or a picture cannot be - an avatar with initials, a colored
    /// dot, a flag. It lives inside whatever the tag became, so it is part of the same target the pointer
    /// and the keyboard activate; putting a control of its own in there is putting a control inside a
    /// button, which is a control no assistive technology can reach.
    /// </remarks>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// The relationship between the current document and the linked one, rendered as the rel attribute of the
    /// anchor the tag becomes while <see cref="Href"/> is set.
    /// </summary>
    /// <remarks>
    /// With no value of its own, a tag opening in a new browsing context (<see cref="Target"/> of
    /// <c>_blank</c>) gets <c>rel="noopener"</c> on its own, which is what keeps the opened page from reaching
    /// back into this one.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Reverses the direction flow of the content of the tag.
    /// </summary>
    /// <remarks>
    /// Everything the tag lays out in a row swaps ends with it: the icon and the label, the trailing
    /// <see cref="SecondaryIcon"/>, the two templates and the dismiss button. It is the mirror of the
    /// default order rather than a change of the writing direction; for the latter, use <c>Dir</c>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The trailing icon of the tag, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SecondaryIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// It is rendered after the label - and after a <see cref="ChildContent"/> - and before the dismiss
    /// button, inside whatever the tag becomes, so it is part of the same target the pointer and the
    /// keyboard activate rather than a control of its own. <see cref="Reversed"/> mirrors it along with the
    /// rest of the row. Like the leading icon it is decorative and hidden from assistive technologies, so
    /// whatever it says has to be said in the label or in the <c>AriaLabel</c> as well.
    /// </remarks>
    [Parameter] public BitIconInfo? SecondaryIcon { get; set; }

    /// <summary>
    /// The name of the trailing icon of the tag, from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// Use it for the glyph that closes the tag rather than opening it - the chevron of a chip that drops a
    /// menu, the arrow of one that leads somewhere - and <see cref="IconName"/> for the one that names what
    /// the tag stands for. For external icon libraries, use <see cref="SecondaryIcon"/> instead.
    /// </remarks>
    [Parameter] public string? SecondaryIconName { get; set; }

    /// <summary>
    /// The secondary text of the tag, rendered under the <see cref="Text"/> in a quieter type.
    /// </summary>
    /// <remarks>
    /// Use it for what qualifies the tag rather than what names it - the role under a person, the count under
    /// a category - and keep it short: a tag is a label, not a card.
    /// </remarks>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// Marks the tag as selected, which paints it in its selected colors and shows a checkmark in front of its content.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Bind it - or set <see cref="OnChange"/> - to turn the tag into a filter chip: it becomes a button that
    /// flips this value on every activation and reports it to assistive technologies through <c>aria-pressed</c>.
    /// <br />
    /// Set without either, it is a static "this one is picked" state the app drives on its own, and one the
    /// tag is not allowed to change - use <see cref="DefaultSelected"/> for a tag that keeps its own. Nothing
    /// announces a static selection either - a tag that is not a control has no state to report - so where it
    /// carries meaning rather than decoration, say so in the <c>AriaLabel</c> or in the text of the tag itself.
    /// <br />
    /// A tag that is a link reports it as <c>aria-current</c> instead, which is what marks the picked one of
    /// a set of links; <c>aria-pressed</c> belongs to a button and would say nothing on an anchor.
    /// </remarks>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool Selected { get; set; }

    /// <summary>
    /// The icon of the checkmark a selected tag shows, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SelectedIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? SelectedIcon { get; set; }

    /// <summary>
    /// The name of the icon of the checkmark a selected tag shows, from the built-in Fluent UI icons.
    /// Defaults to <c>Accept</c> when not set.
    /// </summary>
    [Parameter] public string? SelectedIconName { get; set; }

    /// <summary>
    /// The corner shape of the tag.
    /// </summary>
    /// <remarks>
    /// The default follows the chip corner of the current theme, which is what keeps a tag in the same visual
    /// language as the rest of the library; the other two pin it to a pill or to a rectangle whatever the
    /// theme says.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitTagShape? Shape { get; set; }

    /// <summary>
    /// The size of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom markup rendered at the end of the tag, after the trailing <see cref="SecondaryIcon"/> and
    /// before the dismiss button.
    /// </summary>
    /// <remarks>
    /// The mirror of <see cref="PrefixTemplate"/>, and subject to the same rules: it is added rather than
    /// substituted, it lives inside whatever the tag became, and it names nothing on its own. Use it for a
    /// count, a second avatar, a status dot - and <see cref="SecondaryIcon"/> where a glyph is all it is.
    /// </remarks>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// Stops the click of the tag from bubbling any further up the DOM.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Use it for a tag sitting inside something else that reacts to a click - a row, a card, a list item -
    /// where activating the tag should not also activate what holds it. The dismiss button stops the click
    /// of its own whether or not this is set.
    /// </remarks>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the tag.
    /// </summary>
    [Parameter] public BitTagClassStyles? Styles { get; set; }

    /// <summary>
    /// The browsing context the <see cref="Href"/> of the tag is opened in, for example <c>_blank</c>.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Target { get; set; }

    /// <summary>
    /// The text of the tag.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the tag.
    /// </summary>
    /// <remarks>
    /// It is what spells out whatever the tag shortens - the full label behind a <see cref="NoWrap"/>
    /// ellipsis, or the reading behind an icon. A title is not a text alternative, so what a screen reader
    /// should hear still belongs in <c>AriaLabel</c>.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the tag.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-tag";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-tag-pri",
            BitColor.Secondary => "bit-tag-sec",
            BitColor.Tertiary => "bit-tag-ter",
            BitColor.Info => "bit-tag-inf",
            BitColor.Success => "bit-tag-suc",
            BitColor.Warning => "bit-tag-wrn",
            BitColor.SevereWarning => "bit-tag-swr",
            BitColor.Error => "bit-tag-err",
            BitColor.PrimaryBackground => "bit-tag-pbg",
            BitColor.SecondaryBackground => "bit-tag-sbg",
            BitColor.TertiaryBackground => "bit-tag-tbg",
            BitColor.PrimaryForeground => "bit-tag-pfg",
            BitColor.SecondaryForeground => "bit-tag-sfg",
            BitColor.TertiaryForeground => "bit-tag-tfg",
            BitColor.PrimaryBorder => "bit-tag-pbr",
            BitColor.SecondaryBorder => "bit-tag-sbr",
            BitColor.TertiaryBorder => "bit-tag-tbr",
            _ => "bit-tag-pri"
        });

        ClassBuilder.Register(() => Reversed ? "bit-tag-rvs" : string.Empty);

        ClassBuilder.Register(() => NoWrap ? "bit-tag-nwr" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-tag-flw" : string.Empty);

        ClassBuilder.Register(() => Selected ? $"bit-tag-sel {Classes?.Selected}" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tag-sm",
            BitSize.Medium => "bit-tag-md",
            BitSize.Large => "bit-tag-lg",
            _ => "bit-tag-md"
        });

        ClassBuilder.Register(() => Shape switch
        {
            BitTagShape.Rounded => "bit-tag-rnd",
            BitTagShape.Circular => "bit-tag-cir",
            BitTagShape.Square => "bit-tag-sqr",
            _ => "bit-tag-rnd"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-tag-fil",
            BitVariant.Outline => "bit-tag-otl",
            BitVariant.Text => "bit-tag-txt",
            _ => "bit-tag-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Selected ? Styles?.Selected : string.Empty);
    }



    protected override async Task OnInitializedAsync()
    {
        if (DefaultSelected.HasValue)
        {
            await AssignSelected(DefaultSelected.Value);
        }

        await base.OnInitializedAsync();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitTagParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    /// <summary>
    /// Gives the keyboard focus to the tag.
    /// </summary>
    /// <remarks>
    /// It focuses whatever the tag offers the keyboard: the anchor or the button it becomes while it is a
    /// control, the dismiss button of a tag that only has one, and the root of a tag that is neither - which
    /// is exactly the case where the <c>TabIndex</c> of the tag lands on that root, so a plain tag given one
    /// can be focused and a plain tag without one cannot.
    /// <br />
    /// This is what a list of dismissible tags moves the focus on with after removing one of them: a focus
    /// left on an element that is gone falls back to the document, and the keyboard user loses their place.
    /// </remarks>
    public ValueTask FocusAsync()
    {
        if (_isInteractive) return _contentRef.FocusAsync();

        return OnDismiss.HasDelegate ? _dismissRef.FocusAsync() : RootElement.FocusAsync();
    }



    // A format string is app-supplied, so a wrong one is a typo rather than an exception: the tag falls back
    // to naming the button after itself.
    private static string Format(string format, string text)
    {
        try
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, format, text);
        }
        catch (FormatException)
        {
            return text;
        }
    }



    private async Task HandleOnDismissClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnDismiss.InvokeAsync(e);
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        if (_isToggle is false) return;

        var value = Selected is false;

        if (OnChanging.HasDelegate)
        {
            var args = new BitTagChangeArgs(value);

            await OnChanging.InvokeAsync(args);

            if (args.Cancel) return;
        }

        if (await AssignSelected(value) is false) return;

        await OnChange.InvokeAsync(Selected);
    }

    /// <summary>
    /// Removes the tag from the keyboard, which is what every chip of every other library does and what a
    /// keyboard user reaches for once the pointer has shown them a dismiss button.
    /// </summary>
    /// <remarks>
    /// The handler is only ever attached to elements inside a tag that has something to dismiss, and it
    /// leaves every other key - including the Enter and the Space a button is activated with - alone.
    /// </remarks>
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (OnDismiss.HasDelegate is false) return;

        if (e.Key is not ("Delete" or "Backspace")) return;

        await OnDismiss.InvokeAsync(new MouseEventArgs());
    }

    internal void OnSetHrefAndRel()
    {
        if (Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        if (Rel.HasValue)
        {
            _rel = BitLinkRelUtils.GetRels(Rel.Value);
            return;
        }

        // protects against reverse-tabnabbing when opening the link in a new browsing context. The target
        // attribute is matched case-insensitively by the browser, so a "_BLANK" opens the same new context
        // and has to be recognized here as one.
        _rel = string.Equals(Target, "_blank", StringComparison.OrdinalIgnoreCase) ? "noopener" : null;
    }
}
