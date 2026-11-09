using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Bit.Belemetry.Tests.Sentry;

[TestClass]
public class SentryStackTraceTests
{
    // The same cases as sentry.test.js: the stacks the browser relays to .NET are parsed as the script parses them.
    private const string V8 = """
        TypeError: x is undefined
            at render (https://shop.test/app.js:10:15)
            at https://shop.test/app.js:20:5
            at async load (https://shop.test/app.js?v=3:30:1)
            at new Cart (https://shop.test/cart.js:40:2)
            at Array.map (<anonymous>)
            at eval (eval at run (https://shop.test/app.js:1:1), <anonymous>:1:1)
            at https://shop.test/_framework/blazor.web.js:1:5000
        """;

    private const string Gecko = """
        render@https://shop.test/app.js:10:15
        async*load@https://shop.test/app.js:30:1
        @https://shop.test/app.js:20:5
        run@https://shop.test/app.js line 2 > eval:1:1
        lib@https://cdn.test/npm/pkg@1.2.3/x.js:3:4
        ext@moz-extension://abc/content.js:1:1
        """;

    private const string WebKit = """
        render@https://shop.test/app.js:10:15
        map@[native code]
        global code@app://0.0.0.1/app.js:1:1
        Error: mail me@example.com
        """;

    private static object?[] Row(SentryStackTrace.Frame f) => [f.Function, f.FileName, f.Line, f.Column, f.InApp];

    private static void AssertFrames(IEnumerable<SentryStackTrace.Frame> frames, params object?[][] expected)
    {
        var actual = frames.Select(Row).ToList();
        Assert.HasCount(expected.Length, actual, string.Join(" | ", actual.Select(r => string.Join(", ", r))));
        for (var i = 0; i < expected.Length; i++) CollectionAssert.AreEqual(expected[i], actual[i], $"frame {i}: {string.Join(", ", actual[i])}");
    }

    [TestMethod]
    public void JavaScript_V8_OldestFrameFirst()
    {
        var frames = SentryStackTrace.ParseJavaScript(V8);
        AssertFrames(frames,
            ["?", "https://shop.test/_framework/blazor.web.js", 1, 5000, false],
            ["eval", "https://shop.test/app.js", 1, 1, true],
            ["Array.map", "<anonymous>", null, null, false],
            ["new Cart", "https://shop.test/cart.js", 40, 2, true],
            ["load", "https://shop.test/app.js?v=3", 30, 1, true],
            ["?", "https://shop.test/app.js", 20, 5, true],
            ["render", "https://shop.test/app.js", 10, 15, true]);
        Assert.IsTrue(frames.All(f => f.AbsolutePath == f.FileName && f.Module is null && f.FunctionId is null));
    }

    [TestMethod]
    public void JavaScript_SpiderMonkey_AsyncFrames_Eval_AndAnAtInTheUrl()
    {
        var frames = SentryStackTrace.ParseJavaScript(Gecko);
        frames.Reverse();
        AssertFrames(frames,
            ["render", "https://shop.test/app.js", 10, 15, true],
            ["load", "https://shop.test/app.js", 30, 1, true],
            ["?", "https://shop.test/app.js", 20, 5, true],
            ["run", "https://shop.test/app.js", 2, null, true],
            ["lib", "https://cdn.test/npm/pkg@1.2.3/x.js", 3, 4, true],
            ["ext", "moz-extension://abc/content.js", 1, 1, false]);
    }

    [TestMethod]
    public void JavaScript_JavaScriptCore_SkipsALineThatOnlyHoldsAnAt()
    {
        var frames = SentryStackTrace.ParseJavaScript(WebKit);
        frames.Reverse();
        AssertFrames(frames,
            ["render", "https://shop.test/app.js", 10, 15, true],
            ["map", "[native code]", null, null, false],
            ["global code", "app://0.0.0.1/app.js", 1, 1, true]);
        Assert.IsNull(SentryStackTrace.JavaScriptFrame("Error: mail me@example.com"));
        Assert.IsNull(SentryStackTrace.JavaScriptFrame("TypeError: x is undefined"));
    }

    [TestMethod]
    public void JavaScript_KeepsTheNewestFramesOfADeepStack()
    {
        var stack = string.Join('\n', Enumerable.Range(0, SentryStackTrace.MaxFrames + 20).Select(i => $"    at f{i} (https://shop.test/app.js:{i + 1}:1)"));
        var frames = SentryStackTrace.ParseJavaScript(stack);
        Assert.HasCount(SentryStackTrace.MaxFrames, frames);
        Assert.AreEqual("f0", frames[^1].Function);
    }

