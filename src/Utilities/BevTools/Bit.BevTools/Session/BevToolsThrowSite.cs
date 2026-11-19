namespace Bit.BevTools;

/// <summary>A method and the IL offset in it an exception passed through: a stack frame without its file and line.</summary>
internal sealed record BevToolsThrowSite(System.Reflection.MethodBase Method, int ILOffset)
{
    /// <summary>
    /// The first frame of application code: System, Microsoft and BevTools' own frames are passed over, as the grouping
    /// does (<see cref="BevToolsErrorGroups.StackAnchor"/>). Null when there is none, or its offset is unknown.
    /// </summary>
    public static BevToolsThrowSite? Of(Exception exception)
    {
        try
        {
            foreach (var frame in new System.Diagnostics.StackTrace(exception, fNeedFileInfo: false).GetFrames())
            {
                if (frame.GetMethod() is not { } method || method.Module.Assembly == typeof(BevToolsThrowSite).Assembly)
                {
                    continue;
                }

                var ns = method.DeclaringType?.Namespace ?? "";
                if (ns is "System" or "Microsoft" || ns.StartsWith("System.", StringComparison.Ordinal) || ns.StartsWith("Microsoft.", StringComparison.Ordinal))
                {
                    continue;
                }

                var offset = frame.GetILOffset();
                return offset == System.Diagnostics.StackFrame.OFFSET_UNKNOWN ? null : new BevToolsThrowSite(method, offset);
            }
        }
        catch
        {
            // A runtime that cannot walk the exception's frames: no location, as before.
        }

        return null;
    }

    /// <summary>"ErrorScenarios.razor:44" once the symbols say where that offset is; null until then.</summary>
    public string? Resolve() => BevToolsSourceLocator.At(Method, ILOffset, null) is { } at ? at.FileName + ":" + at.Line : null;
}
