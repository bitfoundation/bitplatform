using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;
using FluentEmail.Core;
using TodoTemplate.Api.Resources;
using TodoTemplate.Api.Models.Emailing;
using Microsoft.AspNetCore.Hosting.Server;
using System.Web;
using Bit.Tooling.SourceGenerators;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController, AllowAnonymous]
public partial class AuthController : ControllerBase
{
    [AutoInject] public UserManager<User> UserManager { get; set; }

    [AutoInject] public IJwtService JwtService { get; set; }

    [AutoInject] public IMapper Mapper { get; set; }

    [AutoInject] public SignInManager<User> SignInManager { get; set; }

    [AutoInject] public IOptionsSnapshot<AppSettings> AppSettings { get; set; }

    [AutoInject] public IFluentEmail FluentEmail { get; set; }

    [AutoInject] public IServer Server { get; set; }

    [HttpPost("[action]")]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        var existingUser = await UserManager.FindByNameAsync(signUpRequest.UserName);

        var userToAdd = Mapper.Map<User>(signUpRequest);

        if (existingUser is not null)
        {
            if (await UserManager.IsEmailConfirmedAsync(existingUser))
            {
                throw new BadRequestException(nameof(ErrorStrings.DuplicateEmail));
            }
            else
            {
                await UserManager.DeleteAsync(existingUser);
                userToAdd.ConfirmationEmailRequestedOn = existingUser.ConfirmationEmailRequestedOn;
            }
        }

        var result = await UserManager.CreateAsync(userToAdd, signUpRequest.Password);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());
        }

        await SendConfirmationEmail(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
    }

    [HttpPost("[action]")]
    public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    {
        var user = await UserManager.FindByEmailAsync(sendConfirmationEmailRequest.Email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        if (await UserManager.IsEmailConfirmedAsync(user))
            throw new BadRequestException(nameof(ErrorStrings.EmailAlreadyConfirmed));

        await SendConfirmationEmail(sendConfirmationEmailRequest, user, cancellationToken);
    }

    private async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, User user, CancellationToken cancellationToken)
    {
        if ((DateTimeOffset.Now - user.ConfirmationEmailRequestedOn) < AppSettings.Value.IdentitySettings.ConfirmationEmailResendDelay)
            throw new TooManyRequestsExceptions(nameof(ErrorStrings.WaitForConfirmationEmailResendDelay));

        var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);

        var controller = RouteData.Values["controller"]!.ToString();

        var confirmationLink = Url.Action(nameof(ConfirmEmail), controller,
            new { user.Email, token },
            HttpContext.Request.Scheme);

        var assembly = typeof(Program).Assembly;

        var result = await FluentEmail
            .To(user.Email, user.DisplayName)
            .Subject(EmailStrings.ConfirmationEmailSubject)
            .UsingTemplateFromEmbedded("TodoTemplate.Api.Resources.EmailConfirmation.cshtml",
                                    new EmailConfirmationModel
                                    {
                                        ConfirmationLink = confirmationLink,
                                        HostUri = Server.GetHostUri()
                                    },
                                    assembly)
            .SendAsync(cancellationToken);

        user.ConfirmationEmailRequestedOn = DateTimeOffset.Now;

        await UserManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.ToArray());
    }

    [HttpPost("[action]")]
    public async Task SendResetPasswordEmail(SendResetPasswordEmailRequestDto sendResetPasswordEmailRequest
        , CancellationToken cancellationToken)
    {
        var user = await UserManager.FindByEmailAsync(sendResetPasswordEmailRequest.Email);

        if ((DateTimeOffset.Now - user.ResetPasswordEmailRequestedOn) < AppSettings.Value.IdentitySettings.ResetPasswordEmailResendDelay)
            throw new TooManyRequestsExceptions(nameof(ErrorStrings.WaitForResetPasswordEmailResendDelay));

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var token = await UserManager.GeneratePasswordResetTokenAsync(user);

        var resetPasswordLink = $"reset-password?email={user.Email}&token={HttpUtility.UrlEncode(token)}";

#if BlazorServer
        resetPasswordLink = $"{AppSettings.Value.WebServerAddress}{resetPasswordLink}";
#else
        resetPasswordLink = $"{Server.GetHostUri()}{resetPasswordLink}";
#endif

        var assembly = typeof(Program).Assembly;

        var result = await FluentEmail
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

        await UserManager.UpdateAsync(user);

        if (!result.Successful)
            throw new ResourceValidationException(result.ErrorMessages.ToArray());
    }

    [HttpGet("[action]")]
    public async Task<ActionResult> ConfirmEmail(string email, string token)
    {
        var user = await UserManager.FindByEmailAsync(email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var emailConfirmed = user.EmailConfirmed || (await UserManager.ConfirmEmailAsync(user, token)).Succeeded;

        string url = $"email-confirmation?email={email}&email-confirmed={emailConfirmed}";

#if BlazorServer
        url = $"{AppSettings.Value.WebServerAddress}{url}";
#else
        url = $"/{url}";
#endif

        return Redirect(url);
    }

    [HttpPost("[action]")]
    public async Task ResetPassword(ResetPasswordRequestDto resetPasswordRequest)
    {
        var user = await UserManager.FindByEmailAsync(resetPasswordRequest.Email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var result = await UserManager.ResetPasswordAsync(user, resetPasswordRequest.Token, resetPasswordRequest.Password);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());
    }

    [HttpPost("[action]")]
    public async Task<SignInResponseDto> SignIn(SignInRequestDto signInRequest)
    {
        var user = await UserManager.FindByNameAsync(signInRequest.UserName);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var checkPasswordResult = await SignInManager.CheckPasswordSignInAsync(user, signInRequest.Password, lockoutOnFailure: true);

        if (checkPasswordResult.IsLockedOut)
            throw new BadRequestException(nameof(ErrorStrings.UserLockedOut));

        if (!checkPasswordResult.Succeeded)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        return await JwtService.GenerateToken(user);
    }
}
