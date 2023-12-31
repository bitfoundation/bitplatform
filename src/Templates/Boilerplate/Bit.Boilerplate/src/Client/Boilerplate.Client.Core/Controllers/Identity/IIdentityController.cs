﻿using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IIdentityController : IAppController
{
    [HttpPost]
    Task SignUp(SignUpRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task SendConfirmationEmail(SendConfirmationEmailRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task SendResetPasswordEmail(SendResetPasswordEmailRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task ConfirmEmail(ConfirmEmailRequestDto body);

    [HttpPost]
    Task ResetPassword(ResetPasswordRequestDto body, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<TokenResponseDto> Refresh(RefreshRequestDto body, CancellationToken cancellationToken = default) => default!;

    [HttpPost]
    Task<TokenResponseDto> SignIn(SignInRequestDto body, CancellationToken cancellationToken = default) => default!;
}
