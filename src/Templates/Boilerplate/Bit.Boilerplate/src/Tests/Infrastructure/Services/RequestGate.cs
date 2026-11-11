using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Boilerplate.Tests.Infrastructure.Services;

/// <summary>
/// Holds Server.Api's answer to the requests under a path until the test lets them through, so a test can see what the
/// app shows while it waits on the server - a slow network, without the timing of one. Register it with
/// <see cref="RequestGateExtensions.AddRequestGate"/> through <see cref="AppTestServer.Build"/>'s configureTestApiAppServices.
/// </summary>
public sealed class RequestGate
{
    private volatile string? heldPath;
    private volatile TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>From now on, requests under <paramref name="path"/> wait until <see cref="Release"/>.</summary>
    public void Hold(string path)
    {
        released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        heldPath = path;
    }

    /// <summary>Lets the held requests and every later one through.</summary>
    public void Release()
    {
        heldPath = null;
        released.TrySetResult();
    }

    internal Task WaitIfHeld(HttpContext context)
    {
        return heldPath is { } path && context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)
            ? released.Task.WaitAsync(context.RequestAborted)
            : Task.CompletedTask;
    }
}

public static class RequestGateExtensions
{
    public static IServiceCollection AddRequestGate(this IServiceCollection services, RequestGate gate)
    {
        return services.AddSingleton<IStartupFilter>(new RequestGateStartupFilter(gate));
    }

    private sealed class RequestGateStartupFilter(RequestGate gate) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                await gate.WaitIfHeld(context);
                await nextMiddleware(context);
            });

            next(app);
        };
    }
}
