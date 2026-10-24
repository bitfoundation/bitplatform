using System.Security.Cryptography;

namespace Boilerplate.Server.Api.Features.Identity.Services;

public partial class AppUserManager(IUserStore<User> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<User> passwordHasher,
    IEnumerable<IUserValidator<User>> userValidators,
    IEnumerable<IPasswordValidator<User>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<User>> logger,
    IConfiguration configuration,
    ServerApiSettings appSettings,
    TimeProvider timeProvider)
    : UserManager<User>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    public override async Task<byte[]> CreateSecurityTokenAsync(User user)
    {
        return HMACSHA256.HashData(AppCertificateService.GetSecurityTokenKey(configuration), await base.CreateSecurityTokenAsync(user));
    }

    public override Task<string> GenerateTwoFactorTokenAsync(User user, string tokenProvider)
    {
        if (tokenProvider != TokenOptions.DefaultPhoneProvider)
            return base.GenerateTwoFactorTokenAsync(user, tokenProvider);

        return GenerateUserTokenAsync(user, tokenProvider, GetTwoFactorTokenPurpose(user));
    }

    public override async Task<bool> VerifyTwoFactorTokenAsync(User user, string tokenProvider, string token)
    {
        if (tokenProvider != TokenOptions.DefaultPhoneProvider)
            return await base.VerifyTwoFactorTokenAsync(user, tokenProvider, token);

        if (user.TwoFactorTokenRequestedOn is null ||
            (timeProvider.GetUtcNow() - user.TwoFactorTokenRequestedOn.Value) > appSettings.Identity.TwoFactorTokenLifetime)
            return false;

        return await VerifyUserTokenAsync(user, tokenProvider, GetTwoFactorTokenPurpose(user), token);
    }

    private static string GetTwoFactorTokenPurpose(User user)
    {
        return FormattableString.Invariant($"TwoFactor,{user.TwoFactorTokenRequestedOn?.ToUniversalTime()}");
    }
}
