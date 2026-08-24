using System.Globalization;
using System.Text;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace Bit.BlazorUI;

/// <summary>
/// A BitPersona is a visual representation of a person across products: their picture, or the initials
/// derived from their name, or an icon when there is neither, with up to four lines of detail beside it and
/// a presence dot for whether they are around.
/// </summary>
public partial class BitPersona : BitComponentBase
{
    private bool _isLoaded;
    private bool _hasError;



    /// <summary>
    /// The title of the action button (tooltip).
    /// </summary>
    [Parameter] public string ActionButtonTitle { get; set; } = "Edit image";

    /// <summary>
    /// Icon for the icon button of the custom action.
    /// </summary>
    /// <remarks>
    /// When both <see cref="ActionIcon"/> and <see cref="ActionIconName"/> are provided, <see cref="ActionIcon"/> takes precedence.
    /// Use this property when you need to configure an external or custom icon (for example, when using a custom icon font or SVG).
    /// </remarks>
    [Parameter] public BitIconInfo? ActionIcon { get; set; }

    /// <summary>
    /// Icon name for the icon button of the custom action.
    /// </summary>
    /// <remarks>
    /// This is a convenience property for specifying an icon from the built-in icon set.
    /// If <see cref="ActionIcon"/> is specified, this property is ignored and the value of <see cref="ActionIcon"/> will be used instead.
    /// </remarks>
    [Parameter] public string? ActionIconName { get; set; }

    /// <summary>
    /// Optional Custom template for the custom action element.
    /// </summary>
    [Parameter] public RenderFragment? ActionTemplate { get; set; }

    /// <summary>
    /// Marks the persona as active, which decorates its coin according to <see cref="ActiveAppearance"/>.
    /// </summary>
    /// <remarks>
    /// This is the usual way of pointing at the one person a view is about - the speaker in a call, the
    /// author of the message being read - without moving them out of the list they belong to.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Active { get; set; }

    /// <summary>
    /// How the coin is decorated while <see cref="Active"/> is true. The default is a ring.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPersonaActiveAppearance? ActiveAppearance { get; set; }

    /// <summary>
    /// Whether initials are derived from names that carry no letters at all - a phone number, an order id,
    /// a project named after a number sequence.
    /// </summary>
    /// <remarks>
    /// Two arbitrary numerals abbreviate into nothing a reader can recognize, so such a name is normally
    /// left to the coin icon instead. Turn this on where the numbers are the name.
    /// </remarks>
    [Parameter] public bool AllowPhoneInitials { get; set; }

    /// <summary>
    /// If true, automatically generates a coin background color derived from the person's name or initials.
    /// When set, this takes effect only when <see cref="CoinColor"/> is not explicitly provided.
    /// </summary>
    /// <remarks>
    /// The color is picked by a stable hash of <see cref="CoinColorSeed"/>, or - when that is not set - of
    /// <see cref="ImageInitials"/> or <see cref="PrimaryText"/>, so the same person always gets the same color.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool AutoCoinColor { get; set; }

    /// <summary>
    /// The colors <see cref="AutoCoinColor"/> is allowed to pick from, in place of the built-in set.
    /// </summary>
    /// <remarks>
    /// A palette of one pins every persona to that color; a palette that leaves out the semantic colors
    /// keeps a list of people from reading as a list of statuses. An empty or null value falls back to the
    /// built-in set.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public IEnumerable<BitColor>? AutoCoinColors { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitPersona component.
    /// </summary>
    [Parameter] public BitPersonaClassStyles? Classes { get; set; }

    /// <summary>
    /// The background color when the user's initials are displayed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? CoinColor { get; set; }

    /// <summary>
    /// The text <see cref="AutoCoinColor"/> hashes to pick a coin color, for when the color has to stay
    /// with the identity of the person rather than with the name being displayed.
    /// </summary>
    /// <remarks>
    /// A user id or an email address keeps the color stable while the displayed name changes, and keeps two
    /// different people who happen to share a name apart. When it is not set, <see cref="ImageInitials"/>
    /// and then <see cref="PrimaryText"/> are hashed instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? CoinColorSeed { get; set; }

    /// <summary>
    /// The icon rendered inside the coin in place of the initials.
    /// </summary>
    /// <remarks>
    /// When both <see cref="CoinIcon"/> and <see cref="CoinIconName"/> are provided, <see cref="CoinIcon"/> takes precedence.
    /// The icon is also what stands in for an image that failed to load when no initials can be derived.
    /// </remarks>
    [Parameter] public BitIconInfo? CoinIcon { get; set; }

    /// <summary>
    /// The name of the icon rendered inside the coin in place of the initials.
    /// </summary>
    /// <remarks>
    /// This is a convenience property for specifying an icon from the built-in icon set.
    /// If <see cref="CoinIcon"/> is specified, this property is ignored and the value of <see cref="CoinIcon"/> will be used instead.
    /// </remarks>
    [Parameter] public string? CoinIconName { get; set; }

    /// <summary>
    /// Optional custom persona coin size in pixel.
    /// </summary>
    /// <remarks>
    /// The initials and the presence dot are scaled along with the coin, so a custom size stays in proportion.
    /// </remarks>
    [Parameter] public int? CoinSize { get; set; }

