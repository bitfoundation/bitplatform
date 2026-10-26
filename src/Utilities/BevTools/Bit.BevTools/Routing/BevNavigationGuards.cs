using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BevTools;

/// <summary>Something that can stop or confirm a navigation before it happens.</summary>
/// <param name="Kind">"NavigationLock" or "LocationChanging handler".</param>
/// <param name="Description">What it does: the handler it runs, whether it confirms leaving the site.</param>
/// <param name="OwnerInstanceId">The component that registered it, when it is one BevTools tracks.</param>
/// <param name="OwnerName">That component's name.</param>
public sealed record BevNavigationGuard(string Kind, string Description, long? OwnerInstanceId, string? OwnerName);

/// <summary>
/// Reads what can block or confirm navigation, without registering anything: a LocationChanging handler of BevTools'
/// own would make the browser ask .NET before every navigation, which changes how the app behaves.
/// </summary>
internal static class BevNavigationGuards
{
    private static readonly FieldInfo? HandlersField = typeof(NavigationManager).GetField("_locationChangingHandlers", BindingFlags.Instance | BindingFlags.NonPublic);

    public static bool IsAvailable => HandlersField is not null;

    public static IReadOnlyList<BevNavigationGuard> Read(BevToolsSession session)
    {
        var guards = new List<BevNavigationGuard>();
        var locks = new HashSet<object>(ReferenceEqualityComparer.Instance);

        // NavigationLock components first: they say more than the handler they register.
        foreach (var record in session.Components.All)
        {
            if (record.IsDisposed || record.IsBevTools || record.Component is not NavigationLock navigationLock)
            {
                continue;
            }

            locks.Add(navigationLock);
            var parts = new List<string>();
            if (navigationLock.ConfirmExternalNavigation)
            {
                parts.Add("asks the browser to confirm leaving the site");
            }

            if (navigationLock.OnBeforeInternalNavigation.HasDelegate)
            {
                parts.Add("runs " + BevEventCallbacks.Describe(navigationLock.OnBeforeInternalNavigation) + " before every navigation within the app, which can prevent it");
            }

            var owner = Owner(record);
            guards.Add(new BevNavigationGuard("NavigationLock", parts.Count == 0 ? "registered, and does nothing (no confirmation, no callback)" : string.Join("; ", parts), owner?.InstanceId, owner?.DisplayName));
        }

        if (session.Services.GetService(typeof(NavigationManager)) is NavigationManager navigation
            && SafeHandlers(navigation) is { } handlers)
        {
            foreach (var handler in handlers.OfType<Delegate>())
            {
                if (handler.Target is { } target && locks.Contains(target))
                {
                    continue;
                }

                var owner = OwnerOf(session, handler);
                guards.Add(new BevNavigationGuard("LocationChanging handler", BevEventCallbacks.Method(handler) + " runs before every navigation within the app, and can prevent it", owner?.InstanceId, owner?.DisplayName));
            }
        }

        return guards;
    }

    private static IList? SafeHandlers(NavigationManager navigation)
    {
        try
        {
            return HandlersField?.GetValue(navigation) is IList list ? list.Cast<object>().ToArray() : null;
        }
        catch
        {
            return null;
        }
    }

    private static BevComponentRecord? Owner(BevComponentRecord record)
    {
        for (var current = record.Parent; current is not null; current = current.Parent)
        {
            if (!current.IsHidden && !current.IsBevTools)
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>The component a handler belongs to: its target, or the component a lambda's closure captured.</summary>
    private static BevComponentRecord? OwnerOf(BevToolsSession session, Delegate handler)
    {
        var target = handler.Target;
        if (target is IComponent component && session.Components.TryGet(component, out var record))
        {
            return record;
        }

        if (target is not null && target.GetType().IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            foreach (var field in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.GetValue(target) is IComponent captured && session.Components.TryGet(captured, out var owner))
                {
                    return owner;
                }
            }
        }

        return null;
    }
}
