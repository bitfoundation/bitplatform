//-:cnd:noEmit
using System.Web;
using Microsoft.AspNetCore.Hosting.Server;
using FluentEmail.Core;
using TodoTemplate.Api.Resources;
using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;
using TodoTemplate.Api.Models.Emailing;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController, AllowAnonymous]
public partial class AuthController : ControllerBase
{
    [AutoInject] private UserManager<User> _userManager = default!;

    [AutoInject] private IJwtService _jwtService = default!;

    [AutoInject] private IMapper _mapper = default!;

    [AutoInject] private SignInManager<User> _signInManager = default!;

    [AutoInject] private IOptionsSnapshot<AppSettings> _appSettings = default!;

    [AutoInject] private IFluentEmail _fluentEmail = default!;

    [HttpPost("[action]")]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByNameAsync(signUpRequest.UserName);

        var userToAdd = _mapper.Map<User>(signUpRequest);

        if (existingUser is not null)
        {
            if (await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                throw new BadRequestException(nameof(ErrorStrings.DuplicateEmail));
            }
            else
            {
                await _userManager.DeleteAsync(existingUser);
                userToAdd.ConfirmationEmailRequestedOn = existingUser.ConfirmationEmailRequestedOn;
            }
        }

        var result = await _userManager.CreateAsync(userToAdd, signUpRequest.Password);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());
        }

        await SendConfirmationEmail(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
    }

    [HttpPost("[action]")]
    public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(sendConfirmationEmailRequest.Email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        if (await _userManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(nameof(ErrorStrings.EmailAlreadyConfirmed));

        await SendConfirmationEmail(sendConfirmationEmailRequest, user, cancellationToken);
    }

    private async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, User user, CancellationToken cancellationToken)
    {
        if ((DateTimeOffset.Now - user.ConfirmationEmailRequestedOn) < _appSettings.Value.IdentitySettings.ConfirmationEmailResendDelay)
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
                    ConfirmationLink = confirmationLink,
                    HostUri = new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}")
                },assembly)
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

        if ((DateTimeOffset.Now - user.ResetPasswordEmailRequestedOn) < _appSettings.Value.IdentitySettings.ResetPasswordEmailResendDelay)
            throw new TooManyRequestsExceptions(nameof(ErrorStrings.WaitForResetPasswordEmailResendDelay));

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetPasswordLink = $"reset-password?email={user.Email}&token={HttpUtility.UrlEncode(token)}";

#if BlazorServer
        resetPasswordLink = $"{_appSettings.Value.WebServerAddress}{resetPasswordLink}";
#else
        resetPasswordLink = $"{new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}")}{resetPasswordLink}";
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
        url = $"{_appSettings.Value.WebServerAddress}{url}";
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