    private const string DotnetText = """
        System.InvalidOperationException: outer failed
         ---> System.ArgumentException: inner failed
           at Shop.Orders.Inner() in /src/Shop/Orders.cs:line 12
           at Shop.Orders.Outer() in C:\src\Shop\Orders.cs:line 20
           --- End of inner exception stack trace ---
           at Shop.Orders.Outer() in /src/Shop/Orders.cs:line 24
           at Shop.Orders.<RunAsync>d__3.MoveNext()
        --- End of stack trace from previous location ---
           at Shop.Orders.Load[T](String id)
           at Shop.Orders..ctor()
           at Shop.Orders.<>c.<Start>b__4_0()
           at System.Threading.Tasks.Task.Run()
        """;

    [TestMethod]
    public void Dotnet_Text_TheChainOutermostFirst_FramesOldestFirst()
    {
        var chain = SentryStackTrace.ParseDotnet(DotnetText);
        Assert.HasCount(2, chain);

        Assert.AreEqual("System.InvalidOperationException", chain[0].Type);
        Assert.AreEqual("outer failed", chain[0].Message);
        CollectionAssert.AreEqual(
            new[] { "System.Threading.Tasks.Task.Run", "Shop.Orders.<>c.<Start>b__4_0", "Shop.Orders..ctor", "Shop.Orders.Load", "Shop.Orders.RunAsync", "Shop.Orders.Outer" },
            chain[0].Frames.Select(f => f.Module + "." + f.Function).ToArray());
        Assert.IsFalse(chain[0].Frames[0].InApp, "Framework code.");
        Assert.IsTrue(chain[0].Frames[1].InApp);
        var outer = chain[0].Frames[^1];
        Assert.AreEqual("Orders.cs", outer.FileName);
        Assert.AreEqual("/src/Shop/Orders.cs", outer.AbsolutePath);
        Assert.AreEqual(24, outer.Line);

        Assert.AreEqual("System.ArgumentException", chain[1].Type);
        Assert.AreEqual("inner failed", chain[1].Message);
        CollectionAssert.AreEqual(new[] { "Outer", "Inner" }, chain[1].Frames.Select(f => f.Function).ToArray());
        Assert.AreEqual("Orders.cs", chain[1].Frames[0].FileName, "A Windows path too.");
        Assert.AreEqual(12, chain[1].Frames[1].Line);
    }

    [TestMethod]
    public void Dotnet_Text_IsToldFromJavaScriptByItsLines()
    {
        Assert.IsTrue(SentryStackTrace.IsDotnet(DotnetText));
        Assert.IsTrue(SentryStackTrace.IsDotnet(SentryStackTraceTests.Thrower.Catch(SentryStackTraceTests.Thrower.Wrapped).ToString()));
        foreach (var stack in new[] { V8, Gecko, WebKit, "", null }) Assert.IsFalse(SentryStackTrace.IsDotnet(stack), stack);
    }

    [TestMethod]
    public void Dotnet_Text_AnAggregateExceptionsOtherInnerExceptions_AreNotTheChains()
    {
        const string text = """
            System.AggregateException: One or more errors occurred. (a) (b)
             ---> System.Exception: a
               at X.Y.A()
               --- End of inner exception stack trace ---
               at X.Y.Outer()
             ---> (Inner Exception #1) System.Exception: b
               at X.Y.B()<---
            """;

        var chain = SentryStackTrace.ParseDotnet(text);
        Assert.HasCount(2, chain);
        Assert.AreEqual("System.AggregateException", chain[0].Type);
        CollectionAssert.AreEqual(new[] { "Outer" }, chain[0].Frames.Select(f => f.Function).ToArray());
        Assert.AreEqual("a", chain[1].Message);
        CollectionAssert.AreEqual(new[] { "A" }, chain[1].Frames.Select(f => f.Function).ToArray());
    }

    [TestMethod]
    public void Dotnet_Text_WithoutFrames_OrWithoutAMessage()
    {
        var never = SentryStackTrace.ParseDotnet("System.Exception: never thrown").Single();
        Assert.AreEqual("System.Exception", never.Type);
        Assert.AreEqual("never thrown", never.Message);
        Assert.IsEmpty(never.Frames);

        var bare = SentryStackTrace.ParseDotnet("System.Exception\n   at X.Y.Z()").Single();
        Assert.AreEqual("System.Exception", bare.Type);
        Assert.IsNull(bare.Message);
        Assert.AreEqual("Z", bare.Frames.Single().Function);

        Assert.AreEqual("Main", SentryStackTrace.DotnetFrame("at App.Program.Main(String[] args) + 0x5a")!.Function, "NativeAOT's format.");
        Assert.IsNull(SentryStackTrace.DotnetFrame("--- End of stack trace from previous location ---"));
    }

