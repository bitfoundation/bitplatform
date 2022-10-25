using System;
using System.Drawing;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI;

public class BitChoiceGroupOption
{
    /// <summary>
    /// AriaLabel attribute for the GroupOption Option input.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Whether or not the GroupOption Option is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The icon to show as Option content.
    /// </summary>
    public BitIconName? IconName { get; set; }

    /// <summary>
    /// The image address to show as Option content.
    /// </summary>
    public string? ImageSrc { get; set; }

    /// <summary>
    /// Provides alternative information for the Option image.
    /// </summary>
    public string? ImageAlt { get; set; }

    /// <summary>
    /// Provides Height and Width for the Option image.
    /// </summary>
    public Size? ImageSize { get; set; }

    /// <summary>
    /// Set attribute of Id for the GroupOption Option input.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Set attribute of Id for the GroupOption Option label.
    /// </summary>
    public string? LabelId { get; set; }

    /// <summary>
    /// Provides a new image for the selected Option in the Image-GroupOption.
    /// </summary>
    public string? SelectedImageSrc { get; set; }

    /// <summary>
    /// Text to show as content of GroupOption Option.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// This value is returned when GroupOption Option is Clicked.
    /// </summary>
    public string? Value { get; set; }
}
