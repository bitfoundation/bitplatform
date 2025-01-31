namespace Boilerplate.Shared.Controllers;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IAttachmentController : IAppController
{
    [HttpDelete]
    Task RemoveProfileImage(CancellationToken cancellationToken);
}
