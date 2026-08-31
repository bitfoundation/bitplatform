namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.CascadingValueProvider;

/// <summary>
/// A mutable state holder, cascaded as a single instance that is updated in place, which is the case
/// that BitCascadingValue.NotifyChanged exists for.
/// </summary>
public sealed class CascadingDemoStatus
{
    public string Text { get; set; } = "Idle";
}
