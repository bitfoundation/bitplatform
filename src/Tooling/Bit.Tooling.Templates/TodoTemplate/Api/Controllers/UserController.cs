using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    private readonly IJwtService _jwtService;

    private readonly IMapper _mapper;

    private readonly SignInManager<User> _signInManager;

    public UserController(SignInManager<User> signInManager, UserManager<User> userManager, IJwtService jwtService, IMapper mapper)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _mapper = mapper;
        _signInManager = signInManager;
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
    public async Task SignUp(UserDto dto, CancellationToken cancellationToken)
    {
        var userToAdd = _mapper.Map<User>(dto);

        var result = await _userManager.CreateAsync(userToAdd, dto.Password);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => $"{e.Code}: {e.Description}").ToArray());
    }

    [HttpPut]
    public async Task Update(UserDto dto, CancellationToken cancellationToken)
    {
        var userToUpdate = await _userManager.Users.FirstOrDefaultAsync(user => user.Id == dto.Id, cancellationToken);

        if (userToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserCouldNotBeFound));

        var updatedUser = _mapper.Map(dto, userToUpdate);

        await _userManager.UpdateAsync(updatedUser);
    }

    [HttpPost("[action]"), AllowAnonymous]
    public async Task<SignInResponseDto> SignIn(SignInRequestDto requestToken)
    {
        var user = await _userManager.FindByNameAsync(requestToken.UserName);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.InvalidUserNameAndOrPassword));

        var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, requestToken.Password, lockoutOnFailure: true);

        if (checkPasswordResult.IsLockedOut)
            throw new BadRequestException(nameof(ErrorStrings.AccountIsLockedOut));

        if (!checkPasswordResult.Succeeded)
            throw new BadRequestException(nameof(ErrorStrings.InvalidUserNameAndOrPassword));

        return await _jwtService.GenerateToken(user);
    }
}
