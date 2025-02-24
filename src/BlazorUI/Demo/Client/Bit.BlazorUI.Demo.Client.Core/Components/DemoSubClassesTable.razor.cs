namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoSubClassesTable
{
    [Parameter] public List<ComponentSubClass> Classes { get; set; } = new();
}
