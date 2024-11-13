namespace Boilerplate.Server.Api.Services.Identity;

public partial class AppIdentityErrorDescriber : IdentityErrorDescriber
{
    [AutoInject] IStringLocalizer<IdentityStrings> localizer = default!;

    IdentityError CreateIdentityError(string code, params object[] args)
    {
        return new()
        {
            Code = code,
            Description = localizer[code, args]
        };
    }

    public override IdentityError ConcurrencyFailure() => CreateIdentityError(nameof(IdentityStrings.ConcurrencyFailure));

    public override IdentityError DuplicateEmail(string email) => CreateIdentityError(nameof(IdentityStrings.DuplicateEmail), email);

    public override IdentityError DuplicateRoleName(string role) => CreateIdentityError(nameof(IdentityStrings.DuplicateRoleName), role);

    public override IdentityError DuplicateUserName(string userName) => CreateIdentityError(nameof(IdentityStrings.DuplicateUserName), userName);

    public override IdentityError InvalidEmail(string? email) => CreateIdentityError(nameof(IdentityStrings.InvalidEmail), email ?? string.Empty);

    public override IdentityError InvalidRoleName(string? role) => CreateIdentityError(nameof(IdentityStrings.InvalidRoleName), role ?? string.Empty);

    public override IdentityError InvalidToken() => CreateIdentityError(nameof(IdentityStrings.InvalidToken));

    public override IdentityError InvalidUserName(string? userName) => CreateIdentityError(nameof(IdentityStrings.InvalidUserName), userName ?? string.Empty);

    public override IdentityError LoginAlreadyAssociated() => CreateIdentityError(nameof(IdentityStrings.LoginAlreadyAssociated));

    public override IdentityError PasswordMismatch() => CreateIdentityError(nameof(IdentityStrings.PasswordMismatch));

    public override IdentityError PasswordRequiresDigit() => CreateIdentityError(nameof(IdentityStrings.PasswordRequiresDigit));

    public override IdentityError PasswordRequiresLower() => CreateIdentityError(nameof(IdentityStrings.PasswordRequiresLower));

    public override IdentityError PasswordRequiresNonAlphanumeric() => CreateIdentityError(nameof(IdentityStrings.PasswordRequiresNonAlphanumeric));

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => CreateIdentityError(nameof(IdentityStrings.PasswordRequiresUniqueChars), uniqueChars);

    public override IdentityError PasswordRequiresUpper() => CreateIdentityError(nameof(IdentityStrings.PasswordRequiresUpper));

    public override IdentityError PasswordTooShort(int length) => CreateIdentityError(nameof(IdentityStrings.PasswordTooShort));

    public override IdentityError RecoveryCodeRedemptionFailed() => CreateIdentityError(nameof(IdentityStrings.RecoveryCodeRedemptionFailed));

    public override IdentityError UserAlreadyHasPassword() => CreateIdentityError(nameof(IdentityStrings.UserAlreadyHasPassword));

    public override IdentityError UserAlreadyInRole(string role) => CreateIdentityError(nameof(IdentityStrings.UserAlreadyInRole), role);

    public override IdentityError UserLockoutNotEnabled() => CreateIdentityError(nameof(IdentityStrings.UserLockoutNotEnabled));

    public override IdentityError UserNotInRole(string role) => CreateIdentityError(nameof(IdentityStrings.UserNotInRole), role);

    public override IdentityError DefaultError() => CreateIdentityError(nameof(IdentityStrings.DefaultError));
}
