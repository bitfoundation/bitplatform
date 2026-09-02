namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>
/// What using one Butil API entails, beyond the signatures of its members.
/// <para>
/// Data about the API, and nothing that reads as advice: the prose telling a caller what to do
/// about any of it is the plan's <see cref="ButilFeaturePlanDto.Checklist"/>, written once for the
/// whole set and naming the APIs each item applies to. This carried its own copy of that advice
/// once - the same paragraph about prerendering, permissions and disposal repeated per API, in a
/// plan that then said all of it again in the checklist, about a client that had already read the
/// same rules in the server's instructions. Four copies of a paragraph is four times the tokens and
/// no more likely to be followed.
/// </para>
/// </summary>
public record ButilApiInspectionDto
{
    /// <summary>The name that was looked up.</summary>
    public required string Query { get; init; }

    /// <summary>False when nothing in Bit.Butil goes by that name - Message then says what to try.</summary>
    public required bool IsKnown { get; init; }

    public string? Message { get; init; }

    /// <summary>The documented API this resolved to, e.g. "Clipboard".</summary>
    public string? Api { get; init; }

    /// <summary>The Bit.Butil types behind it.</summary>
    public string[]? Services { get; init; }

    /// <summary>The injection lines to put in a component, one per injectable service behind the API.</summary>
    public string[]? Inject { get; init; }

    public string? BrowserSupport { get; init; }

    /// <summary>The preconditions the calling page has to satisfy before the call can succeed.</summary>
    public string[]? Requires { get; init; }

    /// <summary>Members whose result has to be disposed - a subscription or a handle on real hardware.</summary>
    public string[]? Disposables { get; init; }

    /// <summary>The follow-up calls that return the full text: the API reference and the docs page.</summary>
    public string[]? NextCalls { get; init; }
}
