using System.Globalization;

namespace Bit.Belemetry;

/// <summary>
/// One filter of a Live Metrics document stream or custom chart (<c>FilterInfo</c>): a field of the item, a predicate and
/// a comparand, with the semantics of the Azure Monitor OpenTelemetry distros - text compares ordinally (<c>Contains</c>
/// ignoring case), <c>Duration</c> and the response or result code as numbers, <c>Success</c> as a boolean,
/// <c>CustomDimensions.{key}</c> a dimension as text, and <c>*</c> any text field or dimension.
/// </summary>
internal sealed class AzureMonitorLiveFilter
{
    /// <summary>The predicates, in the order of their numbers in the older protocol.</summary>
    internal static readonly string[] Predicates = ["Equal", "NotEqual", "LessThan", "GreaterThan", "LessThanOrEqual", "GreaterThanOrEqual", "Contains", "DoesNotContain"];

    private const string DimensionPrefix = "CustomDimensions.";

    private readonly Field _field;
    private readonly string? _dimension;
    private readonly Predicate _predicate;
    private readonly string _comparand;
    private readonly double _number;
    private readonly bool _boolean;

    private AzureMonitorLiveFilter(Field field, string? dimension, Predicate predicate, string comparand, double number = 0, bool boolean = false)
    {
        _field = field;
        _dimension = dimension;
        _predicate = predicate;
        _comparand = comparand;
        _number = number;
        _boolean = boolean;
    }

    private enum Field
    {
        Any, Dimension, Name, Url, ResponseCode, ResultCode, Success, Duration, Target, Type, Data, Message, ExceptionMessage, ExceptionStackTrace, ExceptionType,
    }

    private enum Predicate
    {
        Equal, NotEqual, LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual, Contains, DoesNotContain,
    }

    /// <summary>The filter for items of <paramref name="type"/>.</summary>
    /// <exception cref="FormatException">The field, the predicate or the comparand is not one this filter can apply; the message says why.</exception>
    public static AzureMonitorLiveFilter Create(AzureMonitorLiveTelemetryType type, string? fieldName, string? predicateName, string? comparand)
    {
        if (string.IsNullOrEmpty(fieldName)) throw new FormatException("A filter must have a field name.");

        var index = Array.IndexOf(Predicates, predicateName);
        if (index < 0) throw new FormatException($"'{predicateName}' is not a valid predicate.");
        var predicate = (Predicate)index;

        if (string.IsNullOrEmpty(comparand)) throw new FormatException($"A filter must have a comparand. FieldName: '{fieldName}' Predicate: '{predicateName}'.");

        var textual = predicate is Predicate.Equal or Predicate.NotEqual or Predicate.Contains or Predicate.DoesNotContain;

        if (fieldName == "*")
        {
            if (predicate is not (Predicate.Contains or Predicate.DoesNotContain)) throw new FormatException($"The predicate '{predicateName}' is not supported for the field name '*'.");
            return new AzureMonitorLiveFilter(Field.Any, null, predicate, comparand);
        }

        if (fieldName.StartsWith(DimensionPrefix, StringComparison.Ordinal))
        {
            var key = fieldName[DimensionPrefix.Length..];
            if (key.Length == 0) throw new FormatException("A custom dimension filter must name the dimension.");
            if (textual is false) throw new FormatException($"The predicate '{predicateName}' is not supported for the field name '{fieldName}': a custom dimension is text.");
            return new AzureMonitorLiveFilter(Field.Dimension, key, predicate, comparand);
        }

        if (fieldName.StartsWith("CustomMetrics.", StringComparison.Ordinal)) throw new FormatException("Filtering on a custom metric is not supported.");

        var field = Resolve(type, fieldName) ?? throw new FormatException($"'{fieldName}' is not a valid field name for the telemetry type {type}.");

        switch (field)
        {
            case Field.Success:
                if (predicate is not (Predicate.Equal or Predicate.NotEqual)) throw new FormatException($"The predicate '{predicateName}' is not supported for the field name '{fieldName}'.");
                if (bool.TryParse(comparand, out var boolean) is false) throw new FormatException($"The comparand '{comparand}' is not a valid boolean value for the field name '{fieldName}'.");
                return new AzureMonitorLiveFilter(field, null, predicate, comparand, boolean: boolean);

            case Field.Duration:
                if (predicate is Predicate.Contains or Predicate.DoesNotContain) throw new FormatException($"The predicate '{predicateName}' is not supported for the field name '{fieldName}'.");
                if (TryParseDuration(comparand, out var milliseconds) is false) throw new FormatException($"The comparand '{comparand}' can't be converted to a duration (d.hh:mm:ss).");
                return new AzureMonitorLiveFilter(field, null, predicate, comparand, number: milliseconds);

            case Field.ResponseCode or Field.ResultCode:
                if (predicate is Predicate.Contains or Predicate.DoesNotContain) throw new FormatException($"The predicate '{predicateName}' is not supported for the field name '{fieldName}'.");
                if (TryParseNumber(comparand, out var number) is false) throw new FormatException($"The comparand '{comparand}' can't be converted to a number.");
                return new AzureMonitorLiveFilter(field, null, predicate, comparand, number: number);

            default:
                if (textual is false) throw new FormatException($"The predicate '{predicateName}' is not supported for the field name '{fieldName}': it is text.");
                return new AzureMonitorLiveFilter(field, null, predicate, comparand);
        }
    }

