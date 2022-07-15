using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI;

/// <summary>
/// A delegate for all sorts of mouse events on a legend item.
/// </summary>
/// <param name="mouseEvent">The mouse event.</param>
/// <param name="legendItem">The affected legend item.</param>
public delegate void LegendItemMouseEvent(JObject mouseEvent, LegendItem legendItem);
