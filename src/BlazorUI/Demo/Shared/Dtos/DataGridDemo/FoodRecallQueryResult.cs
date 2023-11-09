namespace Bit.BlazorUI.Demo.Shared.Dtos.DataGridDemo;

public class FoodRecallQueryResult
{
    [JsonPropertyName("meta")]
    public Meta? Meta { get; set; }

    [JsonPropertyName("results")]
    public List<FoodRecall>? Results { get; set; }
}
