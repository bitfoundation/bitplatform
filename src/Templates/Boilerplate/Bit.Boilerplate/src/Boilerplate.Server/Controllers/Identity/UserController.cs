using Boilerplate.Shared;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Server.Models.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Server.Controllers.Identity;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class UserController : AppControllerBase, IUserController
{
    [AutoInject] private UserManager<User> userManager = default!;


    [HttpGet]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ResourceNotFoundException();

        return user.Map();
    }

    [HttpPut]
    public async Task<UserDto> Update(EditUserDto userDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ResourceNotFoundException();

        userDto.Patch(user);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        return await GetCurrentUser(cancellationToken);
    }

    [HttpDelete]
    public async Task Delete(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }
}
