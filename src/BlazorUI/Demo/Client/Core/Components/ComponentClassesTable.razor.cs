namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class ComponentClassesTable
{
    [Parameter] public List<ComponentSubClass> Classes { get; set; } = new();
}
