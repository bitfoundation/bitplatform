namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>
/// The icons the demo pages put on their buttons: the inner markup of a 24×24 icon, without the
/// <c>&lt;svg&gt;</c> element around it. <see cref="Icon"/> supplies that, along with the class
/// demo.css strokes them through.
/// <para>
/// They carry no colour of their own: <c>svg.icon</c> is stroked with <c>currentColor</c>, so an
/// icon is the colour of the button it sits in and follows it through every theme, hover and
/// disabled state. The site's chrome draws its own icons (Layout/); these are the samples' only.
/// </para>
/// </summary>
public static class Icons
{
    /// <summary>Run a sample.</summary>
    public const string Play = """<path d="M7.75 4.75 19 12 7.75 19.25Z" />""";

    /// <summary>Save a result to a file.</summary>
    public const string Download = """<path d="M12 3.75v11.5m-4.75-4.5L12 15.5l4.75-4.75" /><path d="M4.25 16.5v1.75a2 2 0 0 0 2 2h11.5a2 2 0 0 0 2-2V16.5" />""";

    /// <summary>Measure: a gauge.</summary>
    public const string Gauge = """<path d="M4.25 17.5a8.25 8.25 0 1 1 15.5 0" /><path d="m12 13.5 4-4.25" /><circle cx="12" cy="13.5" r="1.25" />""";

    /// <summary>Search.</summary>
    public const string Search = """<circle cx="11" cy="11" r="7" /><path d="m16.2 16.2 4.3 4.3" />""";
}
