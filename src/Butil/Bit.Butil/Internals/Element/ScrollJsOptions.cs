using System.Text.Json.Serialization;

namespace Bit.Butil;

internal class ScrollJsOptions
{
    public string Behavior { get; set; } = default!;

    // An unset offset has to be left out rather than sent as null: the browser reads a present
    // null as 0 and scrolls that axis back to its origin instead of leaving it where it is.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Top { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Left { get; set; }
}
