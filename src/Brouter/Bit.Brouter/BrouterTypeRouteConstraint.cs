namespace Bit.Brouter;

/// <summary>
/// A route constraint that requires the value to be parseable as a specified type.
/// </summary>
/// <typeparam name="T">The type to which the value must be parseable.</typeparam>
public class BrouterTypeRouteConstraint<T> : BrouterRouteConstraint
{
    private readonly TryParseDelegate _parser;

    public delegate bool TryParseDelegate(string value, out T result);

    public BrouterTypeRouteConstraint(TryParseDelegate parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public override bool TryMatch(string segment, out object? convertedValue)
    {
        if (_parser(segment, out var result))
        {
            convertedValue = result;
            return true;
        }

        convertedValue = null;
        return false;
    }
}
