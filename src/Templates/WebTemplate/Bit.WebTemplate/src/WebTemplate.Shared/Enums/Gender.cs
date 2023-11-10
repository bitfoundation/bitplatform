namespace WebTemplate.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<Gender>))]
public enum Gender
{
    Male,
    Female,
    Other
}
