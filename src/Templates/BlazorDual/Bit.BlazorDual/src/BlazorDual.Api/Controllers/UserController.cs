using BlazorDual.Api.Models.Account;
using BlazorDual.Shared.Dtos.Account;

namespace BlazorDual.Api.Controllers;

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

        return Mapper.Map<User, UserDto>(user);
    }

    [HttpPut]
    public async Task Update(EditUserDto userDto, CancellationToken cancellationToken)
    {
        var userId = UserInformationProvider.GetUserId();

        var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        var updatedUser = Mapper.Map(userDto, user);

        await _userManager.UpdateAsync(updatedUser);
    }
}
