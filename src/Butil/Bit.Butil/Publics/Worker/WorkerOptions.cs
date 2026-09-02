namespace Bit.Butil;

/// <summary>Options for <see cref="Worker.Create"/> and <see cref="Worker.CreateShared"/>.</summary>
public class WorkerOptions
{
    /// <summary>
    /// A name for the worker, visible as <c>self.name</c> inside it and in the debugger.
    /// </summary>
    /// <remarks>
    /// Cosmetic for a dedicated worker. For a shared worker it is part of the identity: the script
    /// URL and this name together decide whether two pages reach the same worker or two different
    /// ones.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// True to run the script as an ES module, which is what lets it use <c>import</c>. False (the
    /// default) runs it as a classic script, where <c>importScripts()</c> is the way to load code.
    /// </summary>
    public bool Module { get; set; }

    /// <summary>
    /// How credentials are sent when fetching a module worker's script: <c>"omit"</c>,
    /// <c>"same-origin"</c> (the default) or <c>"include"</c>. Ignored for a classic worker.
    /// </summary>
    public string? Credentials { get; set; }
}
