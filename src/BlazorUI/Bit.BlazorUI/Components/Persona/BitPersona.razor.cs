﻿using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

public partial class BitPersona
{
    private readonly Regex MULTIPLE_WHITESPACES_REGEX = new(@"\s+");
    //private readonly Regex PHONE_NUMBER_REGEX = new(@"^\d+[\d\s]*(:?ext|x|)\s*\d+$");
    private readonly Regex UNWANTED_CHARS_REGEX = new(@"\([^)]*\)|[\0-\u001F\!-/:-@\[-`\{-\u00BF\u0250-\u036F\uD800-\uFFFF]");
    private readonly Regex UNSUPPORTED_TEXT_REGEX = new(@"[\u1100-\u11FF\u3130-\u318F\uA960-\uA97F\uAC00-\uD7AF\uD7B0-\uD7FF\u3040-\u309F\u30A0-\u30FF\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]|[\uD840-\uD869][\uDC00-\uDED6]");

    private const int PRESENCE_MAX_SIZE = 40;
    private const int COIN_SIZE_PRESENCE_SCALE_FACTOR = 3;

    private string? size;
    private string? imageUrl;

    private bool _isLoaded;
    private bool _hasError;
    private bool _shouldRenderIcon;
    private string? _iconStyle = string.Empty;
    private string? _presenceStyle = string.Empty;
    private string _internalInitials = string.Empty;
    private string? _presenceFontSize = string.Empty;
    private string? _presenceHeightWidth = string.Empty;

    /// <summary>
    /// Whether to not render persona details, and just render the persona image/initials.
    /// </summary>
    [Parameter] public bool HidePersonaDetails { get; set; }

    /// <summary>
    /// This flag can be used to signal the persona is out of office. This will change the way the presence icon looks for statuses that support dual-presence.
    /// </summary>
    [Parameter] public bool IsOutOfOffice { get; set; }

    /// <summary>
    /// If true renders the initials while the image is loading. This only applies when an imageUrl is provided.
    /// </summary>
    [Parameter] public bool ShowInitialsUntilImageLoads { get; set; }

    /// <summary>
    /// Optional custom persona coin size in pixel.
    /// </summary>
    [Parameter] public int CoinSize { get; set; } = -1;

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
    /// Presence title to be shown as a tooltip on hover over the presence icon.
    /// </summary>
    [Parameter] public string? PresenceTitle { get; set; }

    /// <summary>
    /// Decides the size of the control.
    /// </summary>
    [Parameter]
    public string? Size
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
    /// Alt text for the image to use. default is empty string.
    /// </summary>
    [Parameter] public string? ImageAlt { get; set; }

    /// <summary>
    /// The background color when the user's initials are displayed.
    /// </summary>
    [Parameter] public BitPersonaInitialsColor? InitialsColor { get; set; }

    /// <summary>
    /// The user's initials to display in the image area when there is no image.
    /// </summary>
    [Parameter] public string? ImageInitials { get; set; }

    /// <summary>
    /// Primary text to display, usually the name of the person.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Secondary text to display, usually the role of the user.
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// Tertiary text to display, usually the status of the user.
    /// The tertiary text will only be shown when using size72 or size100.
    /// </summary>
    [Parameter] public string? TertiaryText { get; set; }

    /// <summary>
    /// Optional text to display, usually a custom message set.
    /// The optional text will only be shown when using size100.
    /// </summary>
    [Parameter] public string? OptionalText { get; set; }

    /// <summary>
    /// If true, show the special coin for unknown persona. 
    /// It has '?' in place of initials, with static font and background colors.
    /// </summary>
    [Parameter] public bool ShowUnknownPersonaCoin { get; set; }

    /// <summary>
    /// Presence of the person to display - will not display presence if undefined.
    /// </summary>
    [Parameter] public BitPersonaPresenceStatus Presence { get; set; } = BitPersonaPresenceStatus.None;

