namespace Bit.BlazeDb.Demo.Client.Components;

/// <summary>
/// The demo's tones - <c>info</c>, <c>accent</c>, <c>ok</c>, <c>warn</c>, <c>err</c> and none - as the site
/// kit's classes. The pages decide a tone from what the engine did (a scenario's outcome, a file's role, a
/// benchmark's speed); this is the one place that says what each tone looks like.
/// </summary>
public static class Tones
{
    /// <summary>A note (Fluent's MessageBar) in the tone.</summary>
    public static string Note(string? tone) => tone switch
    {
        "ok" => "bb-note bb-note-success",
        "warn" => "bb-note bb-note-warn",
        "err" => "bb-note bb-note-danger",
        _ => "bb-note",
    };

    /// <summary>A badge in the tone; no tone is the neutral one.</summary>
    public static string Badge(string? tone) => tone switch
    {
        "ok" => "bb-badge bb-badge-success",
        "warn" => "bb-badge bb-badge-warn",
        "err" => "bb-badge bb-badge-danger",
        "info" => "bb-badge",
        "accent" => "bb-badge bd-badge-accent",
        _ => "bb-badge bd-badge-neutral",
    };
}
