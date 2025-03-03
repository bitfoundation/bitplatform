//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Shared.Controllers;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IAttachmentController : IAppController
{
    [HttpDelete]
    Task RemoveProfileImage(CancellationToken cancellationToken);

    //#if (module == "Sales" || modile == "Admin")
    [HttpDelete("{id}")]
    Task<ProductDto> RemoveProductImage(Guid id, CancellationToken cancellationToken);
    //#endif
}
