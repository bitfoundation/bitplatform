using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blow;

/// <summary>
/// What every reader of graph JSON shares: the issues found so far, the limits, and readers for the values the schema
/// is made of, each of which reports a value of the wrong kind at its path and carries on rather than throwing.
/// </summary>
internal abstract class BlowJsonReader(BlowImportLimits limits)
{
    protected readonly List<BlowImportIssue> _issues = [];
    protected readonly BlowImportLimits _limits = limits;

    protected void Issue(string code, BlowValidationSeverity severity, string path, string message)
        => _issues.Add(new BlowImportIssue(code, severity, path, message));

    protected void Error(string code, string path, string message) => Issue(code, BlowValidationSeverity.Error, path, message);

    protected BlowEdgePathOptions? PathOptions(JsonElement e, string path)
    {
        if (e.ValueKind != JsonValueKind.Object) return Invalid<BlowEdgePathOptions?>(path, "\"pathOptions\" has to be an object.");
        var options = new BlowEdgePathOptions();
        foreach (var p in e.EnumerateObject())
        {
            var at = $"{path}.{p.Name}";
            switch (p.Name)
            {
                case "curvature": options.Curvature = Number(p.Value, at); break;
                case "borderRadius": options.BorderRadius = Number(p.Value, at); break;
                case "offset": options.Offset = Number(p.Value, at); break;
                case "stepPosition": options.StepPosition = Number(p.Value, at); break;
                case "loopRadius": options.LoopRadius = Number(p.Value, at); break;
                default: Unknown(at); break;
            }
        }
        return options;
    }

    protected BlowViewport? Viewport(JsonElement e, string path)
    {
        if (e.ValueKind != JsonValueKind.Object) return Invalid<BlowViewport?>(path, "\"viewport\" has to be an object.");
        var x = e.TryGetProperty("x", out var vx) ? Number(vx, path + ".x") : 0;
        var y = e.TryGetProperty("y", out var vy) ? Number(vy, path + ".y") : 0;
        var zoom = e.TryGetProperty("zoom", out var vz) ? Number(vz, path + ".zoom") : 1;
        if (x is null || y is null || zoom is null) return null;
        if (zoom <= 0) return Invalid<BlowViewport?>(path + ".zoom", "The zoom has to be greater than zero.");
        return new BlowViewport(x.Value, y.Value, zoom.Value);
    }


    protected object? Data(JsonElement value, string path, Func<JsonElement, object?>? reader)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        if (reader is null) return value.Clone();

