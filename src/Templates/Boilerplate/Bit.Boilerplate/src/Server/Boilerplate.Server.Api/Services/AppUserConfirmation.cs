using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Services;

public class AppUserConfirmation : IUserConfirmation<User>
{
    public async Task<bool> IsConfirmedAsync(UserManager<User> manager, User user)
    {
        return user.EmailConfirmed || user.PhoneNumberConfirmed;
    }
}
