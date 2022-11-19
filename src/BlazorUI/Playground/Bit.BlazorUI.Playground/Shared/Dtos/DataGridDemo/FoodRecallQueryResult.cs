using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI.Playground.Shared.Dtos.DataGridDemo;

public class FoodRecallQueryResult
{
    [JsonPropertyName("meta")]
    public Meta Meta { get; set; }

    [JsonPropertyName("results")]
    public List<FoodRecall> Results { get; set; }
}
