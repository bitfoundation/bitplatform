using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Tag component provides a visual representation of an attribute, person, or asset.
/// </summary>
public partial class BitTag : BitComponentBase
{
    private string? _rel;

    // A tag that leads somewhere is an anchor, and a tag that does something - acting on the page or
    // toggling itself - is a button. Anything else is a label, which is not a control and never takes focus.
    private bool _isLink => Href.HasValue();
    private bool _isToggle => SelectedChanged.HasDelegate || OnChange.HasDelegate;
    private bool _isButton => _isLink is false && (OnClick.HasDelegate || _isToggle);
    private bool _isInteractive => _isLink || _isButton;

    // The dismiss button carries no text of its own, so the name it is given is the only thing a screen
    // reader has to announce it by. It falls back to a bare "Dismiss" for the app that never sets one.
    private string _dismissLabel => DismissLabel ?? "Dismiss";



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
    /// Child content of component, the content that the tag will apply to.
    /// </summary>
    /// <remarks>
    /// It replaces <see cref="Text"/> and <see cref="SecondaryText"/> only; an <see cref="Icon"/>, an
    /// <see cref="IconUrl"/> and the checkmark of a selected tag keep rendering before it, so a template
    /// does not have to reproduce them.
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
    /// <br />
    /// The default value is <strong>Dismiss</strong>.
    /// </summary>
    /// <remarks>
    /// The button carries a glyph rather than words, so this is the only thing a screen reader has to
    /// announce it by. Set it to something that names what is being removed - "Remove the Design tag" reads
    /// far better than a row of buttons all called "Dismiss" - and to the language of the app, since the
    /// fallback is English.
    /// </remarks>
    [Parameter] public string? DismissLabel { get; set; }

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
    /// Setting it - or binding <see cref="Selected"/> - is what turns the tag into a toggle: it becomes a
    /// button that flips its own selection on every activation and reports that state through <c>aria-pressed</c>.
    /// </remarks>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

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
    /// keys while the focus is anywhere inside the tag, in which case the callback is invoked with an empty
    /// <see cref="MouseEventArgs"/>.
    /// <br />
    /// The component does not remove itself: what the handler does with the dismissal - taking the tag out of
    /// a list, clearing a filter - is up to the app.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

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
    /// The icon moves after the label and the dismiss button to the head of the tag, which is the mirror of
    /// the default order rather than a change of the writing direction; for the latter, use <c>Dir</c>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

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
    /// Set without either, it is a static "this one is picked" state the app drives on its own. Nothing then
    /// announces it - a tag that is not a control has no state to report - so where the selection carries
    /// meaning rather than decoration, say so in the <c>AriaLabel</c> or in the text of the tag itself.
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



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitTagParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
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

        if (await AssignSelected(Selected is false) is false) return;

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

    private void OnSetHrefAndRel()
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

        // protects against reverse-tabnabbing when opening the link in a new browsing context
        _rel = Target == "_blank" ? "noopener" : null;
    }
}
