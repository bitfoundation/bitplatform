using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI.Legacy;

internal static class AsyncQueryExecutorSupplier
{
    // The primary goal with this is to ensure that:
    //  - If you're using EF Core, then we resolve queries efficiently using its ToXyzAsync async extensions and don't
    //    just fall back on the synchronous IQueryable ToXyz calls
    //  - ... but without BitDataGrid referencing Microsoft.EntityFramework directly. That's because it would bring in
    //    heavy dependencies you may not be using (and relying on trimming isn't enough, as it's still desirable to have
    //    heavy unused dependencies for Blazor Server).
    //
    // As a side-effect, we have an abstraction IAsyncQueryExecutor that developers could use to plug in their own
    // mechanism for resolving async queries from other data sources than EF. It's not really a major goal to make this
    // adapter generally useful beyond EF, but fine if people do have their own uses for it.

    private static readonly ConcurrentDictionary<Type, bool> IsEntityFrameworkProviderTypeCache = new();

    [SuppressMessage("Trimming", "IL2111:Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.", Justification = "<Pending>")]
    public static IAsyncQueryExecutor? GetAsyncQueryExecutor<T>(IServiceProvider services, IQueryable<T>? queryable)
    {
        if (queryable is not null)
        {
            // Inspect every registered executor, not just the first one resolved: a registered executor
            // that does not support this queryable must not shadow another that does, nor suppress the
            // EF misconfiguration warning below. Keep scanning and return the *last* supported executor
            // so later, more specific registrations override earlier generic ones - mirroring how DI
            // resolves a single service (last registration wins).
            IAsyncQueryExecutor? selected = null;
            foreach (var executor in services.GetServices<IAsyncQueryExecutor>())
            {
                if (executor.IsSupported(queryable))
                {
                    selected = executor;
                }
            }

            if (selected is not null)
            {
                return selected;
            }

            // No registered executor supports this queryable. It's useful to detect if the developer is
            // unaware that they should register an IAsyncQueryExecutor, otherwise they will likely never
            // notice and simply deploy an inefficient app that blocks threads on each query.
            var providerType = queryable.Provider?.GetType();
            if (providerType is not null && IsEntityFrameworkProviderTypeCache.GetOrAdd(providerType, IsEntityFrameworkProviderType))
            {
                throw new InvalidOperationException(
                    $"The supplied {nameof(IQueryable)} is provided by Entity Framework. To query it efficiently without blocking threads, " +
                    $"register an implementation of {nameof(IAsyncQueryExecutor)} in your service collection that wraps EF Core's async query APIs " +
                    $"(for example ToArrayAsync/CountAsync) and reports IsSupported(queryable) == true for EF queryables. " +
                    $"Alternatively, supply non-EF data via the Items or ItemsProvider parameters.");
            }
        }

        return null;
    }

    // We have to do this via reflection because the whole point is to avoid any static dependency on EF unless you
    // reference the adapter. Trimming won't cause us any problems because this is only a way of detecting misconfiguration
    // so it's sufficient if it can detect the misconfiguration in development.
    private static bool IsEntityFrameworkProviderType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type queryableProviderType)
        => queryableProviderType.GetInterfaces().Any(x => string.Equals(x.FullName, "Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider")) == true;
}
