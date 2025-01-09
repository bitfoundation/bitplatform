namespace Bit.BlazorUI;

/// <summary>
/// A BitPersona is a visual representation of a person across products, typically showcasing the image that person has chosen to upload themselves. The control can also be used to show that person's online status.
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
    /// Icon name for the icon button of the custom action.
    /// </summary>
    [Parameter] public string? ActionIconName { get; set; } = "Edit";

    /// <summary>
    /// Optional Custom template for the custom action element.
    /// </summary>
    [Parameter] public RenderFragment? ActionTemplate { get; set; }

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
    /// The shape of the coin.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPersonaCoinShape? CoinShape { get; set; }

    /// <summary>
    /// Optional custom persona coin size in pixel.
    /// </summary>
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
    [Parameter] public string? ImageAlt { get; set; }

    /// <summary>
    /// The user's initials to display in the image area when there is no image.
    /// </summary>
    [Parameter] public string? ImageInitials { get; set; }

    /// <summary>
    /// Optional Custom template for the image overlay.
    /// </summary>
    [Parameter] public RenderFragment? ImageOverlayTemplate { get; set; }

    /// <summary>
    /// The text of the image overlay.
    /// </summary>
    [Parameter] public string ImageOverlayText { get; set; } = "Edit image";

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
    [Parameter] public EventCallback<MouseEventArgs> OnImageClick { get; set; }

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
    /// The icons to be used for the presence status.
    /// </summary>
    [Parameter] public Dictionary<BitPersonaPresence, string>? PresenceIcons { get; set; }

    /// <summary>
    /// Presence title to be shown as a tooltip on hover over the presence icon.
    /// </summary>
    [Parameter] public string? PresenceTitle { get; set; }

    /// <summary>
    /// Primary text to display, usually the name of the person.
    /// </summary>
    [Parameter] public string? PrimaryText { get; set; }

    /// <summary>
    /// Custom primary text template.
    /// </summary>
    [Parameter] public RenderFragment? PrimaryTextTemplate { get; set; }

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
    /// If true, show the special coin for unknown persona. 
    /// It has '?' in place of initials, with static font and background colors.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Unknown { get; set; }

    /// <summary>
    /// Decides the size of the control.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitPersonaSize Size { get; set; } = BitPersonaSize.Size48;

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



    protected override string RootElementClass => "bit-prs";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullWidth ? "bit-prs-fwi" : string.Empty);

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
            _ => "bit-prs-inf"
        });

        ClassBuilder.Register(() => CoinShape switch
        {
            BitPersonaCoinShape.Circular => "bit-prs-crl",
            BitPersonaCoinShape.Square => "bit-prs-sqr",
            _ => "bit-prs-crl"
        });
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
            _ => null
        };
    }

    private string? GetPresentationStyle()
    {
        if (CoinSize is null) return null;

        string? position = null;
        var presentationSize = CoinSize.Value / 3D;
        if (CoinShape == BitPersonaCoinShape.Square)
        {
            var presentationPosition = presentationSize / 3D;
            position = $"right:-{presentationPosition}px;bottom:-{presentationPosition}px;";
        }
        return $"width:{presentationSize}px;height:{presentationSize}px;{position}{Styles?.Presence?.Trim(';')}";
    }

    private string? GetPresentationIcon()
    {
        if (Size is BitPersonaSize.Size8 or BitPersonaSize.Size24 or BitPersonaSize.Size32) return null;

        if (PresenceIcons?.ContainsKey(Presence) ?? false)
        {
            return PresenceIcons[Presence];
        }

        return null;
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

    private string? GetCoinWidthStyle()
    {
        if (Size is BitPersonaSize.Size8) return null;
        if (CoinSize is null) return null;

        return $"width:{CoinSize.Value}px;";
    }

    private string GetInitials()
    {
        if (ImageInitials.HasValue()) return ImageInitials!;

        if (PrimaryText.HasNoValue()) return string.Empty;

        var text = PrimaryText!.Trim();
        var splits = text.Split(' ');

        string initials = string.Empty;
        if (splits.Length == 2)
        {
            initials = $"{splits[0][0]}{splits[1][0]}";
        }
        else if (splits.Length == 3)
        {
            initials = $"{splits[0][0]}{splits[2][0]}";
        }
        else if (splits.Length != 0)
        {
            initials = $"{splits[0][0]}";
        }

        if (Dir == BitDir.Rtl && initials.Length > 1)
        {
            return $"{initials[1]}{initials[0]}";
        }

        return initials;
    }

    private string GetPersonaImageDimension()
    {
        if (CoinSize.HasValue)
        {
            return $"{CoinSize.Value}px";
        }

        return Size switch
        {
            BitPersonaSize.Size8 => "8px",
            BitPersonaSize.Size24 => "24px",
            BitPersonaSize.Size32 => "32px",
            BitPersonaSize.Size40 => "40px",
            BitPersonaSize.Size48 => "48px",
            BitPersonaSize.Size56 => "56px",
            BitPersonaSize.Size72 => "72px",
            BitPersonaSize.Size100 => "100px",
            BitPersonaSize.Size120 => "120px",
            _ => "48px"
        };
    }

    private string? GetImageContainerClass()
    {
        var klass = $"{(CoinTemplate is null ? "bit-prs-imc" : null)} {GetCoinClass()} {Classes?.ImageContainer}".Trim();
        return klass.HasValue() ? klass : null;
    }

    private string? GetImageContainerStyle()
    {
        var coinWidthStyle = GetCoinWidthStyle();
        var style = $"{coinWidthStyle}{Styles?.ImageContainer?.Trim(';')}";
        return style.HasValue() ? style : null;
    }

    private async Task HandleActionClick(MouseEventArgs e)
    {
        await OnActionClick.InvokeAsync(e);
    }

    private async Task HandleImageClick(MouseEventArgs e)
    {
        await OnImageClick.InvokeAsync(e);
    }

    private void HandleOnError(Microsoft.AspNetCore.Components.Web.ErrorEventArgs e)
    {
        _hasError = true;
        _isLoaded = true;
        StateHasChanged();
    }

    private void HandleOnLoad(ProgressEventArgs e)
    {
        _isLoaded = true;
        StateHasChanged();
    }

    private void OnSetImageUrl()
    {
        _hasError = false;
    }
}
