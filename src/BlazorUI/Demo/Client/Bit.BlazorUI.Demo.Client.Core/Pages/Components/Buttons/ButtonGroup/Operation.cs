namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public class Operation
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Image { get; set; }

    public BitIconInfo? IconInfo { get; set; }

    public bool ReversedIcon { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public Action<Operation>? Clicked { get; set; }

    public string? OnImage { get; set; }

    public BitIconInfo? OnIconInfo { get; set; }

    public string? OffImage { get; set; }

    public BitIconInfo? OffIconInfo { get; set; }

    public string? OnName { get; set; }

    public string? OffName { get; set; }

    public string? OnTitle { get; set; }

    public string? OffTitle { get; set; }

    public bool IsSelected { get; set; }
}
