using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Console(IJSRuntime js)
{
    /// <summary>
    /// Log a message and stack trace to console if the first argument is false.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/assert_static">https://developer.mozilla.org/en-US/docs/Web/API/console/assert_static</see>
    /// </summary>
    public async Task Assert(bool? condition, params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.assert", [condition, .. args]);

    /// <summary>
    /// Clear the console.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/clear_static">https://developer.mozilla.org/en-US/docs/Web/API/console/clear_static</see>
    /// </summary>
    public async Task Clear()
        => await js.FastInvokeVoidAsync("BitButil.console.clear");

    /// <summary>
    /// Log the number of times this line has been called with the given label.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/count_static">https://developer.mozilla.org/en-US/docs/Web/API/console/count_static</see>
    /// </summary>
    public async Task Count(string? label = null)
        => await (label is null ? js.FastInvokeVoidAsync("BitButil.console.count")
                                : js.FastInvokeVoidAsync("BitButil.console.count", label));

    /// <summary>
    /// Resets the value of the counter with the given label.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/countreset_static">https://developer.mozilla.org/en-US/docs/Web/API/console/countreset_static</see>
    /// </summary>
    public async Task CountReset(string? label = null)
        => await (label is null ? js.FastInvokeVoidAsync("BitButil.console.countReset")
                                : js.FastInvokeVoidAsync("BitButil.console.countReset", label));

    /// <summary>
    /// Outputs a message to the console with the log level debug.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/debug_static">https://developer.mozilla.org/en-US/docs/Web/API/console/debug_static</see>
    /// </summary>
    public async Task Debug(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.debug", args);

    /// <summary>
    /// Displays an interactive listing of the properties of a specified JavaScript object. 
    /// This listing lets you use disclosure triangles to examine the contents of child objects.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/dir_static">https://developer.mozilla.org/en-US/docs/Web/API/console/dir_static</see>
    /// </summary>
    public async Task Dir(object? item, object? options = null)
        => await js.FastInvokeVoidAsync("BitButil.console.dir", item, options);

    /// <summary>
    /// Displays an XML/HTML Element representation of the specified object if possible 
    /// or the JavaScript Object view if it is not possible.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/dirxml_static">https://developer.mozilla.org/en-US/docs/Web/API/console/dirxml_static</see>
    /// </summary>
    public async Task Dirxml(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.dirxml", args);

    /// <summary>
    /// Outputs an error message. You may use string substitution and additional arguments with this method.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/error_static">https://developer.mozilla.org/en-US/docs/Web/API/console/error_static</see>
    /// </summary>
    public async Task Error(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.error", args);

    /// <summary>
    /// Creates a new inline group, indenting all following output by another level. 
    /// To move back out a level, call console.groupEnd().
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/group_static">https://developer.mozilla.org/en-US/docs/Web/API/console/group_static</see>
    /// </summary>
    public async Task Group(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.group", args);

    /// <summary>
    /// Creates a new inline group, indenting all following output by another level. However, unlike console.group() 
    /// this starts with the inline group collapsed requiring the use of a disclosure button to expand it. 
    /// To move back out a level, call console.groupEnd().
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/groupcollapsed_static">https://developer.mozilla.org/en-US/docs/Web/API/console/groupcollapsed_static</see>
    /// </summary>
    public async Task GroupCollapsed(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.groupCollapsed", args);

    /// <summary>
    /// Exits the current inline group.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/groupend_static">https://developer.mozilla.org/en-US/docs/Web/API/console/groupend_static</see>
    /// </summary>
    public async Task GroupEnd()
        => await js.FastInvokeVoidAsync("BitButil.console.groupEnd");

    /// <summary>
    /// Informative logging of information. You may use string substitution and additional arguments with this method.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/info_static">https://developer.mozilla.org/en-US/docs/Web/API/console/info_static</see>
    /// </summary>
    public async Task Info(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.info", args);

    /// <summary>
    /// For general output of logging information. You may use string substitution and additional arguments with this method.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/log_static">https://developer.mozilla.org/en-US/docs/Web/API/console/log_static</see>
    /// </summary>
    public async Task Log(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.log", args);

    /// <summary>
    /// Starts the browser's built-in profiler (for example, the Firefox performance tool). 
    /// You can specify an optional name for the profile.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/profile_static">https://developer.mozilla.org/en-US/docs/Web/API/console/profile_static</see>
    /// </summary>
    public async Task Profile(string? name = null)
        => await (name is null ? js.FastInvokeVoidAsync("BitButil.console.profile")
                               : js.FastInvokeVoidAsync("BitButil.console.profile", name));

    /// <summary>
    /// Stops the profiler. You can see the resulting profile in the browser's performance tool 
    /// (for example, the Firefox performance tool).
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/profileend_static">https://developer.mozilla.org/en-US/docs/Web/API/console/profileend_static</see>
    /// </summary>
    public async Task ProfileEnd(string? name = null)
        => await (name is null ? js.FastInvokeVoidAsync("BitButil.console.profileEnd")
                               : js.FastInvokeVoidAsync("BitButil.console.profileEnd", name));

    /// <summary>
    /// Displays tabular data as a table.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/table_static">https://developer.mozilla.org/en-US/docs/Web/API/console/table_static</see>
    /// </summary>
    public async Task Table(object? data, object? properties = null)
        => await (properties is null ? js.FastInvokeVoidAsync("BitButil.console.table", data)
                                     : js.FastInvokeVoidAsync("BitButil.console.table", data, properties));

    /// <summary>
    /// Starts a timer with a name specified as an input parameter. Up to 10,000 simultaneous timers can run on a given page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/time_static">https://developer.mozilla.org/en-US/docs/Web/API/console/time_static</see>
    /// </summary>
    public async Task Time(string? label = null)
        => await (label is null ? js.FastInvokeVoidAsync("BitButil.console.time")
                                : js.FastInvokeVoidAsync("BitButil.console.time", label));

    /// <summary>
    /// Stops the specified timer and logs the elapsed time in milliseconds since it started.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/timeend_static">https://developer.mozilla.org/en-US/docs/Web/API/console/timeend_static</see>
    /// </summary>
    public async Task TimeEnd(string? label = null)
        => await (label is null ? js.FastInvokeVoidAsync("BitButil.console.timeEnd")
                                : js.FastInvokeVoidAsync("BitButil.console.timeEnd", label));

    /// <summary>
    /// Logs the value of the specified timer to the console.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/timelog_static">https://developer.mozilla.org/en-US/docs/Web/API/console/timelog_static</see>
    /// </summary>
    public async Task TimeLog(string? label = null, params object?[]? args)
        => await (label is null ? js.FastInvokeVoidAsync("BitButil.console.timeLog")
                                : js.FastInvokeVoidAsync("BitButil.console.timeLog", [label, .. args]));

    /// <summary>
    /// Adds a marker to the browser performance tool's timeline.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/timestamp_static">https://developer.mozilla.org/en-US/docs/Web/API/console/timestamp_static</see>
    /// </summary>
    public async Task TimeStamp(string? label = null)
        => await (label is null ? js.FastInvokeVoidAsync("BitButil.console.timeStamp")
                                : js.FastInvokeVoidAsync("BitButil.console.timeStamp", label));

    /// <summary>
    /// Outputs a stack trace.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/trace_static">https://developer.mozilla.org/en-US/docs/Web/API/console/trace_static</see>
    /// </summary>
    public async Task Trace(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.trace", args);

    /// <summary>
    /// Outputs a warning message.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console/warn_static">https://developer.mozilla.org/en-US/docs/Web/API/console/warn_static</see>
    /// </summary>
    public async Task Warn(params object?[]? args)
        => await js.FastInvokeVoidAsync("BitButil.console.warn", args);
}
