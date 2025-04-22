//+:cnd:noEmit
namespace Boilerplate.Shared.Controllers;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IAttachmentController : IAppController
{
    [HttpDelete]
    Task DeleteUserProfilePicture(CancellationToken cancellationToken);

    //#if (module == "Sales" || module == "Admin")
    [HttpDelete("{productId}")]
    Task DeleteProductPrimaryImage(Guid productId, CancellationToken cancellationToken);
    //#endif
}
