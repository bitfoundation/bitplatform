namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoSubEnumsTable
{
    [Parameter] public List<ComponentSubEnum> Enums { get; set; } = new();
}
