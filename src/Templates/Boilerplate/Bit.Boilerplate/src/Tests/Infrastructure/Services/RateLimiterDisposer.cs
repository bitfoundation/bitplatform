using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Boilerplate.Tests.Infrastructure.Services;

/// <summary>
/// Disposes the rate limiters of a test server once it has stopped.
/// <para>
/// The rate limiting middleware creates a partitioned limiter of its own and never disposes it
/// (https://github.com/dotnet/aspnetcore/issues/66434). The limiter's heartbeat timer is a GC root that reaches the
/// middleware, the rest of the request pipeline and the app's services, so every test server would otherwise stay in
/// memory until the test process exits. Nothing public reaches that limiter, so this finds the middleware in the built
/// pipeline, the way each middleware holds the next one. The global limiter is the app's own and is disposed with it.
/// </para>
/// </summary>
public sealed class RateLimiterDisposer(IHostApplicationLifetime lifetime, IOptions<RateLimiterOptions> rateLimiterOptions) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(pipeline =>
        {
            var limiters = FindMiddlewareLimiters(pipeline).ToList();

            lifetime.ApplicationStopped.Register(() =>
            {
                foreach (var limiter in limiters)
                {
                    limiter.Dispose();
                }

                rateLimiterOptions.Value.GlobalLimiter?.Dispose();
            });

            return pipeline;
        });

        next(app);
    };

    private static IEnumerable<IDisposable> FindMiddlewareLimiters(RequestDelegate pipeline)
    {
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        Queue<object> pending = new([pipeline]);

        while (visited.Count < 10_000 && pending.TryDequeue(out var current))
        {
            if (visited.Add(current) is false)
                continue;

            if (current is Delegate @delegate)
            {
                foreach (var target in @delegate.GetInvocationList().Select(invocation => invocation.Target).OfType<object>())
                {
                    pending.Enqueue(target);
                }

                continue;
            }

            if (current.GetType().FullName is "Microsoft.AspNetCore.RateLimiting.RateLimitingMiddleware")
            {
                if (current.GetType().GetField("_endpointLimiter", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(current) is IDisposable limiter)
                    yield return limiter;

                continue;
            }

            // A middleware, a closure of app.Use or the options of a branch: whatever holds a request delegate leads further down the pipeline.
            foreach (var field in GetInstanceFields(current.GetType()))
            {
                if (field.GetValue(current) is { } value && (value is Delegate || GetInstanceFields(value.GetType()).Any(f => f.FieldType == typeof(RequestDelegate))))
                {
                    pending.Enqueue(value);
                }
            }
        }
    }

    private static IEnumerable<FieldInfo> GetInstanceFields(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType.IsValueType is false)
                    yield return field;
            }
        }
    }
}
