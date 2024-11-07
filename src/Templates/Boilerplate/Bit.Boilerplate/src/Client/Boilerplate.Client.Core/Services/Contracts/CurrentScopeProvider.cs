using Boilerplate.Client.Core.Components;

namespace Boilerplate.Client.Core.Services.Contracts;

/// <summary>
/// Provides the current scope's <see cref="IServiceProvider"/>.
///
/// In different hosting environments, this delegate returns the `IServiceProvider` from:
///
/// - **Blazor Server, SSR, and Pre-rendering:** `HttpContextAccessor.HttpContext.RequestServices`
/// - **Blazor WebAssembly and Hybrid:** Gets it from <see cref="ClientAppCoordinator.CurrentServiceProvider"/>
///
/// The delegate may return `null` in the following scenarios:
///
/// - When there's no active `HttpContext` in backend environments.
/// - When the Routes.razor page is not loaded yet.
/// </summary>
/// <returns>The current scope's `IServiceProvider`, or `null` if not available.</returns>
public delegate IServiceProvider? CurrentScopeProvider();
