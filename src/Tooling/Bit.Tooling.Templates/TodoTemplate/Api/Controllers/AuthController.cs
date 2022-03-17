using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;
using FluentEmail.Core;
using TodoTemplate.Api.Resources;
using TodoTemplate.Api.Models.Emailing;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController, AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    private readonly IJwtService _jwtService;

    private readonly IMapper _mapper;

    private readonly SignInManager<User> _signInManager;

    private readonly AppSettings _appSettings;

    private readonly IFluentEmail _fluentEmail;

    public AuthController(SignInManager<User> signInManager,
        UserManager<User> userManager,
        IJwtService jwtService,
        IMapper mapper,
        IOptionsSnapshot<AppSettings> setting,
        IFluentEmail fluentEmail
        )
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _mapper = mapper;
        _signInManager = signInManager;
        _appSettings = setting.Value;
        _fluentEmail = fluentEmail;
    }

    [HttpPost("[action]")]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        var existingUserIfAny = await _userManager.FindByNameAsync(signUpRequest.UserName);

        if (existingUserIfAny is not null)
        {
            if (await _userManager.IsEmailConfirmedAsync(existingUserIfAny))
            {
                throw new BadRequestException(nameof(ErrorStrings.DuplicateEmail));
            }
            else
            {
                await _userManager.DeleteAsync(existingUserIfAny);
            }
        }

        var userToAdd = _mapper.Map<User>(signUpRequest);

        var result = await _userManager.CreateAsync(userToAdd, signUpRequest.Password);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());

        await SendEmailConfirmLink(new() { Email = userToAdd.Email }, cancellationToken);
    }

    [HttpPost("[action]")]
    public async Task SendEmailConfirmLink(SendEmailConfirmLinkRequestDto sendConfirmLinkEmailRequest, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(sendConfirmLinkEmailRequest.Email);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var controller = RouteData.Values["controller"]!.ToString();

        var confirmationLink = Url.Action(nameof(ConfirmEmail), controller,
            new { user.Email, token },
            HttpContext.Request.Scheme);

        var assembly = typeof(Program).Assembly;

        var result = await _fluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(EmailStrings.ConfirmationLinkEmailSubject)
            .UsingTemplateFromEmbedded("TodoTemplate.Api.Resources.EmailConfirmation.cshtml",
                                    new EmailConfirmationModel
                                    {
                                        DisplayName = user.DisplayName,
                                        ConfirmationLink = confirmationLink
                                    },
                                    assembly)
            .SendAsync(cancellationToken);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.ToArray());
    }

    [HttpGet("[action]")]
    public async Task<ActionResult> ConfirmEmail(string email, string token)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        if (user.EmailConfirmed)
            throw new BadRequestException();

        var result = await _userManager.ConfirmEmailAsync(user, token);

        string url = $"email-confirmation?email={email}&email-confirmed={result.Succeeded}";

#if BlazorServer
        url = $"{_appSettings.WebServerAddress}{url}";
#endif

        return Redirect(url);
    }

    [HttpPost("[action]")]
    public async Task<SignInResponseDto> SignIn(SignInRequestDto signInRequest)
    {
        var user = await _userManager.FindByNameAsync(signInRequest.UserName);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, signInRequest.Password, lockoutOnFailure: true);

        if (checkPasswordResult.IsLockedOut)
            throw new BadRequestException(nameof(ErrorStrings.UserLockedOut));

        if (!checkPasswordResult.Succeeded)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        return await _jwtService.GenerateToken(user);
    }
}
