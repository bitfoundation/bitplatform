using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;
using Microsoft.AspNetCore.Hosting.Server;
using FluentEmail.Core;
using TodoTemplate.Api.Resources;
using System.Net;

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

    public AuthController(SignInManager<User> signInManager,
        UserManager<User> userManager,
        IJwtService jwtService,
        IMapper mapper,
        IOptionsSnapshot<AppSettings> setting
        )
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _mapper = mapper;
        _signInManager = signInManager;
        _appSettings = setting.Value;
    }

    [HttpPost("[action]")]
    public async Task SignUp(SignUpRequestDto signUpRequest)
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

        await SendEmailConfirmLink(new() { Email = userToAdd.Email });
    }

    [HttpPost("[action]")]
    public async Task SendEmailConfirmLink(SendEmailConfirmLinkRequestDto sendConfirmLinkEmailRequest)
    {
        var user = await _userManager.FindByEmailAsync(sendConfirmLinkEmailRequest.Email);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var controller = RouteData.Values["controller"]!.ToString();

        var confirmationLink = Url.Action(nameof(ConfirmEmail), controller,
            new { user.Email, token },
            HttpContext.Request.Scheme);

        var assembly = typeof(Program).Assembly;

        var result = await Email
            .From(_appSettings.EmailSettings.FromEmailAddress)
            .To(user.Email, user.DisplayName)
            .Subject(EmailStrings.ConfirmationLinkEmailSubject)
            .UsingTemplateFromEmbedded("TodoTemplate.Api.Resources.EmailConfirmation.cshtml",
                                    new { DisplayName = user.DisplayName, Email = user.Email, ConfirmationLink = confirmationLink },
                                    assembly)
            .SendAsync();

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
            throw new BadRequestException(nameof(ErrorStrings.BadRequestException));

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(e => e.Code).ToArray());

#if BlazorServer
        var url = $"{_appSettings.WebServerAddress}email-confirmation?email={email}";
#else
        var url = "/email-confirmation?email={email}";
#endif
        return Redirect(url);
    }

    [HttpPost("[action]")]
    public async Task<bool> EmailConfirmed(EmailConfirmedRequestDto emailConfirmedRequest)
    {
        var user = await _userManager.FindByNameAsync(emailConfirmedRequest.Email);

        if (user is null)
            throw new BadRequestException(nameof(ErrorStrings.UserNameNotFound));

        var emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

        if (!emailConfirmed)
            throw new BadRequestException(nameof(ErrorStrings.EmailNotConfirmed));

        return true;
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
