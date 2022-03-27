using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;
using FluentEmail.Core;
using TodoTemplate.Api.Resources;
using TodoTemplate.Api.Models.Emailing;
using Microsoft.AspNetCore.Hosting.Server;
using System.Web;

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
    
    private readonly IServer _server;

    public AuthController(SignInManager<User> signInManager,
        UserManager<User> userManager,
        IJwtService jwtService,
        IMapper mapper,
        IOptionsSnapshot<AppSettings> setting,
        IFluentEmail fluentEmail,
        IServer server
        )
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _mapper = mapper;
        _signInManager = signInManager;
        _appSettings = setting.Value;
        _fluentEmail = fluentEmail;
        _server = server;
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
                await SendConfirmationEmail(new() { Email = signUpRequest.Email }, cancellationToken);
                return;
            }
        }

        var userToAdd = _mapper.Map<User>(signUpRequest);

        var result = await _userManager.CreateAsync(userToAdd, signUpRequest.Password);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());

        await SendConfirmationEmail(new() { Email = userToAdd.Email }, cancellationToken);
    }

    [HttpPost("[action]")]
    public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(sendConfirmationEmailRequest.Email);

        if ((DateTimeOffset.Now - user.ConfirmationEmailRequestedOn) < _appSettings.IdentitySettings.ConfirmationEmailResendDelay)
            throw new TooManyRequestsExceptions(nameof(ErrorStrings.WaitForConfirmationEmailResendDelay));

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var controller = RouteData.Values["controller"]!.ToString();

        var confirmationLink = Url.Action(nameof(ConfirmEmail), controller,
            new { user.Email, token },
            HttpContext.Request.Scheme);

        var assembly = typeof(Program).Assembly;

        var result = await _fluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(EmailStrings.ConfirmationEmailSubject)
            .UsingTemplateFromEmbedded("TodoTemplate.Api.Resources.EmailConfirmation.cshtml",
                                    new EmailConfirmationModel
                                    {
                                        DisplayName = user.DisplayName,
                                        ConfirmationLink = confirmationLink
                                    },
                                    assembly)
            .SendAsync(cancellationToken);

        user.ConfirmationEmailRequestedOn = DateTimeOffset.Now;

        await _userManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.ToArray());
    }

    [HttpPost("[action]")]
    public async Task SendResetPasswordEmail(SendResetPasswordEmailRequestDto sendResetPasswordEmailRequest
        , CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(sendResetPasswordEmailRequest.Email);

        if ((DateTimeOffset.Now - user.ResetPasswordEmailRequestedOn) < _appSettings.IdentitySettings.ResetPasswordEmailResendDelay)
            throw new TooManyRequestsExceptions(nameof(ErrorStrings.WaitForResetPasswordEmailResendDelay));

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetPasswordLink = $"reset-password?email={user.Email}&token={HttpUtility.UrlEncode(token)}";

#if BlazorServer
        resetPasswordLink = $"{_appSettings.WebServerAddress}{resetPasswordLink}";
#else
        resetPasswordLink = $"{_server.GetHostUri()}{resetPasswordLink}";
#endif

        var assembly = typeof(Program).Assembly;

        var result = await _fluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(EmailStrings.ResetPasswordEmailSubject)
            .UsingTemplateFromEmbedded("TodoTemplate.Api.Resources.ResetPassword.cshtml",
                                    new ResetPasswordModel
                                    {
                                        DisplayName = user.DisplayName,
                                        ResetPasswordLink = resetPasswordLink
                                    },
                                    assembly)
            .SendAsync(cancellationToken);

        user.ResetPasswordEmailRequestedOn = DateTimeOffset.Now;

        await _userManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.ToArray());
    }

    [HttpGet("[action]")]
    public async Task<ActionResult> ConfirmEmail(string email, string token)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var emailConfirmed = user.EmailConfirmed || (await _userManager.ConfirmEmailAsync(user, token)).Succeeded;

        string url = $"email-confirmation?email={email}&email-confirmed={emailConfirmed}";

#if BlazorServer
        url = $"{_appSettings.WebServerAddress}{url}";
#else
        url = $"/{url}";
#endif

        return Redirect(url);
    }

    [HttpPost("[action]")]
    public async Task ResetPassword(ResetPasswordRequestDto resetPasswordRequest)
    {
        var user = await _userManager.FindByEmailAsync(resetPasswordRequest.Email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var result = await _userManager.ResetPasswordAsync(user, resetPasswordRequest.Token, resetPasswordRequest.Password);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());
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
