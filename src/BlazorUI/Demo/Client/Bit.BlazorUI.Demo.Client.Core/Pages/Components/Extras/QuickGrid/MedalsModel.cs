namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.QuickGrid;

public class MedalsModel
{
    public int Gold { get; set; }
    public int Silver { get; set; }
    public int Bronze { get; set; }

    public int Total => Gold + Silver + Bronze;
}
