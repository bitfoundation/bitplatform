using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    private readonly IJwtService _jwtService;

    private readonly IMapper _mapper;

    public UserController(UserManager<User> userManager, IJwtService jwtService, IMapper mapper)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    [HttpGet, EnableQuery]
    public IQueryable<UserDto> Get(CancellationToken cancellationToken)
    {
        return _userManager.Users.ProjectTo<UserDto>(_mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("[action]")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await Get(cancellationToken).FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost("[action]"), AllowAnonymous]
    public async Task<ActionResult<UserDto>> SignUp(UserDto dto, CancellationToken cancellationToken)
    {
        var userToAdd = _mapper.Map<User>(dto);

        var isSuceess = (await _userManager.CreateAsync(userToAdd, dto.Password)).Succeeded;

        isSuceess = isSuceess && (await _userManager.AddClaimAsync(userToAdd, new Claim(ClaimTypes.Name, userToAdd.UserName!))).Succeeded;
        isSuceess = isSuceess && (await _userManager.AddClaimAsync(userToAdd, new Claim(ClaimTypes.NameIdentifier, userToAdd.Id.ToString()!))).Succeeded;

        if (!isSuceess)
            return BadRequest();

        return Ok(await Get(cancellationToken).FirstOrDefaultAsync(u => u.Id == userToAdd.Id, cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<UserDto>> Update(UserDto dto, CancellationToken cancellationToken)
    {
        var userToUpdate = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == dto.Id, cancellationToken);

        if (userToUpdate is null)
            return NotFound();

        var updatedUser = _mapper.Map(dto, userToUpdate);

        await _userManager.UpdateAsync(updatedUser);

        return Ok(await Get(cancellationToken).FirstOrDefaultAsync(u => u.Id == updatedUser.Id, cancellationToken));
    }

    [HttpPost("[action]"), AllowAnonymous]
    public async Task<ActionResult<SignInResponseDto>> SignIn(SignInRequestDto requestToken)
    {
        var user = await _userManager.FindByNameAsync(requestToken.UserName);

        if (user is null)
            return BadRequest();

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, requestToken.Password);

        if (!isPasswordValid)
            return BadRequest();

        return Ok(await _jwtService.GenerateToken(requestToken));
    }
}
