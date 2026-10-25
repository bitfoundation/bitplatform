using Microsoft.Extensions.Caching.Memory;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

public partial class UserSessionHubFilter(AppDbContext dbContext, IMemoryCache memoryCache) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var changesAuthenticationState = invocationContext.HubMethodName is SharedAppMessages.ChangeAuthenticationState;

        if (changesAuthenticationState is false)
        {
            await AbortIfUserSessionIsRevoked(invocationContext.Context);
        }

        var result = await next(invocationContext);

        if (changesAuthenticationState)
        {
            await AbortIfUserSessionIsRevoked(invocationContext.Context);
        }

        return result;
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (await IsUserSessionRevoked(context.Context))
            throw new HubException(nameof(AppStrings.UnauthorizedException)).WithData("ConnectionId", context.Context.ConnectionId);

        await next(context);
    }

    private async Task AbortIfUserSessionIsRevoked(HubCallerContext context)
    {
        if (await IsUserSessionRevoked(context) is false)
            return;

        context.Abort();

        throw new HubException(nameof(AppStrings.UnauthorizedException)).WithData("ConnectionId", context.ConnectionId);
    }

    private async Task<bool> IsUserSessionRevoked(HubCallerContext context)
    {
        var user = context.GetHttpContext()?.User ?? context.User;

        if (user?.IsAuthenticated() is not true)
            return false;

        var userSessionId = user.GetSessionId();

        var securityStamp = await memoryCache.GetOrCreateAsync($"UserSessionSecurityStamp_{context.ConnectionId}_{userSessionId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

            return await dbContext.UserSessions
                .Where(us => us.Id == userSessionId)
                .Select(us => us.User!.SecurityStamp)
                .FirstOrDefaultAsync(context.ConnectionAborted);
        });

        return securityStamp is null || securityStamp != user.GetClaimValue<string?>("AspNet.Identity.SecurityStamp");
    }
}
