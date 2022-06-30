using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class UserController : ControllerBase
{
    [AutoInject] private readonly UserManager<User> _userManager;

    [AutoInject] private readonly IMapper _mapper;

    [HttpGet("[action]")]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        return _mapper.Map<User, UserDto>(user);
    }

    [HttpPut]
    public async Task Update(EditUserDto userDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId != User.GetUserId())
            throw new UnauthorizedException();

        var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        var updatedUser = _mapper.Map(userDto, user);

        await _userManager.UpdateAsync(updatedUser);
    }
}
