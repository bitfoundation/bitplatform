namespace TodoTemplate.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public class SignInResponseDto
{
    public string? AccessToken { get; set; }

    public long ExpiresIn { get; set; }
}
