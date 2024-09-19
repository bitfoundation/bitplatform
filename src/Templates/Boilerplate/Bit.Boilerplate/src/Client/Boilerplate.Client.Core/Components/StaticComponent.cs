namespace Boilerplate.Client.Core.Components;

/// <summary>
/// To prevent rendering recursion into a particular subtree use this component as base class.
/// <see href="https://learn.microsoft.com/en-us/aspnet/core/blazor/performance?view=aspnetcore-8.0#avoid-unnecessary-rendering-of-component-subtrees"/>
/// </summary>
public class StaticComponent: ComponentBase
{
    protected override bool ShouldRender() => false;
}