    /// <summary>
    /// Custom persona coin's image template.
    /// </summary>
    [Parameter] public RenderFragment? CoinTemplate { get; set; }

    /// <summary>
    /// The variant of the coin.
    /// </summary>
    [Parameter] public BitVariant? CoinVariant { get; set; }

    /// <summary>
    /// Renders the persona in full width of its container element.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Whether to not render persona details, and just render the persona image/initials.
    /// </summary>
    /// <remarks>
    /// The coin that is left carries the name for anyone who cannot see it, on its own where nothing else
    /// can claim it. <see cref="BitPersonaSize.Size8"/> is the exception: it has no coin at all, so hiding
    /// its details would leave nothing to render, and they are kept.
    /// </remarks>
    [Parameter] public bool HidePersonaDetails { get; set; }

    /// <summary>
    /// Alt text for the image to use. default is empty string.
    /// </summary>
    /// <remarks>
    /// The name of the person is already announced from the details next to the coin, so the image is
    /// rendered as decorative (an empty alt) unless a description of the picture itself is given here.
    /// </remarks>
    [Parameter] public string? ImageAlt { get; set; }

    /// <summary>
    /// Captures additional HTML attributes to be applied to the rendered img element of the coin.
    /// </summary>
    /// <remarks>
    /// This is where attributes the component does not publish of its own - crossorigin, referrerpolicy,
    /// draggable, decoding, fetchpriority - are set.
    /// </remarks>
    [Parameter] public Dictionary<string, object> ImageAttributes { get; set; } = [];

    /// <summary>
    /// Fades the picture in as it is painted, instead of letting it appear from nothing.
    /// </summary>
    /// <remarks>
    /// A list of faces that pops in one picture at a time reads as flicker; fading them in turns the same
    /// arrival into something the eye can follow. The fade is an animation rather than a state hung off the
    /// load event, which is what keeps a picture that was already in the cache - or a page that was rendered
    /// statically, and has no handler to fire that event at all - from being left hidden for good.
    /// </remarks>
    [Parameter] public bool ImageFadeIn { get; set; }

    /// <summary>
    /// The user's initials to display in the image area when there is no image.
    /// </summary>
    /// <remarks>
    /// When this is not set, the initials are derived from <see cref="PrimaryText"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? ImageInitials { get; set; }

    /// <summary>
    /// Specifies the loading behavior of the image (e.g., "lazy" or "eager").
    /// </summary>
    [Parameter] public BitImageLoading? ImageLoading { get; set; }

    /// <summary>
    /// Optional Custom template for the image overlay.
    /// </summary>
    [Parameter] public RenderFragment? ImageOverlayTemplate { get; set; }

    /// <summary>
    /// The text of the image overlay.
    /// </summary>
    [Parameter] public string ImageOverlayText { get; set; } = "Edit image";

    /// <summary>
    /// The set of media conditions that tells the browser which of the <see cref="ImageSrcSet"/> candidates
    /// to pick (maps to the img sizes attribute).
    /// </summary>
    [Parameter] public string? ImageSizes { get; set; }

    /// <summary>
    /// A set of image source URLs for different display densities or sizes (maps to the img srcset attribute).
    /// </summary>
    /// <remarks>
    /// Changing it starts a fresh load the same way changing <see cref="ImageUrl"/> does, so a coin that had
    /// fallen back to its initials is given the new candidates rather than being left on the old verdict.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetImageSource))]
    public string? ImageSrcSet { get; set; }

    /// <summary>
    /// Url to the image to use, should be a square aspect ratio and big enough to fit in the image area.
    /// </summary>
    /// <remarks>
    /// A coin given nothing but <see cref="ImageSrcSet"/> is still a coin with a picture in it: the browser
    /// picks one of the candidates, and this is only the source it falls back to.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetImageSource))]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Marks the persona as one of the people a view is not about, which shrinks its coin slightly and
    /// fades it back.
    /// </summary>
    /// <remarks>
    /// This is the counterpart of <see cref="Active"/>: in a call grid it is what everyone who is not
    /// speaking looks like. It is ignored while <see cref="Active"/> is true, since a persona cannot be
    /// both at once.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Inactive { get; set; }

    /// <summary>
    /// Callback for the persona custom action.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnActionClick { get; set; }

    /// <summary>
    /// Callback for when the image clicked.
    /// </summary>
    /// <remarks>
    /// Assigning it renders the coin as a real button element, so it is announced as one, takes the focus,
    /// answers Enter and Space, and goes inert with the rest of the persona when it is disabled.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public EventCallback<MouseEventArgs> OnImageClick { get; set; }

    /// <summary>
    /// Callback for when the image fails to load.
    /// </summary>
    [Parameter] public EventCallback<ErrorEventArgs> OnImageError { get; set; }

    /// <summary>
    /// Callback for when the image successfully loads.
    /// </summary>
    [Parameter] public EventCallback<ProgressEventArgs> OnImageLoad { get; set; }

    /// <summary>
    /// Optional text to display, usually a custom message set.
    /// The optional text will only be shown when using size100 or size120.
    /// </summary>
    [Parameter] public string? OptionalText { get; set; }

