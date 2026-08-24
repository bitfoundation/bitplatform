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
    [Parameter] public string? ImageSrcSet { get; set; }

    /// <summary>
    /// Url to the image to use, should be a square aspect ratio and big enough to fit in the image area.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetImageUrl))]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Callback for the persona custom action.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnActionClick { get; set; }

    /// <summary>
    /// Callback for when the image clicked.
    /// </summary>
    /// <remarks>
    /// Assigning it turns the coin into a button: it takes the focus, and Enter and Space activate it the
    /// way a click does.
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
    /// The optional text will only be shown when using size100.
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
    /// If true renders the initials while the image is loading. This only applies when an imageUrl is provided.
    /// </summary>
    [Parameter] public bool ShowInitialsUntilImageLoads { get; set; }

    /// <summary>
    /// Decides the size of the control.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPersonaSize Size { get; set; } = BitPersonaSize.Size48;

    /// <summary>
    /// If true, renders the coin with a square shape instead of the default circular shape.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Squared { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitPersona component.
    /// </summary>
    [Parameter] public BitPersonaClassStyles? Styles { get; set; }

    /// <summary>
    /// Tertiary text to display, usually the status of the user.
    /// The tertiary text will only be shown when using size72 or size100.
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

        ClassBuilder.Register(() => ImageUrl.HasValue() ? "bit-prs-him" : string.Empty);

        ClassBuilder.Register(() => Size is BitPersonaSize.Size8 ? string.Empty : CoinColor switch
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
            null when AutoCoinColor => GetAutoCoinColorClass(),
            _ => "bit-prs-inf"
        });

        ClassBuilder.Register(() => Squared ? "bit-prs-sqr" : null);

        ClassBuilder.Register(() => (Active && Size is not BitPersonaSize.Size8) ? ActiveAppearance switch
        {
            BitPersonaActiveAppearance.Shadow => "bit-prs-ash",
            BitPersonaActiveAppearance.RingShadow => "bit-prs-ars",
            _ => "bit-prs-arg"
        } : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



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

        string? position = null;
        // The dot keeps the same share of the coin a size class gives it, so a custom coin size stays in proportion.
        var presentationSize = CoinSize.Value / 4D;
        if (Squared)
        {
            var presentationPosition = presentationSize / 3D;
            position = FormattableString.Invariant($"inset-inline-end:-{presentationPosition}px;bottom:-{presentationPosition}px;");
        }
        return FormattableString.Invariant($"width:{presentationSize}px;height:{presentationSize}px;{position}{Styles?.Presence?.Trim(';')}");
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

        return null;
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

    private static readonly string[] _autoCoinColorClasses = ["bit-prs-pri", "bit-prs-sec", "bit-prs-ter", "bit-prs-suc", "bit-prs-wrn", "bit-prs-err", "bit-prs-inf"];

    private string GetAutoCoinColorClass()
    {
        var text = (CoinColorSeed.HasValue() ? CoinColorSeed
                  : ImageInitials.HasValue() ? ImageInitials
                  : PrimaryText)?.Trim() ?? string.Empty;
        if (text.HasNoValue()) return "bit-prs-inf";

        // Stable DJB2 hash - not affected by .NET's randomized GetHashCode
        uint hash = 5381;
        foreach (var c in text)
        {
            hash = ((hash << 5) + hash) + c;
        }

        return _autoCoinColorClasses[hash % (uint)_autoCoinColorClasses.Length];
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

    private string GetInitials()
    {
        if (ImageInitials.HasValue()) return ImageInitials!;

        return CalculateInitials(PrimaryText, Dir == BitDir.Rtl);
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
    private static string CalculateInitials(string? text, bool isRtl)
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
        // so the coin falls back to its icon instead of showing two arbitrary numerals.
        if (cleaned.Any(char.IsLetter) is false) return string.Empty;

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

        return isRtl ? $"{second}{first}" : $"{first}{second}";
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
        var klass = $"{(CoinTemplate is null ? "bit-prs-imc" : null)} {GetCoinClass()} {Classes?.ImageContainer}".Trim();
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
    /// Whether the persona has no visible text of its own, in which case the coin is the whole of it and has
    /// to carry a name for anyone who cannot see it.
    /// </summary>
    /// <remarks>
    /// An interactive coin is excluded: role="img" would make everything inside the persona presentational,
    /// and the button in there would stop being reachable. Such a coin names itself instead.
    /// </remarks>
    private bool IsCoinOnly => HidePersonaDetails && Size is not BitPersonaSize.Size8 && HasInteractiveCoin is false;

    /// <summary>
    /// The accessible name of a coin that is also a button. It says what activating it does, and - where the
    /// details are hidden and nothing else says so - who it belongs to.
    /// </summary>
    private string? GetCoinButtonLabel()
    {
        if (OnImageClick.HasDelegate is false) return null;

        if (HidePersonaDetails is false || PrimaryText.HasNoValue()) return ImageOverlayText;

        return $"{PrimaryText}, {ImageOverlayText}";
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

        if (ImageUrl.HasNoValue()) return true;

        return _hasError || (ShowInitialsUntilImageLoads && _isLoaded is false);
    }

    private string? GetRootRole() => IsCoinOnly ? "img" : null;

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

    /// <summary>
    /// Enter and Space on the focused coin do what a click does, which is what turning a div into a button owes
    /// the keyboard.
    /// </summary>
    private async Task HandleImageKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (OnImageClick.HasDelegate is false) return;
        if (e.Key is not ("Enter" or " " or "Spacebar")) return;

        await OnImageClick.InvokeAsync(new MouseEventArgs());
    }

    private async Task HandleOnError(ErrorEventArgs e)
    {
        _hasError = true;
        _isLoaded = true;

        StateHasChanged();

        await OnImageError.InvokeAsync(e);
    }

    private async Task HandleOnLoad(ProgressEventArgs e)
    {
        _isLoaded = true;

        StateHasChanged();

        await OnImageLoad.InvokeAsync(e);
    }

    private void OnSetImageUrl()
    {
        _hasError = false;
        _isLoaded = false;

        StateHasChanged();
    }
}
