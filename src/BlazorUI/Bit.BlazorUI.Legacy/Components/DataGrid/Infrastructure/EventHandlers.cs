namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Configures event handlers for <see cref="BitDataGridLegacy{TGridItem}"/>.
/// </summary>
[EventHandler("onclosecolumnoptions", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}
