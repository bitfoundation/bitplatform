namespace Bit.BlazorUI.Demo.Shared.Dtos;

public class EnumValueDetailsDto
{
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}
