namespace Bit.BlazorUI.Demo.Shared.Dtos;

public class ComponentListDetailsDto
{
    public string Name { get; set; } = default!;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? AlsoKnownAs { get; set; }
    public string? Description { get; set; }
}