    /// <summary>
    /// Custom optional text template.
    /// </summary>
    [Parameter] public RenderFragment? OptionalTextTemplate { get; set; }

    /// <summary>
    /// Presence of the person to display - will not display presence if undefined.
    /// </summary>
    [Parameter] public BitPersonaPresence Presence { get; set; }

    /// <summary>
    /// The icon rendered inside the presence dot of the current <see cref="Presence"/>.
    /// </summary>
    /// <remarks>
    /// This is the single-status counterpart of <see cref="PresenceIcons"/>, for a persona that only ever
    /// shows one status and has no reason to carry a map of all of them. A matching entry in
    /// <see cref="PresenceIcons"/> or <see cref="PresenceIconNames"/> takes precedence over it, and
    /// <see cref="PresenceIcon"/> itself takes precedence over <see cref="PresenceIconName"/>.
    /// The two smallest coins have no room for a glyph and show none.
    /// </remarks>
    [Parameter] public BitIconInfo? PresenceIcon { get; set; }

    /// <summary>
    /// The name of the icon rendered inside the presence dot of the current <see cref="Presence"/>.
    /// </summary>
    /// <remarks>
    /// This is a convenience property for specifying an icon from the built-in icon set.
    /// If <see cref="PresenceIcon"/> is specified, this property is ignored and the value of
    /// <see cref="PresenceIcon"/> will be used instead.
    /// </remarks>
    [Parameter] public string? PresenceIconName { get; set; }

    /// <summary>
    /// The icons to be used for the presence status with <see cref="BitIconInfo"/>.
    /// </summary>
    /// <remarks>
    /// When both <see cref="PresenceIcons"/> and <see cref="PresenceIconNames"/> are provided, entries in
    /// <see cref="PresenceIcons"/> take precedence for the same <see cref="BitPersonaPresence"/> key.
    /// Use this dictionary when you need to configure presence icons using full <see cref="BitIconInfo"/> metadata
    /// (for example, to use custom icon sources or other advanced icon settings supported by <see cref="BitIconInfo"/>).
    /// <para>
    /// Example:
    /// <code>
    /// PresenceIcons = new()
    /// {
    ///     [BitPersonaPresence.Online] = new BitIconInfo { Name = "SkypeCircleCheck" },
    ///     [BitPersonaPresence.Offline] = new BitIconInfo { Name = "SkypeCircleMinus" }
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    [Parameter] public Dictionary<BitPersonaPresence, BitIconInfo>? PresenceIcons { get; set; }

    /// <summary>
    /// The icon names to be used for the presence status.
    /// </summary>
    /// <remarks>
    /// This dictionary is intended for simple scenarios where built-in icon names are sufficient.
    /// For any <see cref="BitPersonaPresence"/> value that also exists in <see cref="PresenceIcons"/>,
    /// the corresponding entry in <see cref="PresenceIconNames"/> is ignored.
    /// <para>
    /// Example:
    /// <code>
    /// PresenceIconNames = new()
    /// {
    ///     [BitPersonaPresence.Online] = "SkypeCircleCheck",
    ///     [BitPersonaPresence.Away] = "SkypeCircleClock"
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    [Parameter] public Dictionary<BitPersonaPresence, string>? PresenceIconNames { get; set; }

    /// <summary>
    /// Presence title to be shown as a tooltip on hover over the presence icon.
    /// </summary>
    /// <remarks>
    /// It also becomes the accessible name of the presence dot. When neither this nor a matching entry of
    /// <see cref="PresenceTitles"/> is given, the dot is still named after its status for screen readers.
    /// </remarks>
    [Parameter] public string? PresenceTitle { get; set; }

    /// <summary>
    /// The titles to be shown as a tooltip on hover over the presence dot, one per status.
    /// </summary>
    /// <remarks>
    /// This is what localizes the presence dot: the entry matching the current <see cref="Presence"/> becomes
    /// both its tooltip and its accessible name, taking precedence over <see cref="PresenceTitle"/>.
    /// <para>
    /// Example:
    /// <code>
    /// PresenceTitles = new()
    /// {
    ///     [BitPersonaPresence.Online] = "Available",
    ///     [BitPersonaPresence.Dnd] = "Do not disturb"
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    [Parameter] public Dictionary<BitPersonaPresence, string>? PresenceTitles { get; set; }

    /// <summary>
    /// Primary text to display, usually the name of the person.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? PrimaryText { get; set; }

    /// <summary>
    /// Custom primary text template.
    /// </summary>
    [Parameter] public RenderFragment? PrimaryTextTemplate { get; set; }

    /// <summary>
    /// Reverses the texts and image location.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Secondary text to display, usually the role of the user.
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// Custom secondary text template.
    /// </summary>
    [Parameter] public RenderFragment? SecondaryTextTemplate { get; set; }

    /// <summary>
    /// The outline of the coin: a circle, a rounded square or a sharp one. The default is a circle.
    /// </summary>
    /// <remarks>
    /// This supersedes <see cref="Squared"/>, which is the same thing said with a flag and can only reach
    /// <see cref="BitPersonaShape.Rounded"/>. When both are set, this one wins.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPersonaShape? Shape { get; set; }

