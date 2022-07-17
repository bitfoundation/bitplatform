using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI;

/// <summary>
/// A delegate for all sorts of mouse events on a chart.
/// </summary>
/// <param name="mouseEvent">The mouse event.</param>
/// <param name="activeElements">The active elements.</param>
public delegate void BitChartChartMouseEvent(JObject mouseEvent, JArray activeElements);
