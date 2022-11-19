using System.Text.Json.Serialization;

namespace Bit.BlazorUI.Playground.Shared.Dtos.DataGridDemo;

public class Meta
{
    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; }

    [JsonPropertyName("terms")]
    public string Terms { get; set; }

    [JsonPropertyName("license")]
    public string License { get; set; }

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; }

    [JsonPropertyName("results")]
    public Results Results { get; set; }
}
