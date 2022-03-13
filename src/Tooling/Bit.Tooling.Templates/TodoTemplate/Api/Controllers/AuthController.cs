using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController, AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    private readonly IJwtService _jwtService;

    private readonly IMapper _mapper;

    private readonly SignInManager<User> _signInManager;

    public AuthController(SignInManager<User> signInManager, UserManager<User> userManager, IJwtService jwtService, IMapper mapper)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _mapper = mapper;
        _signInManager = signInManager;
    }

    [HttpPost("[action]"),]
    public async Task SignUp(UserDto dto)
    {
        var userToAdd = _mapper.Map<User>(dto);

        var result = await _userManager.CreateAsync(userToAdd, dto.Password);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());
    }

    [HttpPost("[action]")]
    public async Task<SignInResponseDto> SignIn(SignInRequestDto requestToken)
    {
        var user = await _userManager.FindByNameAsync(requestToken.UserName);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, requestToken.Password, lockoutOnFailure: true);

        if (checkPasswordResult.IsLockedOut)
            throw new BadRequestException(nameof(ErrorStrings.UserLockedOut));

        if (!checkPasswordResult.Succeeded)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        return await _jwtService.GenerateToken(user);
    }
}
