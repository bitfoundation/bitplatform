﻿
namespace Bit.BlazorUI;

public class BitMenuButtonItem
{
    /// <summary>
    /// Name of an icon to render next to the item text
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Text to render in the item
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// A unique value to use as a key of the item
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
