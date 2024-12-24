namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public class Operation
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool ReversedIcon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<Operation>? Clicked { get; set; }

    public string? OnIcon { get; set; }

    public string? OffIcon { get; set; }

    public string? OnName { get; set; }

    public string? OffName { get; set; }

    public string? OnTitle { get; set; }

    public string? OffTitle { get; set; }
}
