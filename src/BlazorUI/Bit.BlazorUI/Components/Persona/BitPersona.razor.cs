namespace Bit.BlazorUI;

public partial class BitPersona
{
    private bool _isLoaded;
    private bool _hasError;
    private string? imageUrl;
    private string? imageInitials;
    private BitPersonaSize size = BitPersonaSize.Size48;



    /// <summary>
    /// The title of the action button (tooltip).
    /// </summary>
    [Parameter] public string ActionButtonTitle { get; set; } = "Edit image";

    /// <summary>
    /// Icon name for the icon button of the custom action.
    /// </summary>
    [Parameter] public string? ActionIconName { get; set; }

    /// <summary>
    /// Optional Custom template for the custom action element.
    /// </summary>
    [Parameter] public RenderFragment? ActionTemplate { get; set; }

    /// <summary>
    /// Whether initials are calculated for phone numbers and number sequences.
    /// </summary>
    [Parameter] public bool AllowPhoneInitials { get; set; }

    /// <summary>
    /// Optional custom persona coin size in pixel.
    /// </summary>
    [Parameter] public int? CoinSize { get; set; }

    /// <summary>
    /// Custom persona coin's image template
    /// </summary>
    [Parameter] public RenderFragment? CoinTemplate { get; set; }

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
    [Parameter]
    public string? ImageInitials
    {
        get => imageInitials;
        set
        {
            if (imageInitials == value) return;

            imageInitials = value;
        }
    }

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
    [Parameter]
    public string? ImageUrl
    {
        get => imageUrl;
        set
        {
            if (imageUrl == value) return;

            imageUrl = value;
            _hasError = false;
        }
    }

    /// <summary>
    /// The background color when the user's initials are displayed.
    /// </summary>
    [Parameter] public BitPersonaInitialsColor? InitialsColor { get; set; }

    /// <summary>
    /// The text color when the user's initials are displayed.
    /// </summary>
    [Parameter] public BitPersonaInitialsColor? InitialsTextColor { get; set; }

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
    /// Custom optional text template
    /// </summary>
    [Parameter] public RenderFragment? OptionalTextTemplate { get; set; }

    /// <summary>
    /// Presence of the person to display - will not display presence if undefined.
    /// </summary>
    [Parameter] public BitPersonaPresenceStatus Presence { get; set; } = BitPersonaPresenceStatus.None;

    /// <summary>
    /// The icons to be used for the presence status.
    /// </summary>
    [Parameter] public Dictionary<BitPersonaPresenceStatus, string>? PresenceIcons { get; set; }

    /// <summary>
    /// Presence title to be shown as a tooltip on hover over the presence icon.
    /// </summary>
    [Parameter] public string? PresenceTitle { get; set; }

    /// <summary>
    /// Primary text to display, usually the name of the person.
    /// </summary>
    [Parameter] public string? PrimaryText { get; set; }

    /// <summary>
    /// Custom primary text template
    /// </summary>
    [Parameter] public RenderFragment? PrimaryTextTemplate { get; set; }

    /// <summary>
    /// Secondary text to display, usually the role of the user.
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// Custom secondary text template
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
    [Parameter] public bool ShowUnknownPersonaCoin { get; set; }

    /// <summary>
    /// Decides the size of the control.
    /// </summary>
    [Parameter]
    public BitPersonaSize Size
    {
        get => size;
        set
        {
            if (size == value) return;

            size = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Tertiary text to display, usually the status of the user.
    /// The tertiary text will only be shown when using size72 or size100.
    /// </summary>
    [Parameter] public string? TertiaryText { get; set; }

    /// <summary>
    /// Custom tertiary text template
    /// </summary>
    [Parameter] public RenderFragment? TertiaryTextTemplate { get; set; }



    protected override string RootElementClass => "bit-prs";

    protected override void RegisterCssClasses()
    {
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
    }

    private string? GetPresentationClass()
    {
        return Presence switch
        {
            BitPersonaPresenceStatus.Offline => "bit-prs-off",
            BitPersonaPresenceStatus.Online => "bit-prs-onl",
            BitPersonaPresenceStatus.Away => "bit-prs-awy",
            BitPersonaPresenceStatus.Dnd => "bit-prs-dnd",
            BitPersonaPresenceStatus.Blocked => "bit-prs-blk",
            BitPersonaPresenceStatus.Busy => "bit-prs-bsy",
            _ => null
        };
    }

    private string? GetPresentationStyle()
    {
        if (CoinSize.HasValue is false) return null;

        var presentationSize = CoinSize.Value / 3D;
        return $"width: {presentationSize}px; height: {presentationSize}px;";
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

    private string? GetCoinBackgroundColor()
    {
        if (ShowUnknownPersonaCoin) return null;
        if (Size is BitPersonaSize.Size8) return null;

        var initialsColor = InitialsColor ?? BitPersonaColorUtils.GetInitialsColorFromName(PrimaryText);
        var backgroundColor = BitPersonaColorUtils.GetPersonaColorHexCode(initialsColor);
        return $"background-color: {backgroundColor};";
    }

    private string? GetCoinWidth()
    {
        if (Size is BitPersonaSize.Size8) return null;
        if (CoinSize.HasValue is false) return null;

        return $"width: {CoinSize.Value}px;";
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
        };
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
}
