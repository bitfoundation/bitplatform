namespace Bit.BlazorUI;

/// <summary>
/// Configures event handlers for <see cref="BitDataGrid{TGridItem}"/>.
/// </summary>
[EventHandler("onclosecolumnoptions", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}
