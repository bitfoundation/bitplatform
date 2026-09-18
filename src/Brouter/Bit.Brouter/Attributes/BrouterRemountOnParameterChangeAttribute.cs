namespace Bit.Brouter;

/// <summary>
/// Page-level counterpart of <see cref="Broute.RemountOnParameterChange"/>, declared on the routed
/// component itself next to its <c>@page</c> / <c>[Route]</c>:
/// <c>@attribute [BrouterRemountOnParameterChange]</c>. A navigation that stays on the page but
/// changes its route parameter values then disposes the page and mounts a fresh instance instead of
/// re-binding the live one. Pass <c>false</c> to keep the instance even where
/// <see cref="BrouterOptions.RemountOnParameterChange"/> turns rebuilding on application-wide.
/// <para>
/// Applies to attribute-discovered routes and to any <see cref="Broute"/> rendering the component
/// through <see cref="Broute.Component"/>. An explicit <see cref="Broute.RemountOnParameterChange"/>
/// on the route takes precedence over this attribute, which in turn takes precedence over the global
/// option. Ignored on a <see cref="Broute.KeepAlive"/> route, which never remounts.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BrouterRemountOnParameterChangeAttribute(bool remount = true) : Attribute
{
    /// <summary>Whether a parameter-only navigation rebuilds the page. Defaults to <c>true</c>.</summary>
    public bool Remount { get; } = remount;
}
