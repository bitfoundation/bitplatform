//+:cnd:noEmit
//#if (module == "Sales" || module == "Admin")
using Boilerplate.Shared.Dtos.Products;
//#endif

namespace Boilerplate.Shared.Controllers;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IAttachmentController : IAppController
{
    [HttpDelete]
    Task RemoveProfileImage(CancellationToken cancellationToken);

    //#if (module == "Sales" || module == "Admin")
    [HttpDelete("{id}")]
    Task<ProductDto> RemoveProductImage(Guid id, CancellationToken cancellationToken);
    //#endif
}
