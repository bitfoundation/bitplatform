namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public class Operation
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<Operation>? Clicked { get; set; }
}
