using System.Text;

namespace Bit.Minifier;

/// <summary>
/// The one thing both programs of this package do: Bit.Minifier, which an app installs to be minified on
/// publish, and Bit.Minifier.Cli, which whoever holds a map runs. One implementation, compiled into both, so
/// what they read a stack trace back into can never drift apart.
/// </summary>
internal static class Decode
{
    /// <summary><paramref name="args"/> is <c>--decode &lt;map file&gt; [&lt;stack trace file&gt;]</c>.</summary>
    public static int Run(string usage, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            Console.Error.WriteLine(usage);
            return 2;
        }
        var mapFile = args[1];
        if (File.Exists(mapFile) is false)
        {
            Console.Error.WriteLine($"Bit.Minifier: no map at {Path.GetFullPath(mapFile)}. It is written next to the assemblies a publish minifies, as obj/<configuration>/<tfm>/bit-minifier.map, and only kept until the next clean.");
            return 2;
        }
        if (args.Length == 3 && File.Exists(args[2]) is false)
        {
            Console.Error.WriteLine($"Bit.Minifier: no stack trace at {Path.GetFullPath(args[2])}.");
            return 2;
        }

        // A name is any word C# lets one be, in any script, and the map is read as UTF-8. Redirected, the console
        // would write those names in whatever code page it was started with, so say what they are written in -
        // after the checks above, since this replaces the console's writers.
        try { Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); }
        catch (Exception e) when (e is IOException or PlatformNotSupportedException) { /* a console that won't be told keeps what it has */ }

        var trace = args.Length == 3 ? File.ReadAllText(args[2]) : Console.In.ReadToEnd();
        // the trace is written back the way it came in, with whatever ends its lines
        var decoded = MapDecoder.Load(mapFile).Decode(trace);
        Console.Out.Write(decoded);
        if (decoded.EndsWith('\n') is false) Console.Out.WriteLine();
        return 0;
    }
}