    /// <summary>
    /// If true renders the initials while the image is loading. This only applies when an imageUrl is provided.
    /// </summary>
    /// <remarks>
    /// The initials sit behind the picture rather than beside it, so they are covered the moment it arrives -
    /// and an image element with nothing in it yet paints nothing, which is what lets them show through.
    /// </remarks>
    [Parameter] public bool ShowInitialsUntilImageLoads { get; set; }

    /// <summary>
    /// Whether each of the four detail texts carries itself as a native tooltip, for reading the part of it
    /// that the row had to clip. The default is true.
    /// </summary>
    /// <remarks>
    /// A tooltip repeating a name that was never clipped is noise, so turn this off where the rows are known
    /// to be wide enough for what goes in them.
    /// </remarks>
    [Parameter] public bool ShowOverflowTooltip { get; set; } = true;

    /// <summary>
    /// Shows the secondary text at every size, including the small ones that normally leave no room for it.
    /// </summary>
    /// <remarks>
    /// The second line is otherwise reserved for <see cref="BitPersonaSize.Size40"/> and up. This is what a
    /// compact list of people with a role or an address under each name reaches for.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ShowSecondaryText { get; set; }

    /// <summary>
    /// Decides the size of the control.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPersonaSize Size { get; set; } = BitPersonaSize.Size48;

    /// <summary>
    /// If true, renders the coin with a rounded square shape instead of the default circular shape.
    /// </summary>
    /// <remarks>
    /// This is the shorthand for <see cref="BitPersonaShape.Rounded"/>. Set <see cref="Shape"/> instead to
    /// reach the sharp square as well; a <see cref="Shape"/> of its own takes precedence over this.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Squared { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitPersona component.
    /// </summary>
    [Parameter] public BitPersonaClassStyles? Styles { get; set; }

    /// <summary>
    /// Tertiary text to display, usually the status of the user.
    /// The tertiary text will only be shown when using size72, size100 or size120.
    /// </summary>
    [Parameter] public string? TertiaryText { get; set; }

    /// <summary>
    /// Custom tertiary text template.
    /// </summary>
    [Parameter] public RenderFragment? TertiaryTextTemplate { get; set; }

    /// <summary>
    /// If true, show the special coin for unknown persona.
    /// It shows an icon in place of the initials, and takes precedence over the image and the initials.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Unknown { get; set; }

    /// <summary>
    /// Icon for the unknown persona coin.
    /// </summary>
    /// <remarks>
    /// When both <see cref="UnknownIcon"/> and <see cref="UnknownIconName"/> are provided, <see cref="UnknownIcon"/> takes precedence.
    /// Use this property when you need to configure an external or custom icon (for example, when using a custom icon font or SVG).
    /// </remarks>
    [Parameter] public BitIconInfo? UnknownIcon { get; set; }

    /// <summary>
    /// Icon name for the unknown persona coin.
    /// </summary>
    /// <remarks>
    /// This is a convenience property for specifying an icon from the built-in icon set.
    /// If <see cref="UnknownIcon"/> is specified, this property is ignored and the value of <see cref="UnknownIcon"/> will be used instead.
    /// </remarks>
    [Parameter] public string? UnknownIconName { get; set; }



