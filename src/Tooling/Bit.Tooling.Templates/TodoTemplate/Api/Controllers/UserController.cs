using Bit.Tooling.SourceGenerators;
using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class UserController : ControllerBase
{
    [AutoInject] public UserManager<User> UserManager { get; set; }

    [AutoInject] public IMapper Mapper { get; set; }

    [HttpGet("[action]")]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await UserManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        return Mapper.Map<User, UserDto>(user);
    }

    [HttpPut]
    public async Task Update(EditUserDto userDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await UserManager.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException();

        var updatedUser = Mapper.Map(userDto, user);

        await UserManager.UpdateAsync(updatedUser);
    }
}
