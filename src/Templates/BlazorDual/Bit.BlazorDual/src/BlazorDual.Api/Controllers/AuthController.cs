//-:cnd:noEmit
using System.Web;
using FluentEmail.Core;
using BlazorDual.Api.Resources;
using BlazorDual.Api.Models.Account;
using BlazorDual.Shared.Dtos.Account;
using BlazorDual.Api.Models.Emailing;

namespace BlazorDual.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class AuthController : AppControllerBase
{
    [AutoInject] private UserManager<User> _userManager = default!;

    [AutoInject] private IJwtService _jwtService = default!;

    [AutoInject] private SignInManager<User> _signInManager = default!;

    [AutoInject] private IFluentEmail _fluentEmail = default!;

    [AutoInject] private IStringLocalizer<EmailStrings> _emailLocalizer = default!;

    [HttpPost]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByNameAsync(signUpRequest.UserName);

        var userToAdd = Mapper.Map<User>(signUpRequest);

        if (existingUser is not null)
        {
            if (await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                throw new BadRequestException(Localizer.GetString(nameof(AppStrings.DuplicateEmail), existingUser.Email));
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
            throw new ResourceValidationException(result.Errors.Select(e => Localizer.GetString(e.Code, signUpRequest.UserName!)).ToArray());
        }

        await SendConfirmationEmail(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
    }

    [HttpPost]
    public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(sendConfirmationEmailRequest.Email);

        if (user is null)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), sendConfirmationEmailRequest.Email!));

        if (await _userManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)]);

        await SendConfirmationEmail(sendConfirmationEmailRequest, user, cancellationToken);
    }

    private async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.ConfirmationEmailRequestedOn) - AppSettings.IdentitySettings.ConfirmationEmailResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForConfirmationEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var controller = RouteData.Values["controller"]!.ToString();

        var confirmationLink = Url.Action(nameof(ConfirmEmail), controller,
            new { user.Email, token },
            HttpContext.Request.Scheme);

        var assembly = typeof(Program).Assembly;

        var result = await _fluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(_emailLocalizer[EmailStrings.ConfirmationEmailSubject])
            .UsingTemplateFromEmbedded("BlazorDual.Api.Resources.EmailConfirmation.cshtml",
                new EmailConfirmationModel
                {
                    ConfirmationLink = confirmationLink,
                    HostUri = new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}"),
                    EmailLocalizer = _emailLocalizer
                }, assembly)
            .SendAsync(cancellationToken);

        user.ConfirmationEmailRequestedOn = DateTimeOffset.Now;

        await _userManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.Select(err => Localizer[err]).ToArray());
    }

    [HttpPost]
    public async Task SendResetPasswordEmail(SendResetPasswordEmailRequestDto sendResetPasswordEmailRequest
        , CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(sendResetPasswordEmailRequest.Email);

        if (user is null)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), sendResetPasswordEmailRequest.Email!));

        var resendDelay = (DateTimeOffset.Now - user.ResetPasswordEmailRequestedOn) - AppSettings.IdentitySettings.ResetPasswordEmailResendDelay;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForResetPasswordEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetPasswordLink = $"reset-password?email={HttpUtility.UrlEncode(user.Email)}&token={HttpUtility.UrlEncode(token)}";

#if BlazorServer
        resetPasswordLink = $"{AppSettings.WebServerAddress}{resetPasswordLink}";
#else
        resetPasswordLink = $"{new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}")}{resetPasswordLink}";
#endif

        var assembly = typeof(Program).Assembly;

        var result = await _fluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(_emailLocalizer[EmailStrings.ResetPasswordEmailSubject])
            .UsingTemplateFromEmbedded("BlazorDual.Api.Resources.ResetPassword.cshtml",
                                    new ResetPasswordModel
                                    {
                                        DisplayName = user.DisplayName,
                                        ResetPasswordLink = resetPasswordLink,
                                        HostUri = new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}"),
                                        EmailLocalizer = _emailLocalizer
                                    },
                                    assembly)
            .SendAsync(cancellationToken);

        user.ResetPasswordEmailRequestedOn = DateTimeOffset.Now;

        await _userManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.Select(err => Localizer[err]).ToArray());
    }

    [HttpGet]
    public async Task<ActionResult> ConfirmEmail(string email, string token)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), email));

        var emailConfirmed = user.EmailConfirmed || (await _userManager.ConfirmEmailAsync(user, token)).Succeeded;

        string url = $"email-confirmation?email={email}&email-confirmed={emailConfirmed}";

#if BlazorServer
        url = $"{AppSettings.WebServerAddress}{url}";
#else
        url = $"/{url}";
#endif

        return Redirect(url);
    }

    [HttpPost]
    public async Task ResetPassword(ResetPasswordRequestDto resetPasswordRequest)
    {
        var user = await _userManager.FindByEmailAsync(resetPasswordRequest.Email);

        if (user is null)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), resetPasswordRequest.Email!));

        var result = await _userManager.ResetPasswordAsync(user, resetPasswordRequest.Token, resetPasswordRequest.Password);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => Localizer.GetString(e.Code, resetPasswordRequest.Email!)).ToArray());
    }

    [HttpPost]
    public async Task<SignInResponseDto> SignIn(SignInRequestDto signInRequest)
    {
        var user = await _userManager.FindByNameAsync(signInRequest.UserName);

        if (user is null)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), signInRequest.UserName!));

        var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, signInRequest.Password, lockoutOnFailure: true);

        if (checkPasswordResult.IsLockedOut)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd).Value.ToString("mm\\:ss")));

        if (!checkPasswordResult.Succeeded)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), signInRequest.UserName!));

        return await _jwtService.GenerateToken(user);
    }
}
