namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

public class ComponentSubClass
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<ComponentParameter> Parameters { get; set; } = new();
}
