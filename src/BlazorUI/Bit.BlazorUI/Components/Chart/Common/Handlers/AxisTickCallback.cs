using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI;

/// <summary>
/// A delegate used for customizing tick marks on an axis.
/// If this callback returns <see langword="null"/> (or undefined), the associated grid line will be hidden.
/// </summary>
/// <param name="value">The value. It's usually a <see cref="double"/>
/// but can also be other types like <see cref="string"/> (e.g. in a category axis).</param>
/// <param name="index">The index of the tick mark.</param>
/// <param name="values">An array of labels. Normally those are just strings but in a <see cref="Axes.TimeAxis"/>
/// they have a 'label' (<see cref="string"/>) and a 'major' (<see cref="bool"/>) field.</param>
/// <returns>The new value of the tick mark or <see langword="null"/> if you want to hide that grid line.</returns>
public delegate string AxisTickCallback(JValue value, int index, JArray values);
