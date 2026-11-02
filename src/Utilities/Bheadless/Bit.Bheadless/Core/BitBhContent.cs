using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// Content selection for components that accept both plain <c>ChildContent</c> and a
/// state-receiving <c>Template</c>.
/// </summary>
/// <remarks>
/// <c>ChildContent</c> is always a plain <see cref="RenderFragment"/> so components nest
/// freely inside other templated content (<c>EditForm</c>, panels …) without Razor
/// <c>Context</c> name clashes. Markup that depends on the component's state uses
/// <c>&lt;Template Context="state"&gt;</c> instead.
/// </remarks>
internal static class BitBhContent
{
    /// <summary>The template applied to <paramref name="state"/> when supplied, otherwise the plain content.</summary>
    public static RenderFragment? Pick<TState>(RenderFragment<TState>? template, RenderFragment? content, TState state)
        => template is not null ? template(state) : content;
}