    protected override string RootElementClass => "bit-prs";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullWidth ? "bit-prs-fwi" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-prs-rvs" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitPersonaSize.Size8 => "bit-prs-s8",
            BitPersonaSize.Size24 => "bit-prs-s24",
            BitPersonaSize.Size32 => "bit-prs-s32",
            BitPersonaSize.Size40 => "bit-prs-s40",
            BitPersonaSize.Size48 => "bit-prs-s48",
            BitPersonaSize.Size56 => "bit-prs-s56",
            BitPersonaSize.Size72 => "bit-prs-s72",
            BitPersonaSize.Size100 => "bit-prs-s100",
            BitPersonaSize.Size120 => "bit-prs-s120",
            _ => string.Empty
        });

        ClassBuilder.Register(() => OnImageClick.HasDelegate ? "bit-prs-iac" : string.Empty);

        ClassBuilder.Register(() => HasImage ? "bit-prs-him" : string.Empty);

        ClassBuilder.Register(() => Size is BitPersonaSize.Size8 ? string.Empty
                                  : CoinColor is not null ? GetCoinColorClass(CoinColor.Value)
                                  : AutoCoinColor ? GetAutoCoinColorClass()
                                  : "bit-prs-inf");

        ClassBuilder.Register(() => GetShape() switch
        {
            BitPersonaShape.Rounded => "bit-prs-sqr",
            BitPersonaShape.Square => "bit-prs-sqr bit-prs-sqs",
            _ => string.Empty
        });

        ClassBuilder.Register(() => (Active && Size is not BitPersonaSize.Size8) ? ActiveAppearance switch
        {
            BitPersonaActiveAppearance.Shadow => "bit-prs-ash",
            BitPersonaActiveAppearance.RingShadow => "bit-prs-ars",
            _ => "bit-prs-arg"
        } : string.Empty);

        ClassBuilder.Register(() => (Inactive && Active is false && Size is not BitPersonaSize.Size8) ? "bit-prs-ina" : string.Empty);

        ClassBuilder.Register(() => ShowSecondaryText ? "bit-prs-sst" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    /// <summary>
    /// The shape actually in force, which <see cref="Squared"/> can only ever ask for the rounded square of.
    /// </summary>
    private BitPersonaShape GetShape() => Shape ?? (Squared ? BitPersonaShape.Rounded : BitPersonaShape.Circular);

    private string? GetPresentationClass()
    {
        return Presence switch
        {
            BitPersonaPresence.Offline => "bit-prs-off",
            BitPersonaPresence.Online => "bit-prs-onl",
            BitPersonaPresence.Away => "bit-prs-awy",
            BitPersonaPresence.Dnd => "bit-prs-dnd",
            BitPersonaPresence.Blocked => "bit-prs-blk",
            BitPersonaPresence.Busy => "bit-prs-bsy",
            BitPersonaPresence.OutOfOffice => "bit-prs-oof",
            BitPersonaPresence.Unknown => "bit-prs-unk",
            _ => null
        };
    }

    private string? GetPresentationStyle()
    {
        if (CoinSize is null or <= 0) return Styles?.Presence;

        string? inset = null;
        // The dot keeps the same share of the coin a size class gives it, so a custom coin size stays in proportion.
        var presentationSize = CoinSize.Value / 4D;
        if (GetShape() is not BitPersonaShape.Circular)
        {
            // Retuned as the knob the stylesheet reads rather than as a side of its own, so the nudge follows
            // the dot to whichever corner the writing direction and Reversed between them put it in.
            inset = FormattableString.Invariant($"--bit-prs-presence-inset:-{presentationSize / 3D}px;");
        }
        return FormattableString.Invariant($"width:{presentationSize}px;height:{presentationSize}px;{inset}{Styles?.Presence?.Trim(';')}");
    }

    /// <summary>
    /// The glyph inside the presence dot, which the two smallest coins have no room for.
    /// </summary>
    private BitIconInfo? GetPresenceIcon()
    {
        if (Presence is BitPersonaPresence.None) return null;

        if (Size is BitPersonaSize.Size8 or BitPersonaSize.Size24 or BitPersonaSize.Size32) return null;

        if (PresenceIcons?.TryGetValue(Presence, out var icon) is true && icon is not null) return icon;

        if (PresenceIconNames?.TryGetValue(Presence, out var iconName) is true && iconName.HasValue())
        {
            return BitIconInfo.Bit(iconName!);
        }

        // The single-status pair is what a persona that only ever shows one status reaches for instead of
        // declaring a map of all eight, so it answers only where the map had nothing to say.
        return BitIconInfo.From(PresenceIcon, PresenceIconName);
    }

    /// <summary>
    /// What the presence dot is called - its tooltip when one was given, and its accessible name either way.
    /// </summary>
    private string? GetPresenceTitle()
    {
        if (Presence is BitPersonaPresence.None) return null;

        if (PresenceTitles?.TryGetValue(Presence, out var title) is true && title.HasValue()) return title;

        return PresenceTitle;
    }

    private string? GetPresenceLabel()
    {
        var title = GetPresenceTitle();
        if (title.HasValue()) return title;

        return Presence switch
        {
            BitPersonaPresence.Offline => "Offline",
            BitPersonaPresence.Online => "Online",
            BitPersonaPresence.Away => "Away",
            BitPersonaPresence.Dnd => "Do not disturb",
            BitPersonaPresence.Blocked => "Blocked",
            BitPersonaPresence.Busy => "Busy",
            BitPersonaPresence.OutOfOffice => "Out of office",
            BitPersonaPresence.Unknown => "Presence unknown",
            _ => null
        };
    }

    private string? GetCoinClass()
    {
        return CoinVariant switch
        {
            BitVariant.Fill => "bit-prs-fil",
            BitVariant.Outline => "bit-prs-otl",
            BitVariant.Text => "bit-prs-txt",
            _ => "bit-prs-fil"
        };
    }

    private static string GetCoinColorClass(BitColor color) => color switch
    {
        BitColor.Primary => "bit-prs-pri",
        BitColor.Secondary => "bit-prs-sec",
        BitColor.Tertiary => "bit-prs-ter",
        BitColor.Info => "bit-prs-inf",
        BitColor.Success => "bit-prs-suc",
        BitColor.Warning => "bit-prs-wrn",
        BitColor.SevereWarning => "bit-prs-swr",
        BitColor.Error => "bit-prs-err",
        BitColor.PrimaryBackground => "bit-prs-pbg",
        BitColor.SecondaryBackground => "bit-prs-sbg",
        BitColor.TertiaryBackground => "bit-prs-tbg",
        BitColor.PrimaryForeground => "bit-prs-pfg",
        BitColor.SecondaryForeground => "bit-prs-sfg",
        BitColor.TertiaryForeground => "bit-prs-tfg",
        BitColor.PrimaryBorder => "bit-prs-pbr",
        BitColor.SecondaryBorder => "bit-prs-sbr",
        BitColor.TertiaryBorder => "bit-prs-tbr",
        _ => "bit-prs-inf"
    };

    /// <summary>
    /// The colors a hashed coin is picked from when the caller names none: the accent and the semantic
    /// colors, which are the ones every preset repaints and every scheme keeps legible.
    /// </summary>
    private static readonly BitColor[] _defaultAutoCoinColors =
    [
        BitColor.Primary, BitColor.Secondary, BitColor.Tertiary,
        BitColor.Success, BitColor.Warning, BitColor.Error, BitColor.Info
    ];

    private string GetAutoCoinColorClass()
    {
        // Taken as it comes where it already is a list, so the common case of an array or a List<> passed
        // in by the caller costs nothing to read.
        var palette = (AutoCoinColors as IReadOnlyList<BitColor>) ?? AutoCoinColors?.ToArray();
        if (palette is null || palette.Count == 0) palette = _defaultAutoCoinColors;

        var text = (CoinColorSeed.HasValue() ? CoinColorSeed
                  : ImageInitials.HasValue() ? ImageInitials
                  : PrimaryText)?.Trim() ?? string.Empty;

        // Nothing to hash is not a color of its own: a persona with no name yet takes the first of the
        // palette, which for the built-in set is the same neutral the component defaults to anyway.
        if (text.HasNoValue()) return GetCoinColorClass(ReferenceEquals(palette, _defaultAutoCoinColors) ? BitColor.Info : palette[0]);

        // Stable DJB2 hash - not affected by .NET's randomized GetHashCode
        uint hash = 5381;
        foreach (var c in text)
        {
            hash = ((hash << 5) + hash) + c;
        }

        return GetCoinColorClass(palette[(int)(hash % (uint)palette.Count)]);
    }

    /// <summary>
    /// The icon that stands in for the initials: the one that was asked for, or - when there are no initials
    /// to show either - the generic person, so a coin is never left blank.
    /// </summary>
    private BitIconInfo? GetCoinIcon(string initials)
    {
        var icon = BitIconInfo.From(CoinIcon, CoinIconName);
        if (icon is not null) return icon;

        if (initials.HasValue()) return null;

        return BitIconInfo.Bit("Contact");
    }

    /// <summary>
    /// The class that shrinks a set of initials too long for the coin it has to fit in.
    /// </summary>
    /// <remarks>
    /// Two initials are what the coin is sized for, and what deriving them from a name can ever produce -
    /// but <see cref="ImageInitials"/> is passed through verbatim, and three or four letters at the coin's
    /// own font size are simply clipped by it. The steps are in em, so they follow every size class and any
    /// custom <see cref="CoinSize"/> without being restated per size.
    /// </remarks>
    private static string? GetInitialsFitClass(string initials)
    {
        if (initials.HasNoValue()) return null;

        // Counted in text elements rather than in chars: an emoji is two chars, and a letter carrying a
        // combining mark is two more, and neither takes the room of two letters.
        var length = new StringInfo(initials).LengthInTextElements;

        return length switch
        {
            <= 2 => null,
            3 => "bit-prs-in3",
            _ => "bit-prs-in4"
        };
    }

    private string GetInitials()
    {
        if (ImageInitials.HasValue()) return ImageInitials!;

        return CalculateInitials(PrimaryText, Dir == BitDir.Rtl, AllowPhoneInitials);
    }

    /// <summary>
    /// Derives up to two initials from a display name.
    /// </summary>
    /// <remarks>
    /// A display name is rarely the two clean words the naive reading of it assumes. It carries a parenthetical
    /// ("Elvia Atkins (Contoso)"), or punctuation ("Dr. Ted Randall Jr."), or is an address rather than a name
    /// ("carlos.slattery@contoso.com"), or is a phone number that has no initials at all - and any of its
    /// characters can be a surrogate pair or carry a combining mark, which indexing by char would cut in half.
    /// Each of those is dealt with before the words are counted, and the count then follows the same rule the
    /// rest of the industry settled on: two words give first and last, three skip the middle one, and anything
    /// else gives the first alone.
    /// </remarks>
    private static string CalculateInitials(string? text, bool isRtl, bool allowPhoneInitials)
    {
        if (text.HasNoValue()) return string.Empty;

        var source = text!.Trim();

        // An address is a name written for a machine: the local part is the part a person would have typed.
        var atIndex = source.IndexOf('@', StringComparison.Ordinal);
        if (atIndex > 0 && source.AsSpan().IndexOfAny(' ', '\t', '\n') < 0)
        {
            source = source[..atIndex].Replace('.', ' ').Replace('_', ' ').Replace('-', ' ').Replace('+', ' ');
        }

        var cleaned = CleanupInitialsSource(source);
        if (cleaned.Length == 0) return string.Empty;

        // A phone number, an order id, a row key: digits abbreviate into nothing a reader can recognize,
        // so the coin falls back to its icon instead of showing two arbitrary numerals - unless the caller
        // says the numbers are the name.
        if (allowPhoneInitials is false && cleaned.Any(char.IsLetter) is false) return string.Empty;

        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return string.Empty;

        var first = FirstTextElement(words[0]);
        var second = words.Length switch
        {
            2 => FirstTextElement(words[1]),
            3 => FirstTextElement(words[2]),
            _ => string.Empty
        };

        if (second.Length == 0) return first;

        // Writing the pair back to front is what puts it in reading order inside a right-to-left layout -
        // but only for initials that are themselves left-to-right, which the bidi algorithm lays out left
        // to right wherever they sit. Initials taken from a right-to-left name are already laid out from
        // the right, and reversing those would undo the very order it is here to produce.
        var reverse = isRtl && IsRtlText(first) is false && IsRtlText(second) is false;

        return reverse ? $"{second}{first}" : $"{first}{second}";
    }

    /// <summary>
    /// Whether a text element is written in a right-to-left script.
    /// </summary>
    /// <remarks>
    /// .NET does not publish the bidirectional class of a character, so the question is answered from the
    /// Unicode blocks that carry the right-to-left scripts: Hebrew through Arabic Extended-A, and the two
    /// Arabic presentation form blocks.
    /// </remarks>
    private static bool IsRtlText(string text)
    {
        foreach (var c in text)
        {
            // Hebrew through Arabic Extended-A.
            if (c is >= (char)0x0590 and <= (char)0x08FF) return true;
            // The two Arabic presentation form blocks.
            if (c is >= (char)0xFB1D and <= (char)0xFDFF) return true;
            if (c is >= (char)0xFE70 and <= (char)0xFEFF) return true;
        }

        return false;
    }

    /// <summary>
    /// Strips what a name carries but initials cannot use - parenthesized asides, punctuation and symbols -
    /// and collapses the runs of whitespace that are left into single spaces.
    /// </summary>
    private static string CleanupInitialsSource(string text)
    {
        var builder = new StringBuilder(text.Length);
        var depth = 0;
        var pendingSpace = false;

        foreach (var c in text)
        {
            if (c is '(' or '[' or '{')
            {
                depth++;
                continue;
            }

            if (c is ')' or ']' or '}')
            {
                if (depth > 0) depth--;
                continue;
            }

            if (depth > 0) continue;

            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            // Letters, digits, combining marks and both halves of a surrogate pair survive; the rest is noise.
            if (char.IsPunctuation(c) || char.IsSymbol(c)) continue;

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The first grapheme of a word, which is not the same thing as its first char: an emoji is two chars, and
    /// a letter carrying a combining mark is two more.
    /// </summary>
    private static string FirstTextElement(string word)
    {
        if (word.Length == 0) return string.Empty;

        return StringInfo.GetNextTextElement(word, 0);
    }

    /// <summary>
    /// The value of the width and height attributes of the coin image, which HTML takes as bare pixel counts.
    /// </summary>
    private string GetPersonaImageDimension()
    {
        if (CoinSize is > 0)
        {
            return CoinSize.Value.ToString(CultureInfo.InvariantCulture);
        }

        return Size switch
        {
            BitPersonaSize.Size8 => "8",
            BitPersonaSize.Size24 => "24",
            BitPersonaSize.Size32 => "32",
            BitPersonaSize.Size40 => "40",
            BitPersonaSize.Size48 => "48",
            BitPersonaSize.Size56 => "56",
            BitPersonaSize.Size72 => "72",
            BitPersonaSize.Size100 => "100",
            BitPersonaSize.Size120 => "120",
            _ => "48"
        };
    }

    private string? GetImageContainerClass()
    {
        // bit-prs-cne is on the coin whatever fills it, while bit-prs-imc is only on the coin the component
        // draws itself. Everything about the element as a control - the pointer, the focus ring, the overlay
        // it reveals - is hung off the first, so a coin filled by a template is as reachable as any other;
        // everything about how the coin looks stays on the second, which a template has taken over.
        var klass = $"bit-prs-cne {(CoinTemplate is null ? "bit-prs-imc" : null)} {GetCoinClass()} {Classes?.ImageContainer}".Trim();
        return klass.HasValue() ? klass : null;
    }

    private string? GetImageContainerStyle()
    {
        var style = $"{GetCoinSizeStyle()}{Styles?.ImageContainer?.Trim(';')}";
        return style.HasValue() ? style : null;
    }

    private string? GetCoinSizeStyle()
    {
        if (Size is BitPersonaSize.Size8) return null;
        if (CoinSize is null or <= 0) return null;

        // The initials are sized off the coin rather than off the type ramp, the same way the size classes do
        // it, so a custom coin size does not leave them at the size the size class happened to set.
        return FormattableString.Invariant($"width:{CoinSize.Value}px;height:{CoinSize.Value}px;font-size:{CoinSize.Value * 0.4:0.##}px;");
    }

    /// <summary>
    /// Whether the coin is something to operate rather than only something to look at.
    /// </summary>
    private bool HasInteractiveCoin => OnImageClick.HasDelegate || OnActionClick.HasDelegate;

    /// <summary>
    /// Whether there is a picture to put in the coin.
    /// </summary>
    /// <remarks>
    /// A set of candidates in <see cref="ImageSrcSet"/> is a picture as much as a single
    /// <see cref="ImageUrl"/> is - the img element needs only one of the two to have something to fetch.
    /// </remarks>
    private bool HasImage => ImageUrl.HasValue() || ImageSrcSet.HasValue();

    /// <summary>
    /// Whether the persona has no visible text of its own, in which case the coin is the whole of it and has
    /// to carry a name for anyone who cannot see it.
    /// </summary>
    /// <remarks>
    /// An interactive coin is excluded: role="img" would make everything inside the persona presentational,
    /// and the button in there would stop being reachable. Such a coin names itself instead.
    /// </remarks>
    private bool IsCoinOnly => HidePersonaDetails && Size is not BitPersonaSize.Size8 && HasInteractiveCoin is false;

    /// <summary>
    /// Whether there is anything for the overlay to say. A clickable coin that has been given no text and no
    /// template of its own has nothing to reveal, and a bare tinted veil over the picture says less than
    /// leaving it alone - such a coin is a plain button, not one that offers to change the picture.
    /// </summary>
    private bool HasImageOverlay => OnImageClick.HasDelegate && (ImageOverlayTemplate is not null || ImageOverlayText.HasValue());

    /// <summary>
    /// Whether the coin has to carry a name of its own, which is the case where the details are hidden and
    /// the root cannot claim the image role because an action button beside the coin has to stay reachable.
    /// </summary>
    private bool IsCoinNamed => HidePersonaDetails
                             && Size is not BitPersonaSize.Size8
                             && OnImageClick.HasDelegate is false
                             && OnActionClick.HasDelegate
                             && (AriaLabel.HasValue() || PrimaryText.HasValue());

    private string? GetCoinRole() => IsCoinNamed ? "img" : null;

    private string? GetCoinLabel() => IsCoinNamed ? (AriaLabel.HasValue() ? AriaLabel : PrimaryText) : null;

    /// <summary>
    /// The accessible name of a coin that is also a button. It says what activating it does, and - where the
    /// details are hidden and nothing else says so - who it belongs to.
    /// </summary>
    private string? GetCoinButtonLabel()
    {
        if (OnImageClick.HasDelegate is false) return null;

        // A label given by hand names the persona better than the name being displayed does, and is the only
        // thing left to call the coin after where there is no name being displayed at all.
        var name = AriaLabel.HasValue() ? AriaLabel : PrimaryText;

        // A template of one's own in the overlay is free to leave the text empty, and so is a coin that only
        // opens a profile rather than offering to change the picture - and a button with nothing to announce
        // is a button nobody can tell apart from the next one, so the name of the person is what is left to
        // call it after.
        if (ImageOverlayText.HasNoValue()) return name;

        if (HidePersonaDetails is false || name.HasNoValue()) return ImageOverlayText;

        return $"{name}, {ImageOverlayText}";
    }

    /// <summary>
    /// Whether anything is going to be shown in place of a picture, which is the only reason to work out what
    /// the initials are. A coin carrying a picture that has loaded never asks.
    /// </summary>
    private bool NeedsCoinFallback()
    {
        if (Size is BitPersonaSize.Size8) return false;
        if (Unknown) return false;
        if (CoinTemplate is not null) return false;

        if (HasImage is false) return true;

        return _hasError || (ShowInitialsUntilImageLoads && _isLoaded is false);
    }

    /// <summary>
    /// The role of a persona that shows nothing but its coin.
    /// </summary>
    /// <remarks>
    /// The role is only claimed where there is a name to go with it. An image role with nothing to announce
    /// is worse than no role at all: it makes everything inside the persona presentational and then has
    /// nothing of its own to put in their place.
    /// </remarks>
    private string? GetRootRole() => IsCoinOnly && GetRootAriaLabel().HasValue() ? "img" : null;

    /// <summary>
    /// The name of a persona that shows nothing but its coin. The presence dot is folded into it rather than
    /// left to name itself: role="img" on the coin makes everything inside it presentational, so a nested
    /// label would never be read.
    /// </summary>
    private string? GetRootAriaLabel()
    {
        if (AriaLabel.HasValue()) return AriaLabel;

        if (IsCoinOnly is false) return null;

        var presence = GetPresenceLabel();
        if (presence.HasNoValue()) return PrimaryText;

        return PrimaryText.HasValue() ? $"{PrimaryText}, {presence}" : presence;
    }

    private async Task HandleActionClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnActionClick.InvokeAsync(e);
    }

    private async Task HandleImageClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnImageClick.InvokeAsync(e);
    }

    private async Task HandleOnError(ErrorEventArgs e)
    {
        _hasError = true;
        _isLoaded = true;

        StateHasChanged();

        await OnImageError.InvokeAsync(e);
    }

    /// <remarks>
    /// The load is watched for the sake of <see cref="ShowInitialsUntilImageLoads"/> and of
    /// <see cref="OnImageLoad"/>, and for nothing else: the picture itself is never held back until this
    /// arrives. A statically rendered page has no handler attached to fire it at all, and a prerendered one
    /// can have the picture in the cache before it does - either of which would have left a coin hidden for
    /// good behind a load event that was never coming.
    /// </remarks>
    private async Task HandleOnLoad(ProgressEventArgs e)
    {
        _isLoaded = true;

        StateHasChanged();

        await OnImageLoad.InvokeAsync(e);
    }

    private void OnSetImageSource()
    {
        _hasError = false;
        _isLoaded = false;

        StateHasChanged();
    }
}
