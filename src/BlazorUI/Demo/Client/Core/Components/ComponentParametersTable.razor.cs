namespace Bit.BlazorUI.Demo.Client.Core.Components;
public partial class ComponentParametersTable
{
    [Parameter] public string? Name { get; set; }

    [Parameter] public List<ComponentParameter> Parameters { get; set; } = new();
}
