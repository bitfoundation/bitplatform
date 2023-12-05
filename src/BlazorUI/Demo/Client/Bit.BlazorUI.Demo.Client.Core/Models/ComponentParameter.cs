namespace Bit.BlazorUI.Demo.Client.Core.Models;

public class ComponentParameter
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public LinkType LinkType { get; set; }
    public string? Href { get; set; }
}
