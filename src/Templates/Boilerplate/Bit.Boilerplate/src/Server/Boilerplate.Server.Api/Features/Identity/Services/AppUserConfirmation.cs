using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Server.Api.Features.Identity.Services;

public partial class AppUserConfirmation : IUserConfirmation<User>
{
    public async Task<bool> IsConfirmedAsync(UserManager<User> manager, User user)
    {
        return user.EmailConfirmed ||
            user.PhoneNumberConfirmed ||
            (await manager.GetLoginsAsync(user)).Any();
    }
}
