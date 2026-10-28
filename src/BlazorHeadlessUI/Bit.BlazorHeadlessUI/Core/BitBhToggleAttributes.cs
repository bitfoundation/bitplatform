namespace Bit.BlazorHeadlessUI;

/// <summary>Shared ARIA wiring for disclosure-style triggers and panels.</summary>
internal static class BitBhToggleAttributes
{
    public static void ApplyTrigger(Dictionary<string, object> attrs, string tag, bool isOpen, bool disabled, string? contentId)
    {
        var isButton = tag.Equals("button", StringComparison.OrdinalIgnoreCase);
        if (isButton)
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["aria-expanded"] = isOpen ? "true" : "false";
        if (contentId is not null)
            attrs["aria-controls"] = contentId;
    }

    /// <summary>Panel attributes: hidden / hidden-until-found, presence, trigger link for find-in-page.</summary>
    public static void ApplyPanel(Dictionary<string, object> attrs, bool isOpen, bool hiddenUntilFound, string? triggerId)
    {
        attrs["data-bit-bh-presence"] = "";
        if (!isOpen)
            attrs["hidden"] = hiddenUntilFound ? "until-found" : true;
        if (hiddenUntilFound && triggerId is not null)
            attrs["data-bit-bh-trigger"] = triggerId;
    }
}