    public bool Matches(AzureMonitorLiveItem item)
    {
        switch (_field)
        {
            case Field.Any:
                var found = false;
                foreach (var value in item.Values())
                {
                    if (value.Contains(_comparand, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                // DoesNotContain: in none of them - not merely missing from one.
                return found == (_predicate == Predicate.Contains);

            case Field.Dimension:
                // A missing dimension matches no predicate, as in the distros.
                return item.TryGetDimension(_dimension!, out var dimension) && CompareText(dimension);

            case Field.Success:
                return (item.Success == _boolean) == (_predicate == Predicate.Equal);

            case Field.Duration:
                return CompareNumber(item.Duration.TotalMilliseconds);

            case Field.ResponseCode or Field.ResultCode:
                // A code that is no number (none at all, or a database's) equals no number.
                return TryParseNumber(item.Code, out var code) ? CompareNumber(code) : _predicate == Predicate.NotEqual;

            default:
                return CompareText(_field switch
                {
                    Field.Name => item.Name,
                    Field.Url => item.Url,
                    Field.Target => item.Target,
                    Field.Type => item.DependencyType,
                    Field.Data => item.Data,
                    Field.Message => item.Message,
                    Field.ExceptionMessage => item.ExceptionMessage,
                    Field.ExceptionStackTrace => item.StackTrace,
                    _ => item.ExceptionType,
                });
        }
    }

    /// <summary>
    /// A duration comparand, in milliseconds. The service sends <c>[days].[hours]:[minutes]:[seconds]</c> with components
    /// of any length (<c>0.0:0:0.2</c> is 200 ms, <c>14.6:56:7.89</c> is 1234567890 ms); the days may be left out.
    /// </summary>
    internal static bool TryParseDuration(string value, out double milliseconds)
    {
        milliseconds = 0;
        var parts = value.Split(':');
        if (parts.Length != 3) return false;

        var dayHour = parts[0].Split('.');
        if (dayHour.Length > 2) return false;

        double days = 0;
        if (dayHour.Length == 2 && TryParseNumber(dayHour[0], out days) is false) return false;
        if (TryParseNumber(dayHour[^1], out var hours) is false || TryParseNumber(parts[1], out var minutes) is false || TryParseNumber(parts[2], out var seconds) is false) return false;

        milliseconds = days * 86_400_000 + hours * 3_600_000 + minutes * 60_000 + seconds * 1000;
        return true;
    }

    private static bool TryParseNumber(string value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);

    private static Field? Resolve(AzureMonitorLiveTelemetryType type, string name) => type switch
    {
        AzureMonitorLiveTelemetryType.Request => name switch
        {
            "Name" => Field.Name,
            "Url" => Field.Url,
            "ResponseCode" => Field.ResponseCode,
            "Success" => Field.Success,
            "Duration" => Field.Duration,
            _ => null,
        },
        AzureMonitorLiveTelemetryType.Dependency => name switch
        {
            "Name" => Field.Name,
            "Target" => Field.Target,
            "Type" => Field.Type,
            "Data" => Field.Data,
            "ResultCode" => Field.ResultCode,
            "Success" => Field.Success,
            "Duration" => Field.Duration,
            _ => null,
        },
        AzureMonitorLiveTelemetryType.Exception => name switch
        {
            "Exception.Message" => Field.ExceptionMessage,
            "Exception.StackTrace" => Field.ExceptionStackTrace,
            "Exception.Type" => Field.ExceptionType,
            _ => null,
        },
        AzureMonitorLiveTelemetryType.Event => name == "Name" ? Field.Name : null,
        _ => name == "Message" ? Field.Message : null,
    };

    private bool CompareText(string value) => _predicate switch
    {
        Predicate.Equal => value == _comparand,
        Predicate.NotEqual => value != _comparand,
        Predicate.Contains => value.Contains(_comparand, StringComparison.OrdinalIgnoreCase),
        _ => value.Contains(_comparand, StringComparison.OrdinalIgnoreCase) is false,
    };

    private bool CompareNumber(double value) => _predicate switch
    {
        Predicate.Equal => value == _number,
        Predicate.NotEqual => value != _number,
        Predicate.LessThan => value < _number,
        Predicate.GreaterThan => value > _number,
        Predicate.LessThanOrEqual => value <= _number,
        _ => value >= _number,
    };
}
