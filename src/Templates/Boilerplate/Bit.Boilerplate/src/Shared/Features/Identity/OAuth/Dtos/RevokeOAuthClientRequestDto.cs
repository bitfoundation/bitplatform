//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Identity.OAuth.Dtos;

/// <summary>A body rather than a route segment, because a client id can be a url hundreds of characters long.</summary>
[DtoResourceType(typeof(AppStrings))]
public partial class RevokeOAuthClientRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? ClientId { get; set; }
}
