namespace Bit.BlazorUI;

internal static class ListExtensions
{
    /// <summary>
    /// Adds a CSS class to a list that is joined into a class attribute, leaving out one that is empty - the color
    /// and the size classes of a component are empty while their parameters are unset.
    /// </summary>
    internal static void AddIfHasValue(this List<string> classes, string? value)
    {
        if (value.HasValue())
        {
            classes.Add(value!);
        }
    }
}
