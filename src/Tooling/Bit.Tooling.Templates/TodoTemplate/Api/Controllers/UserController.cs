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
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await Get(cancellationToken).FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserCouldNotBeFound));

        return user;
    }

    [HttpPost("[action]"), AllowAnonymous]
    public async Task<UserDto> SignUp(UserDto dto, CancellationToken cancellationToken)
    {
        var userToAdd = _mapper.Map<User>(dto);

        List<IdentityResult> results = new();

        results.Add(await _userManager.CreateAsync(userToAdd, dto.Password));

        if (results.Last().Succeeded)
            results.Add(await _userManager.AddClaimAsync(userToAdd, new Claim(ClaimTypes.Name, userToAdd.UserName!)));

        if (results.Last().Succeeded)
            results.Add(await _userManager.AddClaimAsync(userToAdd, new Claim(ClaimTypes.NameIdentifier, userToAdd.Id.ToString()!)));

        if (!results.All(r => r.Succeeded))
            throw new ResourceValidationException(results.SelectMany(r => r.Errors).Select(e => $"{e.Code}: {e.Description}").ToArray());

        return await Get(cancellationToken).FirstAsync(u => u.Id == userToAdd.Id, cancellationToken);
    }

    [HttpPut]
    public async Task<UserDto> Update(UserDto dto, CancellationToken cancellationToken)
    {
        var userToUpdate = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == dto.Id, cancellationToken);

        if (userToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserCouldNotBeFound));

        var updatedUser = _mapper.Map(dto, userToUpdate);

        await _userManager.UpdateAsync(updatedUser);

        return await Get(cancellationToken).FirstAsync(u => u.Id == updatedUser.Id, cancellationToken);
    }

    [HttpPost("[action]"), AllowAnonymous]
    public async Task<SignInResponseDto> SignIn(SignInRequestDto requestToken)
    {
        var user = await _userManager.FindByNameAsync(requestToken.UserName);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.InvalidUserNameAndOrPassword));

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, requestToken.Password);

        if (!isPasswordValid)
            throw new BadRequestException(nameof(ErrorStrings.InvalidUserNameAndOrPassword));

        return await _jwtService.GenerateToken(requestToken);
    }
}
