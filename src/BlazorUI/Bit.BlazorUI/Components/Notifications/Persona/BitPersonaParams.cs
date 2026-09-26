namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPersona"/> component.
/// </summary>
/// <remarks>
/// It carries what a set of personas shares - their size, shape, coin coloring, layout and the localized presence
/// titles - and leaves what is one person's own (the texts, the picture, the presence, the callbacks) to each persona.
/// </remarks>
public class BitPersonaParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitPersona"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPersona value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitPersona)}";



    public string Name => ParamName;



    /// <summary>
    /// The title (tooltip and accessible name) of the action button.
    /// <br />
    /// <see cref="BitPersona.ActionButtonTitle"/>.
    /// </summary>
    public string? ActionButtonTitle { get; set; }

    /// <summary>
    /// The icon of the action button, taking precedence over <see cref="ActionIconName"/>.
    /// <br />
    /// <see cref="BitPersona.ActionIcon"/>.
    /// </summary>
    public BitIconInfo? ActionIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon of the action button.
    /// <br />
    /// <see cref="BitPersona.ActionIconName"/>.
    /// </summary>
    public string? ActionIconName { get; set; }

    /// <summary>
    /// How the coin is decorated while the persona is active.
    /// <br />
    /// <see cref="BitPersona.ActiveAppearance"/>.
    /// </summary>
    public BitPersonaActiveAppearance? ActiveAppearance { get; set; }

    /// <summary>
    /// Whether initials are derived from names that carry no letters at all.
    /// <br />
    /// <see cref="BitPersona.AllowPhoneInitials"/>.
    /// </summary>
    public bool? AllowPhoneInitials { get; set; }

    /// <summary>
    /// Picks the coin color by a stable hash of the person's identity when no CoinColor is set.
    /// <br />
    /// <see cref="BitPersona.AutoCoinColor"/>.
    /// </summary>
    public bool? AutoCoinColor { get; set; }

    /// <summary>
    /// The colors <see cref="AutoCoinColor"/> is allowed to pick from, in place of the built-in set.
    /// <br />
    /// <see cref="BitPersona.AutoCoinColors"/>.
    /// </summary>
    public IEnumerable<BitColor>? AutoCoinColors { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the persona.
    /// <br />
    /// <see cref="BitPersona.Classes"/>.
    /// </summary>
    public BitPersonaClassStyles? Classes { get; set; }

    /// <summary>
    /// The background color of the coin when there is no picture in it.
    /// <br />
    /// <see cref="BitPersona.CoinColor"/>.
    /// </summary>
    public BitColor? CoinColor { get; set; }

    /// <summary>
    /// The icon rendered inside the coin in place of the initials, taking precedence over <see cref="CoinIconName"/>.
    /// <br />
    /// <see cref="BitPersona.CoinIcon"/>.
    /// </summary>
    public BitIconInfo? CoinIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon rendered inside the coin in place of the initials.
    /// <br />
    /// <see cref="BitPersona.CoinIconName"/>.
    /// </summary>
    public string? CoinIconName { get; set; }

    /// <summary>
    /// A custom coin size in pixels.
    /// <br />
    /// <see cref="BitPersona.CoinSize"/>.
    /// </summary>
    public int? CoinSize { get; set; }

    /// <summary>
    /// The variant of the coin.
    /// <br />
    /// <see cref="BitPersona.CoinVariant"/>.
    /// </summary>
    public BitVariant? CoinVariant { get; set; }

    /// <summary>
    /// Renders the persona in the full width of its container.
    /// <br />
    /// <see cref="BitPersona.FullWidth"/>.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Renders the coin alone, without the details beside it.
    /// <br />
    /// <see cref="BitPersona.HidePersonaDetails"/>.
    /// </summary>
    public bool? HidePersonaDetails { get; set; }

    /// <summary>
    /// Fades the picture in as it is painted.
    /// <br />
    /// <see cref="BitPersona.ImageFadeIn"/>.
    /// </summary>
    public bool? ImageFadeIn { get; set; }

    /// <summary>
    /// The loading behavior of the picture (lazy or eager).
    /// <br />
    /// <see cref="BitPersona.ImageLoading"/>.
    /// </summary>
    public BitImageLoading? ImageLoading { get; set; }

    /// <summary>
    /// The text of the overlay a clickable coin reveals.
    /// <br />
    /// <see cref="BitPersona.ImageOverlayText"/>.
    /// </summary>
    public string? ImageOverlayText { get; set; }

    /// <summary>
    /// The icons rendered inside the presence dot, one per status.
    /// <br />
    /// <see cref="BitPersona.PresenceIcons"/>.
    /// </summary>
    public Dictionary<BitPersonaPresence, BitIconInfo>? PresenceIcons { get; set; }

    /// <summary>
    /// The names of the built-in icons rendered inside the presence dot, one per status.
    /// <br />
    /// <see cref="BitPersona.PresenceIconNames"/>.
    /// </summary>
    public Dictionary<BitPersonaPresence, string>? PresenceIconNames { get; set; }

    /// <summary>
    /// The tooltips and accessible names of the presence dot, one per status - the way to localize them for a whole view.
    /// <br />
    /// <see cref="BitPersona.PresenceTitles"/>.
    /// </summary>
    public Dictionary<BitPersonaPresence, string>? PresenceTitles { get; set; }

    /// <summary>
    /// The rel attribute of the link a coin with an Href renders.
    /// <br />
    /// <see cref="BitPersona.Rel"/>.
    /// </summary>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Puts the coin after the details instead of before them.
    /// <br />
    /// <see cref="BitPersona.Reversed"/>.
    /// </summary>
    public bool? Reversed { get; set; }

    /// <summary>
    /// The outline of the coin.
    /// <br />
    /// <see cref="BitPersona.Shape"/>.
    /// </summary>
    public BitPersonaShape? Shape { get; set; }

    /// <summary>
    /// Renders the initials while the picture is loading.
    /// <br />
    /// <see cref="BitPersona.ShowInitialsUntilImageLoads"/>.
    /// </summary>
    public bool? ShowInitialsUntilImageLoads { get; set; }

    /// <summary>
    /// Whether each detail row carries itself as a native tooltip.
    /// <br />
    /// <see cref="BitPersona.ShowOverflowTooltip"/>.
    /// </summary>
    public bool? ShowOverflowTooltip { get; set; }

    /// <summary>
    /// Shows the secondary text at every size, including the small ones.
    /// <br />
    /// <see cref="BitPersona.ShowSecondaryText"/>.
    /// </summary>
    public bool? ShowSecondaryText { get; set; }

    /// <summary>
    /// The size of the persona.
    /// <br />
    /// <see cref="BitPersona.Size"/>.
    /// </summary>
    public BitPersonaSize? Size { get; set; }

    /// <summary>
    /// Renders the coin as a rounded square.
    /// <br />
    /// <see cref="BitPersona.Squared"/>.
    /// </summary>
    public bool? Squared { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the persona.
    /// <br />
    /// <see cref="BitPersona.Styles"/>.
    /// </summary>
    public BitPersonaClassStyles? Styles { get; set; }

    /// <summary>
    /// The target attribute of the link a coin with an Href renders.
    /// <br />
    /// <see cref="BitPersona.Target"/>.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The icon of the unknown coin, taking precedence over <see cref="UnknownIconName"/>.
    /// <br />
    /// <see cref="BitPersona.UnknownIcon"/>.
    /// </summary>
    public BitIconInfo? UnknownIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon of the unknown coin.
    /// <br />
    /// <see cref="BitPersona.UnknownIconName"/>.
    /// </summary>
    public string? UnknownIconName { get; set; }

    /// <summary>
    /// Stacks the coin over the details and centers both.
    /// <br />
    /// <see cref="BitPersona.Vertical"/>.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPersona"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitPersona"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPersona"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitPersona"/>.
    /// </remarks>
    /// <param name="bitPersona">
    /// The <see cref="BitPersona"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPersona bitPersona)
    {
        if (bitPersona is null) return;

        UpdateBaseParameters(bitPersona);

        var linkWasSet = false;

        if (ActionButtonTitle.HasValue() && bitPersona.HasNotBeenSet(nameof(ActionButtonTitle)))
        {
            bitPersona.ActionButtonTitle = ActionButtonTitle!;
        }

        if (ActionIcon is not null && bitPersona.HasNotBeenSet(nameof(ActionIcon)))
        {
            bitPersona.ActionIcon = ActionIcon;
        }

        if (ActionIconName.HasValue() && bitPersona.HasNotBeenSet(nameof(ActionIconName)))
        {
            bitPersona.ActionIconName = ActionIconName;
        }

        if (ActiveAppearance.HasValue && bitPersona.HasNotBeenSet(nameof(ActiveAppearance)))
        {
            bitPersona.ActiveAppearance = ActiveAppearance.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (AllowPhoneInitials.HasValue && bitPersona.HasNotBeenSet(nameof(AllowPhoneInitials)))
        {
            bitPersona.AllowPhoneInitials = AllowPhoneInitials.Value;
        }

        if (AutoCoinColor.HasValue && bitPersona.HasNotBeenSet(nameof(AutoCoinColor)))
        {
            bitPersona.AutoCoinColor = AutoCoinColor.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (AutoCoinColors is not null && bitPersona.HasNotBeenSet(nameof(AutoCoinColors)))
        {
            bitPersona.AutoCoinColors = AutoCoinColors;

            bitPersona.ClassBuilder.Reset();
        }

        if (Classes is not null && bitPersona.HasNotBeenSet(nameof(Classes)))
        {
            bitPersona.Classes = Classes;

            bitPersona.ClassBuilder.Reset();
        }

        if (CoinColor.HasValue && bitPersona.HasNotBeenSet(nameof(CoinColor)))
        {
            bitPersona.CoinColor = CoinColor.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (CoinIcon is not null && bitPersona.HasNotBeenSet(nameof(CoinIcon)))
        {
            bitPersona.CoinIcon = CoinIcon;
        }

        if (CoinIconName.HasValue() && bitPersona.HasNotBeenSet(nameof(CoinIconName)))
        {
            bitPersona.CoinIconName = CoinIconName;
        }

        if (CoinSize.HasValue && bitPersona.HasNotBeenSet(nameof(CoinSize)))
        {
            bitPersona.CoinSize = CoinSize.Value;
        }

        if (CoinVariant.HasValue && bitPersona.HasNotBeenSet(nameof(CoinVariant)))
        {
            bitPersona.CoinVariant = CoinVariant.Value;
        }

        if (FullWidth.HasValue && bitPersona.HasNotBeenSet(nameof(FullWidth)))
        {
            bitPersona.FullWidth = FullWidth.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (HidePersonaDetails.HasValue && bitPersona.HasNotBeenSet(nameof(HidePersonaDetails)))
        {
            bitPersona.HidePersonaDetails = HidePersonaDetails.Value;
        }

        if (ImageFadeIn.HasValue && bitPersona.HasNotBeenSet(nameof(ImageFadeIn)))
        {
            bitPersona.ImageFadeIn = ImageFadeIn.Value;
        }

        if (ImageLoading.HasValue && bitPersona.HasNotBeenSet(nameof(ImageLoading)))
        {
            bitPersona.ImageLoading = ImageLoading.Value;
        }

        if (ImageOverlayText.HasValue() && bitPersona.HasNotBeenSet(nameof(ImageOverlayText)))
        {
            bitPersona.ImageOverlayText = ImageOverlayText!;
        }

        if (PresenceIcons is not null && bitPersona.HasNotBeenSet(nameof(PresenceIcons)))
        {
            bitPersona.PresenceIcons = PresenceIcons;
        }

        if (PresenceIconNames is not null && bitPersona.HasNotBeenSet(nameof(PresenceIconNames)))
        {
            bitPersona.PresenceIconNames = PresenceIconNames;
        }

        if (PresenceTitles is not null && bitPersona.HasNotBeenSet(nameof(PresenceTitles)))
        {
            bitPersona.PresenceTitles = PresenceTitles;
        }

        if (Rel.HasValue && bitPersona.HasNotBeenSet(nameof(Rel)))
        {
            bitPersona.Rel = Rel.Value;

            linkWasSet = true;
        }

        if (Reversed.HasValue && bitPersona.HasNotBeenSet(nameof(Reversed)))
        {
            bitPersona.Reversed = Reversed.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (Shape.HasValue && bitPersona.HasNotBeenSet(nameof(Shape)))
        {
            bitPersona.Shape = Shape.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (ShowInitialsUntilImageLoads.HasValue && bitPersona.HasNotBeenSet(nameof(ShowInitialsUntilImageLoads)))
        {
            bitPersona.ShowInitialsUntilImageLoads = ShowInitialsUntilImageLoads.Value;
        }

        if (ShowOverflowTooltip.HasValue && bitPersona.HasNotBeenSet(nameof(ShowOverflowTooltip)))
        {
            bitPersona.ShowOverflowTooltip = ShowOverflowTooltip.Value;
        }

        if (ShowSecondaryText.HasValue && bitPersona.HasNotBeenSet(nameof(ShowSecondaryText)))
        {
            bitPersona.ShowSecondaryText = ShowSecondaryText.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (Size.HasValue && bitPersona.HasNotBeenSet(nameof(Size)))
        {
            bitPersona.Size = Size.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (Squared.HasValue && bitPersona.HasNotBeenSet(nameof(Squared)))
        {
            bitPersona.Squared = Squared.Value;

            bitPersona.ClassBuilder.Reset();
        }

        if (Styles is not null && bitPersona.HasNotBeenSet(nameof(Styles)))
        {
            bitPersona.Styles = Styles;

            bitPersona.ClassBuilder.Reset();

            bitPersona.StyleBuilder.Reset();
        }

        if (Target.HasValue() && bitPersona.HasNotBeenSet(nameof(Target)))
        {
            bitPersona.Target = Target;

            linkWasSet = true;
        }

        if (UnknownIcon is not null && bitPersona.HasNotBeenSet(nameof(UnknownIcon)))
        {
            bitPersona.UnknownIcon = UnknownIcon;
        }

        if (UnknownIconName.HasValue() && bitPersona.HasNotBeenSet(nameof(UnknownIconName)))
        {
            bitPersona.UnknownIconName = UnknownIconName;
        }

        if (Vertical.HasValue && bitPersona.HasNotBeenSet(nameof(Vertical)))
        {
            bitPersona.Vertical = Vertical.Value;

            bitPersona.ClassBuilder.Reset();
        }

        // The rel attribute is worked out when Href, Rel or Target is set on the persona itself, which a value
        // handed down from here does not trigger.
        if (linkWasSet)
        {
            bitPersona.OnSetHrefAndRel();
        }
    }
}