    [TestMethod]
    public void Dotnet_Object_MatchesItsText_AndCarriesTokensOffsetsAndImages()
    {
        var exception = Thrower.Catch(() => Thrower.RunAsync().GetAwaiter().GetResult());
        var images = new List<SentryDebugImage>();

        var frames = SentryStackTrace.FromException(exception, images);
        var text = SentryStackTrace.ParseDotnet(exception.ToString()).Single().Frames;

        // Grouping sees the same frames from the object and from the text: an async method is the method it was written as.
        CollectionAssert.AreEqual(text.Select(f => f.Module + "." + f.Function).ToArray(), frames.Select(f => f.Module + "." + f.Function).ToArray());
        Assert.AreEqual(typeof(Thrower).FullName!.Replace('+', '.') + ".Fail", frames[^1].Module + "." + frames[^1].Function);
        Assert.IsTrue(frames.Any(f => f.Function == nameof(Thrower.RunAsync)));

        var own = frames.Where(f => f.Module?.StartsWith("Bit.Belemetry.Tests", StringComparison.Ordinal) is true).ToList();
        Assert.IsNotEmpty(own);
        foreach (var frame in own)
        {
            Assert.IsTrue(frame.InApp);
            StringAssert.Matches(frame.FunctionId, new Regex("^0x[0-9a-f]+$"));
            StringAssert.Matches(frame.InstructionAddress, new Regex("^0x[0-9a-f]+$"));
            Assert.AreEqual(typeof(Thrower).Assembly.Location, images[frame.Image!.Value].CodeFile);
        }

        Assert.AreEqual(images.Count, images.Distinct().Count(), "An image is listed once.");

        // The MethodDef row, as Sentry's .NET SDK sends it: the token without its table byte (0x06).
        var token = typeof(Thrower).GetMethod(nameof(Thrower.Fail))!.MetadataToken;
        Assert.AreEqual("0x" + (token & 0x00FFFFFF).ToString("x", System.Globalization.CultureInfo.InvariantCulture), frames[^1].FunctionId);
    }

    [TestMethod]
    public void Dotnet_Object_FollowsTheInnerExceptions()
    {
        var exception = Thrower.Catch(Thrower.Wrapped);
        var inner = SentryStackTrace.FromException(exception.InnerException!, []);
        Assert.AreEqual(nameof(Thrower.Fail), inner[^1].Function);
        Assert.IsEmpty(SentryStackTrace.FromException(new InvalidOperationException("never thrown"), []));
    }

    [TestMethod]
    public void DebugImage_IsWhatSystemReflectionMetadataReads()
    {
        var path = typeof(Thrower).Assembly.Location;
        var image = SentryDebugImage.Of(typeof(Thrower).Module);
        Assert.IsNotNull(image);
        Assert.AreSame(image, SentryDebugImage.Of(typeof(Thrower).Module), "Read once.");

        using var pe = new PEReader(File.OpenRead(path));
        var entries = pe.ReadDebugDirectory();
        var codeView = entries.First(e => e.Type == DebugDirectoryEntryType.CodeView);
        var data = pe.ReadCodeViewDebugDirectoryData(codeView);

        // As Sentry's .NET SDK writes them.
        Assert.AreEqual(path, image.CodeFile);
        Assert.AreEqual($"{pe.PEHeaders.CoffHeader.TimeDateStamp:X8}{pe.PEHeaders.PEHeader!.SizeOfImage:x}", image.CodeId);
        Assert.AreEqual(codeView.IsPortableCodeView ? $"{data.Guid}-{codeView.Stamp:x8}" : $"{data.Guid}-{data.Age:x}", image.DebugId);
        Assert.AreEqual(data.Path, image.DebugFile);

        if (entries.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.PdbChecksum) is { Type: DebugDirectoryEntryType.PdbChecksum } checksumEntry)
        {
            var checksum = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);
            Assert.AreEqual(checksum.AlgorithmName + ":" + Convert.ToHexStringLower(checksum.Checksum.AsSpan()), image.DebugChecksum);
        }

        // The runtime's own assemblies have one too.
        Assert.IsNotNull(SentryDebugImage.Of(typeof(object).Module));
    }

    [TestMethod]
    public void DebugImage_OfWhatIsNotAPortableExecutable_IsNone()
    {
        Assert.IsNull(SentryDebugImage.Read(new MemoryStream([]), "empty.dll"));
        Assert.IsNull(SentryDebugImage.Read(new MemoryStream("MZ not really a PE file, just some text that is long enough"u8.ToArray()), "text.dll"));
    }

    internal static class Thrower
    {
        public static Exception Catch(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                return ex;
            }
            throw new AssertFailedException("Nothing was thrown.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task RunAsync()
        {
            await Task.Yield();
            Fail();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fail() => throw new TimeoutException("late");

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Wrapped()
        {
            try
            {
                Fail();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("wrapped", ex);
            }
        }
    }
}
