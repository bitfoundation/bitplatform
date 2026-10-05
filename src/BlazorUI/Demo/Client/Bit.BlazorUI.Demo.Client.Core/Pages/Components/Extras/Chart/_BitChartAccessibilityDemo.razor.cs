namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartAccessibilityDemo
{
    private bool _disabled;
    private string? _selected;

    private readonly BitChartData _data = new()
    {
        Labels = { "Q1", "Q2", "Q3", "Q4" },
        Datasets =
        {
            new BitChartDataset { Label = "North", Data = { 42, 51, 48, 60 } },
            new BitChartDataset { Label = "South", Data = { 35, 39, 46, 52 } }
        }
    };

    private void Select((int DatasetIndex, int DataIndex) element)
        => _selected = $"Selected: {_data.Datasets[element.DatasetIndex].Label}, {_data.Labels[element.DataIndex]}";


    private readonly string keyboardRazorCode = @"
<BitCheckbox Label=""Disabled"" @bind-Value=""_disabled"" />

<div>@(_selected ?? ""Press Enter on a bar"")</div>

<BitChart Type=""BitChartType.Bar""
          Data=""_data""
          Disabled=""_disabled""
          AriaLabel=""Quarterly revenue by region""
          Description=""North leads every quarter and both regions peak in Q4.""
          NavigationHint=""Use the arrow keys to compare regions and quarters, Enter to select one.""
          OnElementClick=""Select"" />";
    private readonly string keyboardCsharpCode = @"
private bool _disabled;
private string? _selected;

private readonly BitChartData _data = new()
{
    Labels = { ""Q1"", ""Q2"", ""Q3"", ""Q4"" },
    Datasets =
    {
        new BitChartDataset { Label = ""North"", Data = { 42, 51, 48, 60 } },
        new BitChartDataset { Label = ""South"", Data = { 35, 39, 46, 52 } }
    }
};

private void Select((int DatasetIndex, int DataIndex) element)
    => _selected = $""Selected: {_data.Datasets[element.DatasetIndex].Label}, {_data.Labels[element.DataIndex]}"";";
}
