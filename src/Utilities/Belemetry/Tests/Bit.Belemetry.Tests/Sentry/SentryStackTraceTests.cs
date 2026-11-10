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
    public void JavaScript_ALineNumberOutOfRange_IsDropped_NotThrown()
    {
        // A relayed stack is anyone's text: a number past int, or another script's digits, is no line.
        var huge = SentryStackTrace.JavaScriptFrame("at f (https://shop.test/app.js:99999999999:1)")!;
        Assert.AreEqual("https://shop.test/app.js", huge.FileName);
        Assert.IsNull(huge.Line);
        Assert.AreEqual(1, huge.Column);

        var arabic = SentryStackTrace.JavaScriptFrame("at f (https://shop.test/app.js:١:٢)")!;
        Assert.AreEqual("https://shop.test/app.js:١:٢", arabic.FileName);
        Assert.IsNull(arabic.Line);

        Assert.IsNull(SentryStackTrace.JavaScriptFrame("f@shop:99999999999"), "No line, no URL: not a frame.");
        Assert.IsNull(SentryStackTrace.DotnetFrame("at X.Y.Z() in /src/a.cs:line ١"));
        Assert.IsNull(SentryStackTrace.DotnetFrame("at X.Y.Z() in /src/a.cs:line 99999999999")!.Line);
    }

    [TestMethod]
    public void JavaScript_TheDotnetRuntimesWebAssembly_IsNotTheApps()
    {
        var frames = SentryStackTrace.ParseJavaScript("""
            Error: boom
                at wasm://wasm/00a8e8e6:wasm-function[8519]:0x1d1d5d
                at render (https://shop.test/app.js:10:15)
            """);
        Assert.HasCount(2, frames);
        Assert.IsTrue(frames[0].InApp);
        Assert.IsFalse(frames[1].InApp, frames[1].FileName);
    }

    [TestMethod]
    public void JavaScript_AnInteropErrorCarryingDotnetText_IsAJavaScriptStack()
    {
        // A [JSInvokable] method threw: the JS Error's message is the .NET exception's text, its frames are JavaScript.
        const string stack = """
            Error: System.InvalidOperationException: boom
               at Shop.Interop.Save() in /src/Shop/Interop.cs:line 9
               at Shop.Interop.<>c.<Run>b__0_0()
                at Object.invokeMethodAsync (https://shop.test/_framework/blazor.webassembly.js:1:3000)
                at save (https://shop.test/app.js:10:15)
            """;

        Assert.IsFalse(SentryStackTrace.IsDotnet(stack));
        AssertFrames(SentryStackTrace.ParseJavaScript(stack),
            ["save", "https://shop.test/app.js", 10, 15, true],
            ["Object.invokeMethodAsync", "https://shop.test/_framework/blazor.webassembly.js", 1, 3000, false]);

        Assert.IsTrue(SentryStackTrace.IsDotnet("System.Exception: mail me@example.com:25\n   at X.Y.Z()"), "A message before the frames is not a frame after them.");
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
    public void Dotnet_Text_AMessageLineStartingWithAt_IsNotAFrame()
    {
        const string text = """
            System.InvalidOperationException: Validation failed:
            at least one item is required
             ---> System.ArgumentException: empty
               at Shop.Cart.Check() in /src/Shop/Cart.cs:line 5
               --- End of inner exception stack trace ---
               at Shop.Cart.Save() in /src/Shop/Cart.cs:line 9
            """;

        var chain = SentryStackTrace.ParseDotnet(text);
        Assert.HasCount(2, chain);
        Assert.AreEqual("Validation failed:\nat least one item is required", chain[0].Message!.ReplaceLineEndings("\n"));
        CollectionAssert.AreEqual(new[] { "Save" }, chain[0].Frames.Select(f => f.Function).ToArray());
        Assert.AreEqual("empty", chain[1].Message);
        CollectionAssert.AreEqual(new[] { "Check" }, chain[1].Frames.Select(f => f.Function).ToArray());

        // A frame without the method's metadata (NativeAOT) starts the frames all the same.
        var aot = SentryStackTrace.ParseDotnet("System.Exception: boom\n   at App!<BaseAddress>+0x1a2b\n   at App!<BaseAddress>+0x3c4d").Single();
        Assert.AreEqual("boom", aot.Message);
        Assert.IsEmpty(aot.Frames);
    }

    [TestMethod]
    public void Dotnet_Text_AnAsyncLambdasOrLocalFunctionsStateMachine_IsTheMethodItWasWrittenAs()
    {
        var lambda = SentryStackTrace.DotnetFrame("at Shop.Cart.<>c__DisplayClass0_0.<<BuildRenderTree>b__0>d.MoveNext() in /src/Cart.razor:line 3")!;
        Assert.AreEqual("Shop.Cart.<>c__DisplayClass0_0", lambda.Module);
        Assert.AreEqual("<BuildRenderTree>b__0", lambda.Function);

        var local = SentryStackTrace.DotnetFrame("at Shop.Program.<<Main>g__Local|0_0>d.MoveNext()")!;
        Assert.AreEqual("Shop.Program", local.Module);
        Assert.AreEqual("<Main>g__Local|0_0", local.Function);

        var iterator = SentryStackTrace.DotnetFrame("at Shop.Orders.<Items>d__7.MoveNext()")!;
        Assert.AreEqual(("Shop.Orders", "Items"), (iterator.Module, iterator.Function));
    }

    [TestMethod]
    public void Dotnet_Frame_IsReadAsThePatternReadsIt_InOnePass()
    {
        // The pattern the hand-written reader replaced, with ASCII digits: the reader must agree with it on every line.
        var pattern = new Regex(@"^at (?<method>[^\s(]+)\((?<args>.*?)\)(?: \+ 0x[0-9a-fA-F]+)?(?: in (?<file>.+):line (?<line>[0-9]+))?$");
        string[] lines =
        [
            "at A.B.C()", "at A.B.C(String a, Int32 b)", "at A.B.C(Func`1 f) + 0x2a", "at A.B.C() + 0x", "at A.B.C() + 0x2a in /x.cs:line 4",
            "at A.B.C() in C:\\src\\x.cs:line 12", "at A.B.C(a) in x:line 1) in y:line 2", "at A.B.C((x)) in /p (1)/x.cs:line 3", "at A.B.C() in :line 3",
            "at A.B.C() in x.cs:line ", "at A.B.C() trailing", "at A.B.C ()", "at (x)", "at A.B.C(", "at A..ctor()", "at A.B.C[T](T x) in x.cs:line 9",
            "at A.B.C(x) + 0x1 + 0x2", "at A.B.C()) in x.cs:line 5", "at f (https://a/b.js:1:2)", "at https://a/b.js:1:2",
        ];
        foreach (var line in lines)
        {
            var match = pattern.Match(line);
            var frame = SentryStackTrace.DotnetFrame(line);
            Assert.AreEqual(match.Success, frame is not null, line);
            if (frame is null) continue;
            Assert.AreEqual(match.Groups["file"].Success ? match.Groups["file"].Value : null, frame.AbsolutePath, line);
            Assert.AreEqual(match.Groups["line"].Success ? int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture) : null, frame.Line, line);
        }

        // Linear: each ')' of this line would re-scan the rest under the pattern (about 80 million steps).
        var adversarial = "at a(" + string.Concat(Enumerable.Repeat(") in x:line 1", 2500)) + "!";
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 10; i++) Assert.IsNull(SentryStackTrace.DotnetFrame(adversarial));
        Assert.IsLessThan(2000L, watch.ElapsedMilliseconds, "Ten reads of a 32K-character line.");
    }

    [TestMethod]
    public void Dotnet_Object_MatchesItsText_AndCarriesTokensOffsetsAndImages()
    {
        var exception = Thrower.Catch(() => Thrower.RunAsync().GetAwaiter().GetResult());

        var (frames, complete) = SentryStackTrace.FromException(exception);
        var text = SentryStackTrace.ParseDotnet(exception.ToString()).Single().Frames;
        Assert.IsTrue(complete, "The JIT has every frame's method.");

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
            Assert.AreEqual(typeof(Thrower).Assembly.Location, frame.DebugImage!.CodeFile);
        }

        // The MethodDef row, as Sentry's .NET SDK sends it: the token without its table byte (0x06).
        var token = typeof(Thrower).GetMethod(nameof(Thrower.Fail))!.MetadataToken;
        Assert.AreEqual("0x" + (token & 0x00FFFFFF).ToString("x", System.Globalization.CultureInfo.InvariantCulture), frames[^1].FunctionId);

        // The file and line come from the text, resolved once as the record was made, not from the PDB again.
        Assert.IsNull(frames[^1].Line);
        Assert.IsTrue(SentryStackTrace.TryAddFileInfo(frames, text));
        Assert.AreEqual(text[^1].Line, frames[^1].Line);
        Assert.AreEqual(text[^1].AbsolutePath, frames[^1].AbsolutePath);
        Assert.IsNotNull(frames[^1].FunctionId, "The object's own data stays.");
        Assert.AreEqual(text[^1].Line, SentryStackTrace.FromException(exception, fileInfo: true).Frames[^1].Line, "What the PDB says.");
        Assert.IsFalse(SentryStackTrace.TryAddFileInfo(frames, text[1..]), "Frames that do not line up get nothing.");
    }

    [TestMethod]
    public void Dotnet_Object_AnAsyncLambda_IsNamedAsItsText()
    {
        var exception = Thrower.Catch(() => Thrower.RunLambdaAsync(1).GetAwaiter().GetResult());

        var frames = SentryStackTrace.FromException(exception).Frames;
        var text = SentryStackTrace.ParseDotnet(exception.ToString()).Single().Frames;
        CollectionAssert.AreEqual(text.Select(f => f.Module + "." + f.Function).ToArray(), frames.Select(f => f.Module + "." + f.Function).ToArray());
        Assert.IsTrue(frames.Any(f => f.Function!.StartsWith("<" + nameof(Thrower.RunLambdaAsync) + ">b__", StringComparison.Ordinal)), string.Join(" | ", frames.Select(f => f.Module + "." + f.Function)));
        Assert.IsFalse(frames.Any(f => f.Function == "MoveNext"));
    }

    [TestMethod]
    public void Dotnet_Object_FollowsTheInnerExceptions()
    {
        var exception = Thrower.Catch(Thrower.Wrapped);
        var inner = SentryStackTrace.FromException(exception.InnerException!).Frames;
        Assert.AreEqual(nameof(Thrower.Fail), inner[^1].Function);
        var (never, complete) = SentryStackTrace.FromException(new InvalidOperationException("never thrown"));
        Assert.IsEmpty(never);
        Assert.IsTrue(complete, "Nothing is missing from no frames.");
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
        public static Task RunLambdaAsync(int value)
        {
            // Captures value: the lambda is a method of a display class, its state machine nested in that class.
            Func<Task> run = async () =>
            {
                await Task.Yield();
                if (value > 0) Fail();
            };
            return run();
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
