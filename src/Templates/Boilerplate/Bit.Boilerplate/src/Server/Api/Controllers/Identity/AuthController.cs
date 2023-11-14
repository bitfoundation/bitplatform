//-:cnd:noEmit
using System.Web;
using Boilerplate.Server.Api.Models.Emailing;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Resources;
using Boilerplate.Shared.Dtos.Identity;
using FluentEmail.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Server.Api.Controllers.Identity;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class AuthController : AppControllerBase
{
    [AutoInject] private UserManager<User> _userManager = default!;

    [AutoInject] private IJwtService _jwtService = default!;

    [AutoInject] private SignInManager<User> _signInManager = default!;

    [AutoInject] private IFluentEmail _fluentEmail = default!;

    [AutoInject] private IStringLocalizer<EmailStrings> _emailLocalizer = default!;

    [AutoInject] private HtmlRenderer _htmlRenderer = default!;

    [AutoInject] protected IStringLocalizer<IdentityStrings> IdentityLocalizer = default!;

    /// <summary>
    /// By leveraging summary tags in your controller's actions and DTO properties you can make your codes much easier to maintain.
    /// These comments will also be used in swagger docs and ui.
    /// </summary>
    [HttpPost]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByNameAsync(signUpRequest.Email!);

        var userToAdd = signUpRequest.Map();

        if (existingUser is not null)
        {
            if (await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                throw new BadRequestException(Localizer.GetString(nameof(AppStrings.DuplicateEmail), existingUser.Email!));
            }
            else
            {
                await _userManager.DeleteAsync(existingUser);
                userToAdd.ConfirmationEmailRequestedOn = existingUser.ConfirmationEmailRequestedOn;
            }
        }

        var result = await _userManager.CreateAsync(userToAdd, signUpRequest.Password!);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => IdentityLocalizer.GetString(e.Code, signUpRequest.Email!)).ToArray());
        }

        await SendConfirmationEmail(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
    }

    [HttpPost]
    public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(sendConfirmationEmailRequest.Email!);

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

        var body = await _htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await _htmlRenderer.RenderComponentAsync<EmailConfirmationTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
            {
                {   nameof(EmailConfirmationTemplate.Model),
                    new EmailConfirmationModel
                    {
                        ConfirmationLink = confirmationLink,
                        HostUri = new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}")
                    }
                }
            }));

            return renderedComponent.ToHtmlString();
        });

        var result = await _fluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(_emailLocalizer[EmailStrings.ConfirmationEmailSubject])
            .Body(body, isHtml: true)
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
        var user = await _userManager.FindByEmailAsync(sendResetPasswordEmailRequest.Email!);

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

        var body = await _htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await _htmlRenderer.RenderComponentAsync<ResetPasswordTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
            {
                { nameof(ResetPasswordTemplate.Model),
                    new ResetPasswordModel
                    {
                        DisplayName = user.DisplayName,
                        ResetPasswordLink = resetPasswordLink,
                        HostUri = new Uri($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}")
                    }
                }
            }));

            return renderedComponent.ToHtmlString();
        });

        var result = await _fluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(_emailLocalizer[EmailStrings.ResetPasswordEmailSubject])
            .Body(body, isHtml: true)
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
        var user = await _userManager.FindByEmailAsync(resetPasswordRequest.Email!);

        if (user is null)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), resetPasswordRequest.Email!));

        var result = await _userManager.ResetPasswordAsync(user, resetPasswordRequest.Token!, resetPasswordRequest.Password!);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => IdentityLocalizer.GetString(e.Code, resetPasswordRequest.Email!)).ToArray());
    }

    [HttpPost]
    public async Task<SignInResponseDto> SignIn(SignInRequestDto signInRequest)
    {
        var user = await _userManager.FindByNameAsync(signInRequest.UserName!);

        if (user is null)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), signInRequest.UserName!));

        var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, signInRequest.Password!, lockoutOnFailure: true);

        if (checkPasswordResult.IsLockedOut)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.ToString("mm\\:ss")));

        if (!checkPasswordResult.Succeeded)
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.InvalidUsernameOrPassword), signInRequest.UserName!));

        return await _jwtService.GenerateToken(user);
    }
}