        try
        {
            return reader(value);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or FormatException)
        {
            Issue(BlowImportCodes.DataNotRead, BlowValidationSeverity.Warning, path,
                $"The data could not be read into its mapped type and was kept as JSON: {ex.Message}");
            return value.Clone();
        }
    }

    protected string? RequiredString(JsonElement e, string name, string path)
    {
        if (!e.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            Error(BlowImportCodes.MissingProperty, path, $"It has no \"{name}\" string, so it was left out.");
            return null;
        }

        var text = String(value, $"{path}.{name}");
        if (string.IsNullOrEmpty(text))
        {
            Error(BlowImportCodes.MissingProperty, $"{path}.{name}", $"\"{name}\" cannot be empty, so the element was left out.");
            return null;
        }
        return text;
    }

    protected string? String(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String) return Invalid<string?>(path, "Expected a string.");

        var text = value.GetString()!;
        if (text.Length > _limits.MaxStringLength)
        {
            Error(BlowImportCodes.StringTooLong, path, $"The string is {text.Length} characters long; at most {_limits.MaxStringLength} are read.");
            return null;
        }
        return text;
    }

    protected bool? Bool(JsonElement value, string path) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => Invalid<bool?>(path, "Expected true or false."),
    };

    protected double? Number(JsonElement value, string path)
        => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var d) && double.IsFinite(d)
            ? d
            : Invalid<double?>(path, "Expected a finite number.");

    protected double? Size(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        var size = Number(value, path);
        return size < 0 ? Invalid<double?>(path, "A size cannot be negative.") : size;
    }

    protected int? Int(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Null) return null;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i) ? i : Invalid<int?>(path, "Expected a whole number.");
    }

    protected BlowXYPosition? Point(JsonElement e, string path)
    {
        if (e.ValueKind != JsonValueKind.Object) return Invalid<BlowXYPosition?>(path, "Expected an object with \"x\" and \"y\".");
        if (!e.TryGetProperty("x", out var x) || !e.TryGetProperty("y", out var y))
            return Invalid<BlowXYPosition?>(path, "Expected an object with \"x\" and \"y\".");
        return Number(x, path + ".x") is { } px && Number(y, path + ".y") is { } py ? new BlowXYPosition(px, py) : null;
    }

    protected BlowRect? Rect(JsonElement e, string path)
    {
        if (e.ValueKind != JsonValueKind.Object) return Invalid<BlowRect?>(path, "Expected an object with \"x\", \"y\", \"width\" and \"height\".");
        double? Get(string name) => e.TryGetProperty(name, out var v) ? Number(v, $"{path}.{name}") : Invalid<double?>($"{path}.{name}", "Missing.");
        var (x, y, w, h) = (Get("x"), Get("y"), Get("width"), Get("height"));
        if (x is null || y is null || w is null || h is null) return null;
        if (w < 0 || h < 0) return Invalid<BlowRect?>(path, "A rectangle cannot have a negative size.");
        return new BlowRect(x.Value, y.Value, w.Value, h.Value);
    }

    protected BlowPosition? Position(JsonElement value, string path) => Lower(value) switch
    {
        "left" => BlowPosition.Left,
        "top" => BlowPosition.Top,
        "right" => BlowPosition.Right,
        "bottom" => BlowPosition.Bottom,
        _ => Invalid<BlowPosition?>(path, "Expected \"left\", \"top\", \"right\" or \"bottom\"."),
    };

    protected BlowHandleType? HandleType(JsonElement value, string path) => Lower(value) switch
    {
        "source" => BlowHandleType.Source,
        "target" => BlowHandleType.Target,
        _ => Invalid<BlowHandleType?>(path, "Expected \"source\" or \"target\"."),
    };

    protected BlowEdgeType? EdgeType(JsonElement value, string path) => Lower(value) switch
    {
        "bezier" or "default" => BlowEdgeType.Bezier,
        "straight" => BlowEdgeType.Straight,
        "step" => BlowEdgeType.Step,
        "smoothstep" => BlowEdgeType.SmoothStep,
        "simplebezier" => BlowEdgeType.SimpleBezier,
        _ => Invalid<BlowEdgeType?>(path, "Expected \"bezier\", \"straight\", \"step\", \"smoothstep\" or \"simplebezier\"."),
    };

    protected BlowMarkerType? MarkerType(JsonElement value, string path) => Lower(value) switch
    {
        "arrow" => BlowMarkerType.Arrow,
        "arrowclosed" => BlowMarkerType.ArrowClosed,
        _ => Invalid<BlowMarkerType?>(path, "Expected \"arrow\" or \"arrowclosed\"."),
    };

    protected static string? Lower(JsonElement value)
        => value.ValueKind == JsonValueKind.String ? value.GetString()!.ToLowerInvariant() : null;

    protected T Invalid<T>(string path, string message)
    {
        Error(BlowImportCodes.InvalidValue, path, message + " The value was ignored.");
        return default!;
    }

    protected void Unknown(string path)
        => Issue(BlowImportCodes.UnknownProperty, BlowValidationSeverity.Info, path, "Unknown property, ignored.");

    /// <summary>A copy of a JSON value that does not depend on the document it was read from.</summary>
    protected JsonNode? Detach(JsonElement value)
        => JsonNode.Parse(value.GetRawText(), null, new JsonDocumentOptions { MaxDepth = _limits.MaxDepth + 1 });
}