    /// <summary>
    /// Whether initials are calculated for phone numbers and number sequences.
    /// </summary>
    [Parameter] public bool AllowPhoneInitials { get; set; }

    /// <summary>
    /// Icon name for the icon button of the custom action.
    /// </summary>
    [Parameter] public string? ActionIconName { get; set; }

    /// <summary>
    /// Callback for the persona custom action.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnActionClick { get; set; }

    /// <summary>
    /// Optional Custom template for the custom action element.
    /// </summary>
    [Parameter] public RenderFragment? ActionFragment { get; set; }

    /// <summary>
    /// Callback for when the image clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnImageClick { get; set; }

    /// <summary>
    /// Optional Custom template for the image overlay.
    /// </summary>
    [Parameter] public RenderFragment? ImageOverlayFragment { get; set; }


    protected override string RootElementClass => "bit-prs";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Size.HasValue() ? $"{RootElementClass}-{Size}" : string.Empty);

        ClassBuilder.Register(() => OnImageClick.HasDelegate ? $"{RootElementClass}-img-act" : string.Empty);

        ClassBuilder.Register(() => Presence is not BitPersonaPresenceStatus.None
                                        ? $"{RootElementClass}-{Presence.ToString().ToLowerInvariant()}"
                                        : string.Empty);
    }

    protected override Task OnParametersSetAsync()
    {
        if (CoinSize != -1)
        {
            _presenceHeightWidth = CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR < PRESENCE_MAX_SIZE
                ? CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR + "px"
                : PRESENCE_MAX_SIZE + "px";

            _presenceFontSize = CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR < PRESENCE_MAX_SIZE
                ? CoinSize / COIN_SIZE_PRESENCE_SCALE_FACTOR + "px"
                : PRESENCE_MAX_SIZE + "px";
        }

        _shouldRenderIcon = (Size != BitPersonaSize.Size20 && Size != BitPersonaSize.Size24 && Size != BitPersonaSize.Size32) && (CoinSize == -1 || CoinSize > 32);

        _internalInitials = ImageInitials ?? GetInitials();

        return base.OnParametersSetAsync();
    }

    private string DetermineIcon()
    {
        if (Presence == BitPersonaPresenceStatus.None) return string.Empty;

        return Presence switch
        {
            BitPersonaPresenceStatus.Online => "presence_available",
            BitPersonaPresenceStatus.Busy => "presence_busy",
            BitPersonaPresenceStatus.Away => IsOutOfOffice ? "presence_oof" : "presence_away",
            BitPersonaPresenceStatus.Dnd => "presence_dnd",
            BitPersonaPresenceStatus.Offline => IsOutOfOffice ? "presence_oof" : "presence_offline",
            _ => "presence_unknown",
        };
    }

    private string GetCoinBackgroundColor()
    {
        return InitialsColor is not null ? BitPersonaColorUtils.GetPersonaColorHexCode(InitialsColor.Value) : BitPersonaColorUtils.GetPersonaColorHexCode(BitPersonaColorUtils.GetInitialsColorFromName(Text));
    }


    protected string GetInitials()
    {
        if (string.IsNullOrWhiteSpace(Text)) return "";

        var text = Text.Trim();
        text = UNWANTED_CHARS_REGEX.Replace(text, "");
        text = MULTIPLE_WHITESPACES_REGEX.Replace(text, " ");

        if (UNSUPPORTED_TEXT_REGEX.IsMatch(text)) return "";
        //if (AllowPhoneInitials && PHONE_NUMBER_REGEX.IsMatch(text) is false) return "";

        var splits = text.Split(' ');

        if (splits.Length == 2)
        {
            return FormattableString.Invariant($"{splits[0][0]}{splits[1][0]}");
        }

        if (splits.Length == 3)
        {
            return FormattableString.Invariant($"{splits[0][0]}{splits[2][0]}");
        }

        if (splits.Length != 0)
        {
            return FormattableString.Invariant($"{splits[0][0]}");
        }

        return string.Empty;
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
