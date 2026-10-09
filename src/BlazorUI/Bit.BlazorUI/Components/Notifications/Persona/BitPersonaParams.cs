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
    public BitShape? Shape { get; set; }

    /// <summary>
    /// Puts the built-in glyph of each status in the presence dot, so the statuses differ by shape as well as color.
    /// <br />
    /// <see cref="BitPersona.ShowDefaultPresenceIcons"/>.
    /// </summary>
    public bool? ShowDefaultPresenceIcons { get; set; }

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

        if (ActionButtonTitle.HasValue())
        {
            bitPersona.TakeFromCascade(nameof(ActionButtonTitle), ActionButtonTitle!, static p => p.ActionButtonTitle, static (p, v) => p.ActionButtonTitle = v);
        }

        var ownActionIcon = bitPersona.HasSetAnyOf(nameof(ActionIcon), nameof(ActionIconName));

        if (ActionIcon is not null)
        {
            bitPersona.TakeFromCascade(nameof(ActionIcon), ActionIcon, static p => p.ActionIcon, static (p, v) => p.ActionIcon = v, outranked: ownActionIcon);
        }

        if (ActionIconName.HasValue())
        {
            bitPersona.TakeFromCascade(nameof(ActionIconName), ActionIconName, static p => p.ActionIconName, static (p, v) => p.ActionIconName = v, outranked: ownActionIcon);
        }

        if (ActiveAppearance.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(ActiveAppearance), ActiveAppearance.Value, static p => p.ActiveAppearance, static (p, v) => p.ActiveAppearance = v);
        }

        if (AllowPhoneInitials.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(AllowPhoneInitials), AllowPhoneInitials.Value, static p => p.AllowPhoneInitials, static (p, v) => p.AllowPhoneInitials = v);
        }

        if (AutoCoinColor.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(AutoCoinColor), AutoCoinColor.Value, static p => p.AutoCoinColor, static (p, v) => p.AutoCoinColor = v);
        }

        if (AutoCoinColors is not null)
        {
            bitPersona.TakeFromCascade(nameof(AutoCoinColors), AutoCoinColors, static p => p.AutoCoinColors, static (p, v) => p.AutoCoinColors = v);
        }

        if (Classes is not null)
        {
            bitPersona.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        // A persona that turns AutoCoinColor on itself is asking for the hashed color, which a CoinColor handed
        // down from here would otherwise win over.
        if (CoinColor.HasValue)
        {
            if ((bitPersona.HasNotBeenSet(nameof(AutoCoinColor)) || bitPersona.AutoCoinColor is false))
            {
                bitPersona.TakeFromCascade(nameof(CoinColor), CoinColor.Value, static p => p.CoinColor, static (p, v) => p.CoinColor = v);
            }
            else
            {
                bitPersona.ReleaseFromCascade(nameof(CoinColor));
            }
        }

        var ownCoinIcon = bitPersona.HasSetAnyOf(nameof(CoinIcon), nameof(CoinIconName));

        if (CoinIcon is not null)
        {
            bitPersona.TakeFromCascade(nameof(CoinIcon), CoinIcon, static p => p.CoinIcon, static (p, v) => p.CoinIcon = v, outranked: ownCoinIcon);
        }

        if (CoinIconName.HasValue())
        {
            bitPersona.TakeFromCascade(nameof(CoinIconName), CoinIconName, static p => p.CoinIconName, static (p, v) => p.CoinIconName = v, outranked: ownCoinIcon);
        }

        // A size class set on the persona itself is the size it asked for, and a CoinSize handed down from here
        // would otherwise override it.
        if (CoinSize.HasValue)
        {
            if (bitPersona.HasNotBeenSet(nameof(Size)))
            {
                bitPersona.TakeFromCascade(nameof(CoinSize), CoinSize.Value, static p => p.CoinSize, static (p, v) => p.CoinSize = v);
            }
            else
            {
                bitPersona.ReleaseFromCascade(nameof(CoinSize));
            }
        }

        if (CoinVariant.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(CoinVariant), CoinVariant.Value, static p => p.CoinVariant, static (p, v) => p.CoinVariant = v);
        }

        if (FullWidth.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static p => p.FullWidth, static (p, v) => p.FullWidth = v);
        }

        if (HidePersonaDetails.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(HidePersonaDetails), HidePersonaDetails.Value, static p => p.HidePersonaDetails, static (p, v) => p.HidePersonaDetails = v);
        }

        if (ImageFadeIn.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(ImageFadeIn), ImageFadeIn.Value, static p => p.ImageFadeIn, static (p, v) => p.ImageFadeIn = v);
        }

        if (ImageLoading.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(ImageLoading), ImageLoading.Value, static p => p.ImageLoading, static (p, v) => p.ImageLoading = v);
        }

        if (ImageOverlayText.HasValue())
        {
            bitPersona.TakeFromCascade(nameof(ImageOverlayText), ImageOverlayText!, static p => p.ImageOverlayText, static (p, v) => p.ImageOverlayText = v);
        }

        // The persona's own single-status glyph answers only where the maps have nothing to say, so a map handed
        // down from here would otherwise hide the glyph the persona set for itself.
        var hasOwnPresenceIcon = bitPersona.PresenceIcon is not null || bitPersona.PresenceIconName.HasValue();

        if (PresenceIcons is not null)
        {
            if (hasOwnPresenceIcon is false)
            {
                bitPersona.TakeFromCascade(nameof(PresenceIcons), PresenceIcons, static p => p.PresenceIcons, static (p, v) => p.PresenceIcons = v);
            }
            else
            {
                bitPersona.ReleaseFromCascade(nameof(PresenceIcons));
            }
        }

        if (PresenceIconNames is not null)
        {
            if (hasOwnPresenceIcon is false)
            {
                bitPersona.TakeFromCascade(nameof(PresenceIconNames), PresenceIconNames, static p => p.PresenceIconNames, static (p, v) => p.PresenceIconNames = v);
            }
            else
            {
                bitPersona.ReleaseFromCascade(nameof(PresenceIconNames));
            }
        }

        if (PresenceTitles is not null)
        {
            bitPersona.TakeFromCascade(nameof(PresenceTitles), PresenceTitles, static p => p.PresenceTitles, static (p, v) => p.PresenceTitles = v);
        }

        if (Rel.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(Rel), Rel.Value, static p => p.Rel, static (p, v) => p.Rel = v);
        }

        if (Reversed.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(Reversed), Reversed.Value, static p => p.Reversed, static (p, v) => p.Reversed = v);
        }

        // Shape wins over Squared, so a Shape handed down from here would otherwise override a persona that
        // squares itself.
        if (Shape.HasValue)
        {
            if ((bitPersona.HasNotBeenSet(nameof(Squared)) || bitPersona.Squared is false))
            {
                bitPersona.TakeFromCascade(nameof(Shape), Shape.Value, static p => p.Shape, static (p, v) => p.Shape = v);
            }
            else
            {
                bitPersona.ReleaseFromCascade(nameof(Shape));
            }
        }

        if (ShowDefaultPresenceIcons.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(ShowDefaultPresenceIcons), ShowDefaultPresenceIcons.Value, static p => p.ShowDefaultPresenceIcons, static (p, v) => p.ShowDefaultPresenceIcons = v);
        }

        if (ShowInitialsUntilImageLoads.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(ShowInitialsUntilImageLoads), ShowInitialsUntilImageLoads.Value, static p => p.ShowInitialsUntilImageLoads, static (p, v) => p.ShowInitialsUntilImageLoads = v);
        }

        if (ShowOverflowTooltip.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(ShowOverflowTooltip), ShowOverflowTooltip.Value, static p => p.ShowOverflowTooltip, static (p, v) => p.ShowOverflowTooltip = v);
        }

        if (ShowSecondaryText.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(ShowSecondaryText), ShowSecondaryText.Value, static p => p.ShowSecondaryText, static (p, v) => p.ShowSecondaryText = v);
        }

        if (Size.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(Size), Size.Value, static p => p.Size, static (p, v) => p.Size = v);
        }

        if (Squared.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(Squared), Squared.Value, static p => p.Squared, static (p, v) => p.Squared = v);
        }

        if (Styles is not null)
        {
            bitPersona.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }

        if (Target.HasValue())
        {
            bitPersona.TakeFromCascade(nameof(Target), Target, static p => p.Target, static (p, v) => p.Target = v);
        }

        var ownUnknownIcon = bitPersona.HasSetAnyOf(nameof(UnknownIcon), nameof(UnknownIconName));

        if (UnknownIcon is not null)
        {
            bitPersona.TakeFromCascade(nameof(UnknownIcon), UnknownIcon, static p => p.UnknownIcon, static (p, v) => p.UnknownIcon = v, outranked: ownUnknownIcon);
        }

        if (UnknownIconName.HasValue())
        {
            bitPersona.TakeFromCascade(nameof(UnknownIconName), UnknownIconName, static p => p.UnknownIconName, static (p, v) => p.UnknownIconName = v, outranked: ownUnknownIcon);
        }

        if (Vertical.HasValue)
        {
            bitPersona.TakeFromCascade(nameof(Vertical), Vertical.Value, static p => p.Vertical, static (p, v) => p.Vertical = v);
        }
    }
}
