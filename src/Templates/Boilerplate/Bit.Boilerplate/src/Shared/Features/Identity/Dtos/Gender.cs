namespace Boilerplate.Shared.Features.Identity.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter<Gender>))]
public enum Gender
{
    Other,
    Male,
    Female,
}
