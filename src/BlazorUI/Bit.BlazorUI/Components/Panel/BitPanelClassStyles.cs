﻿namespace Bit.BlazorUI;

public class BitPanelClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the panel container.
    /// </summary>
    public BitClassStylePair? Container { get; set; }
    /// <summary>
    /// Custom CSS classes/styles for the panel scroll container.
    /// </summary>
    public BitClassStylePair? ScrollContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the panel header.
    /// </summary>
    public BitClassStylePair? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the panel content.
    /// </summary>
    public BitClassStylePair? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the panel footer.
    /// </summary>
    public BitClassStylePair? Footer { get; set; }
}
