namespace Bit.Butil;

/// <summary>
/// One name/value pair of a query string, as
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/URLSearchParams">URLSearchParams</see>
/// iterates them.
/// </summary>
/// <remarks>
/// A pair rather than a dictionary entry because a query string may repeat a name - <c>?tag=a&amp;tag=b</c>
/// is two parameters, and a map would drop one of them.
/// </remarks>
public class UrlQueryParameter
{
    /// <summary>Creates an empty parameter. Required for deserialization.</summary>
    public UrlQueryParameter() { }

    /// <summary>Creates a parameter.</summary>
    public UrlQueryParameter(string key, string? value)
    {
        Key = key;
        Value = value ?? string.Empty;
    }

    /// <summary>The parameter's name, percent-decoded.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The parameter's value, percent-decoded. Empty for <c>?flag</c>.</summary>
    public string Value { get; set; } = string.Empty;
}
