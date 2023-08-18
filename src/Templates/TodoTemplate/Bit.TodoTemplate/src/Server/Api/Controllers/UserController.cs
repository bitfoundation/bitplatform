using TodoTemplate.Server.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Server.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class UserController : AppControllerBase
{
    [AutoInject] private UserManager<User> _userManager = default!;

    [HttpGet]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = UserInformationProvider.GetUserId();

        var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        return user.Map();
    }

    [HttpPut]
    public async Task Update(EditUserDto userDto, CancellationToken cancellationToken)
    {
        var userId = UserInformationProvider.GetUserId();

        var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        userDto.Patch(user);

        await _userManager.UpdateAsync(user);
    }

    [HttpDelete]
    public async Task Delete(CancellationToken cancellationToken)
    {
        var userId = UserInformationProvider.GetUserId();

        var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
                    ?? throw new ResourceNotFoundException();

        await _userManager.DeleteAsync(user);
    }
}
