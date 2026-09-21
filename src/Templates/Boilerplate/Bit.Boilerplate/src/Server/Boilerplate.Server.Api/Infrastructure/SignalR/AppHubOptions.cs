//+:cnd:noEmit
using Microsoft.AspNetCore.Http.Connections;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

public static class AppHubOptions
{
    public static void Configure(HttpConnectionDispatcherOptions options)
    {
        options.AllowStatefulReconnects = true;

        options.EnableAuthenticationRefresh = true;
        options.CloseOnAuthenticationExpiration = true;

        options.OnAuthenticationRefresh = context =>
        {
            var previous = context.PreviousUser;
            var refreshed = context.NewUser;

            if (previous.IsAuthenticated() is false || refreshed.IsAuthenticated() is false)
                return Task.FromResult(false);

            return Task.FromResult(previous.GetUserId() == refreshed.GetUserId() &&
                                   previous.GetSessionId() == refreshed.GetSessionId());
        };
    }
}
