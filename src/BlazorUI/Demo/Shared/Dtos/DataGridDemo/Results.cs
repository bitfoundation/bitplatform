namespace Bit.BlazorUI.Demo.Shared.Dtos.DataGridDemo;

public class Results
{
    [JsonPropertyName("skip")]
    public int Skip { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
