namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class ComponentEnumsTable
{
    [Parameter] public List<ComponentSubEnum> Enums { get; set; } = new();
}
