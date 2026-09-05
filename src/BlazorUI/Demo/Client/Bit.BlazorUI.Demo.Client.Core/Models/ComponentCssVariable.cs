namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// One of the public CSS custom properties a component reads off its root, documented on its demo page.
/// </summary>
public class ComponentCssVariable
{
    public string? Name { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}
