
namespace Bit.BlazorUI;

public class BitSplitButtonItem
{
    /// <summary>
    /// 
    /// </summary>
    public BitIconName? IconName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public RenderFragment<BitSplitButtonItem>? ItemTemplate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool DefaultIsSelected { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
