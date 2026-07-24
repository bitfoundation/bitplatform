namespace Bit.BlazorUI.Demo.Shared.Dtos.QuickGridDemo;

public class FoodRecallQueryResult
{
    [JsonPropertyName("meta")]
    public Meta? Meta { get; set; }

    [JsonPropertyName("results")]
    public List<FoodRecall>? Results { get; set; }
}
