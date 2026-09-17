using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;

namespace Bit.Minifier.Tests;

[TestClass]
public class AssemblyMinifierTests
{
    private const string Library = "Bit.Minifier.Tests.Library";
    private const string Friend = "Bit.Minifier.Tests.Friend";

    private string root = default!;
    private string original = default!;
    private string minified = default!;
    private readonly List<AssemblyLoadContext> contexts = [];

    [TestInitialize]
    public void Initialize()
    {
        root = Path.Combine(Path.GetTempPath(), "bit-minifier-tests", Guid.NewGuid().ToString("N"));
        original = Directory.CreateDirectory(Path.Combine(root, "original")).FullName;
        minified = Directory.CreateDirectory(Path.Combine(root, "minified")).FullName;
        foreach (var name in new[] { Library, Friend })
        {
            foreach (var extension in new[] { ".dll", ".pdb" })
            {
                File.Copy(Path.Combine(AppContext.BaseDirectory, name + extension), Path.Combine(original, name + extension));
                File.Copy(Path.Combine(AppContext.BaseDirectory, name + extension), Path.Combine(minified, name + extension));
            }
        }
        // an ILLink output folder holds the framework too; the minifier resolves from nowhere else
        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (var name in new[] { "System.Private.CoreLib", "System.Runtime", "System.Collections", "System.Linq", "System.Runtime.InteropServices" })
        {
            File.Copy(Path.Combine(runtime, name + ".dll"), Path.Combine(minified, name + ".dll"));
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var context in contexts) context.Unload();
        contexts.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        try { Directory.Delete(root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    [TestMethod]
    public void StripsCompileTimeAttributesButKeepsCompilerGeneratedOnTypes()
    {
        Minify();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var members = module.GetTypes().SelectMany(t => t.Methods.Cast<ICustomAttributeProvider>().Concat(t.Fields).Concat(t.Properties)).ToList();
        Assert.IsFalse(members.Any(m => HasAttribute(m, "CompilerGeneratedAttribute")));
        Assert.IsFalse(module.GetTypes().Cast<ICustomAttributeProvider>().Concat(members).Any(m => HasAttribute(m, "NullableAttribute") || HasAttribute(m, "NullableContextAttribute")));
        Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).SelectMany(m => m.Parameters).Any(p => HasAttribute(p, "NotNullWhenAttribute")));

        var generatedTypes = module.GetTypes().Where(t => t.Name.StartsWith('<') && t.IsNested).ToList();
        Assert.IsNotEmpty(generatedTypes);
        Assert.IsTrue(generatedTypes.All(t => HasAttribute(t, "CompilerGeneratedAttribute")));
    }

    [TestMethod]
    public void KeepNullableKeepsTheNullableMetadata()
    {
        Minify(keepNullable: true);

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        Assert.IsTrue(HasNullableMetadata(module));
    }

    [TestMethod]
    public void ShortensGeneratedNamesAndKeepsHandWrittenOnes()
    {
        Minify();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var names = AllNames(module);

        string[] handWritten = ["AddAsync", "Doubles", "CountAsync", "Multiplier", "SumWhere", "Record", "ShiftAmount", "ThrowFromHelper", "_history", "InternalCounter", "InternalSquare", "Label", "Total"];
        foreach (var name in handWritten)
        {
            CollectionAssert.Contains(names, name);
            Assert.IsFalse(names.Any(n => n.StartsWith($"<{name}>", StringComparison.Ordinal)), $"<{name}> was not shortened");
        }

        // the kind marker stays, so debuggers still recognize what each generated member is
        Assert.IsTrue(names.Any(n => n.StartsWith("<a>d__", StringComparison.Ordinal)));
        Assert.IsTrue(names.Any(n => n.EndsWith(">k__BackingField", StringComparison.Ordinal)));
        Assert.IsTrue(names.Any(n => n.Contains(">g__Twice|", StringComparison.Ordinal)));

        // hoisted locals and captured variables keep the names the debugger shows
        Assert.IsTrue(names.Contains("offset") || names.Any(n => n.StartsWith("<offset>", StringComparison.Ordinal)));
        Assert.IsTrue(names.Any(n => n.StartsWith("<before>5__", StringComparison.Ordinal)));
        Assert.IsTrue(names.Contains("<>4__this"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MinifiedCodeBehavesLikeTheOriginal(bool aggressive)
    {
        var expected = await Exercise(original);

        Minify(aggressive: aggressive);

        CollectionAssert.AreEqual(expected, await Exercise(minified));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StackTracesKeepFilesAndLineNumbers(bool aggressive)
    {
        var expected = FailureStackTrace(original);

        Minify(aggressive: aggressive);
        var actual = FailureStackTrace(minified);

        StringAssert.Contains(expected[0], "Calculator.cs:line");
        Assert.HasCount(expected.Length, actual);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(LineOf(expected[i]), LineOf(actual[i]));
        }
        if (aggressive is false) CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PdbStillDescribesTheMinifiedAssembly(bool aggressive)
    {
        var before = ReadDebugInfo(original, Library);

        Minify(aggressive: aggressive);
        var after = ReadDebugInfo(minified, Library);

        Assert.IsTrue(after.IdMatches);
        Assert.AreEqual(before.SequencePoints, after.SequencePoints);
        Assert.AreEqual(before.MethodsWithSequencePoints, after.MethodsWithSequencePoints);
        Assert.AreEqual(0, after.SequencePointsOutsideBody);
        CollectionAssert.AreEquivalent(before.LocalNames, after.LocalNames);
        Assert.AreEqual(before.StateMachineMethods, after.StateMachineMethods);
    }

    [TestMethod]
    public void AggressiveRenamesInternalsOnlyWhenEveryFriendIsMinified()
    {
        Minify(aggressive: true, assemblies: [Library, Friend]);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            CollectionAssert.DoesNotContain(names, "Record");
            CollectionAssert.DoesNotContain(names, "_history");
            CollectionAssert.DoesNotContain(names, "InternalSquare");
            CollectionAssert.DoesNotContain(names, "InternalCounter");
            // protected and public members are API
            CollectionAssert.Contains(names, "Shift");
            CollectionAssert.Contains(names, "AddAsync");
        }
    }

    [TestMethod]
    public async Task AggressiveKeepsInternalsWhenAFriendIsNotMinified()
    {
        Minify(aggressive: true, assemblies: [Library]);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            CollectionAssert.DoesNotContain(names, "Record");
            CollectionAssert.Contains(names, "InternalSquare");
            CollectionAssert.Contains(names, "InternalCounter");
        }

        CollectionAssert.AreEqual(await Exercise(original), await Exercise(minified));
    }

    [TestMethod]
    public void LeavesTheFolderUntouchedWhenAReferenceWouldBreak()
    {
        // without the InternalsVisibleTo the minifier believes nobody else can see the internals the friend uses
        var libraryPath = Path.Combine(minified, Library + ".dll");
        using (var assembly = AssemblyDefinition.ReadAssembly(libraryPath, new ReaderParameters { InMemory = true, ReadSymbols = true }))
        {
            var ivt = assembly.CustomAttributes.Single(a => a.AttributeType.Name == "InternalsVisibleToAttribute");
            assembly.CustomAttributes.Remove(ivt);
            assembly.Write(libraryPath, new WriterParameters { WriteSymbols = true });
        }
        var before = Snapshot(minified);

        var error = Assert.ThrowsExactly<MinifierException>(() => Minify(aggressive: true, assemblies: [Library]));

        StringAssert.Contains(error.Message, Friend);
        CollectionAssert.AreEqual(before, Snapshot(minified));
    }

    [TestMethod]
    public void SkipsAssembliesThatAreNotInTheFolder()
    {
        var before = Snapshot(minified);

        var results = Minify(assemblies: ["Not.There"]);

        Assert.IsEmpty(results);
        CollectionAssert.AreEqual(before, Snapshot(minified));
    }

    [TestMethod]
    public void ReportsSizesAndWritesTheMap()
    {
        var map = Path.Combine(root, "map", "bit-minifier.map");

        var results = Minify(assemblies: [Library, Friend], mapFile: map);

        var library = results.Single(r => r.Name == Library);
        Assert.IsLessThan(library.OriginalSize, library.MinifiedSize);
        Assert.IsGreaterThan(0, library.RemovedAttributes);
        Assert.IsGreaterThan(0, library.RenamedMembers);
        Assert.AreEqual(library.MinifiedSize, new FileInfo(Path.Combine(minified, Library + ".dll")).Length);

        var lines = File.ReadAllLines(map);
        Assert.IsTrue(lines.Any(l => l.StartsWith($"{Library}\tT\t", StringComparison.Ordinal) && l.Contains("<AddAsync>d__", StringComparison.Ordinal)));
        Assert.IsTrue(lines.All(l => l.Split('\t').Length == 4));
    }

    [TestMethod]
    public void MinifiesAsOftenAsItIsRun()
    {
        Minify();
        var once = Snapshot(minified);

        Minify();

        // a second pass finds nothing left to strip, and the short names map onto themselves
        CollectionAssert.AreEqual(once, Snapshot(minified));
    }

    [TestMethod]
    public async Task OtherLibrariesKeepWhatTheyMayReadAtRuntime()
    {
        var expected = await Exercise(original);

        Minify(full: []);

        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            // EF Core finds backing fields by name, Newtonsoft.Json skips fields by [CompilerGenerated]
            CollectionAssert.Contains(names, "<Label>k__BackingField");
            Assert.IsTrue(module.GetTypes().SelectMany(t => t.Fields).Where(f => f.Name.EndsWith("k__BackingField", StringComparison.Ordinal)).All(f => HasAttribute(f, "CompilerGeneratedAttribute")));
            // the rest still goes
            Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).Any(m => HasAttribute(m, "CompilerGeneratedAttribute")));
            Assert.IsFalse(names.Any(n => n.StartsWith("<AddAsync>", StringComparison.Ordinal)));
            Assert.IsFalse(HasNullableMetadata(module));
        }

        CollectionAssert.AreEqual(expected, await Exercise(minified));
    }

    [TestMethod]
    public void KeepsNullableMetadataOfOtherLibrariesWhenEfCoreIsPublished()
    {
        File.WriteAllBytes(Path.Combine(minified, "Microsoft.EntityFrameworkCore.dll"), []);

        Minify(full: [Friend]);

        using (var library = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            Assert.IsTrue(HasNullableMetadata(library));
        }
        using var friend = ModuleDefinition.ReadModule(Path.Combine(minified, Friend + ".dll"));
        Assert.IsFalse(friend.GetTypes().Cast<ICustomAttributeProvider>().Concat(friend.GetTypes().SelectMany(t => t.Methods)).Any(p => HasAttribute(p, "NullableContextAttribute")));
    }

    [TestMethod]
    public async Task MinifiesEveryAssemblyInTheFolderWhenNoneIsNamed()
    {
        var expected = await Exercise(original);
        var facade = Snapshot(minified).Single(f => f.StartsWith("System.Runtime.dll:", StringComparison.Ordinal));

        var results = new AssemblyMinifier(new MinifierOptions { Directory = minified }).Run();

        CollectionAssert.IsSubsetOf(new[] { Library, Friend }, results.Select(r => r.Name).ToList());
        // the runtime's ReadyToRun images can't be written back, so they are skipped
        CollectionAssert.DoesNotContain(results.Select(r => r.Name).ToList(), "System.Private.CoreLib");
        // nothing to change in a type-forwarding facade, so it is not rewritten
        CollectionAssert.DoesNotContain(results.Select(r => r.Name).ToList(), "System.Runtime");
        CollectionAssert.Contains(Snapshot(minified), facade);
        CollectionAssert.AreEqual(expected, await Exercise(minified));
    }

    [TestMethod]
    public async Task AggressiveKeepsBehaviourAndPublicNames()
    {
        var expected = await Exercise(original);
        var stackTrace = FailureStackTrace(original);

        new AssemblyMinifier(new MinifierOptions { Directory = minified, Assemblies = [Library, Friend], FullyMinified = [], Aggressive = true }).Run();

        CollectionAssert.AreEqual(expected, await Exercise(minified));
        using (var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll")))
        {
            var names = AllNames(module);
            // public API, and what serializers see
            foreach (var name in new[] { "Calculator", "AddAsync", "Fail", "Label", "Total", "Point", "Length", "Box`1", "Shade" }) CollectionAssert.Contains(names, name);
            // everything else is short
            foreach (var name in new[] { "Record", "ShiftAmount", "ThrowFromHelper", "_history", "Select" }) CollectionAssert.DoesNotContain(names, name);
            Assert.IsFalse(module.GetTypes().SelectMany(t => t.Methods).Where(m => m.IsPrivate && m.HasCustomAttributes is false).SelectMany(m => m.Parameters).Any(p => p.Name.Length > 0));
            Assert.IsFalse(module.Assembly.CustomAttributes.Any(a => a.AttributeType.Name == "AssemblyCompanyAttribute"));
        }
        // the public frame keeps its name, and every frame its line
        var actual = FailureStackTrace(minified);
        Assert.HasCount(stackTrace.Length, actual);
        StringAssert.Contains(actual[1], "Calculator.Fail()");
        for (int i = 0; i < actual.Length; i++) Assert.AreEqual(LineOf(stackTrace[i]), LineOf(actual[i]));
    }

    [TestMethod]
    public void AggressiveKeepsNamesStringLiteralsMention()
    {
        new AssemblyMinifier(new MinifierOptions { Directory = minified, Assemblies = [Library, Friend], FullyMinified = [], Aggressive = true }).Run();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        // "total is {0}" in Calculator.ThrowFromHelper names nothing, but GetMethod(nameof(ShiftAmount)) would: see Reflected
        CollectionAssert.Contains(AllNames(module), "ReflectedByName");
        CollectionAssert.Contains(AllNames(module), "_reflectedField");
        // EF Core's compiled model reaches backing fields this way
        CollectionAssert.Contains(AllNames(module), "<Note>k__BackingField");
    }

    [TestMethod]
    public void BackingFieldsAStringNamesKeepTheirNames()
    {
        Minify();

        using var module = ModuleDefinition.ReadModule(Path.Combine(minified, Library + ".dll"));
        var names = AllNames(module);
        CollectionAssert.Contains(names, "<Note>k__BackingField");
        CollectionAssert.DoesNotContain(names, "<Label>k__BackingField");
    }

    private IReadOnlyList<MinifiedAssembly> Minify(bool aggressive = false, bool keepNullable = false, string[]? assemblies = null, string? mapFile = null, string[]? full = null)
        => new AssemblyMinifier(new MinifierOptions
        {
            Directory = minified,
            Assemblies = assemblies ?? [Library, Friend],
            FullyMinified = full ?? [Library, Friend],
            Aggressive = aggressive,
            KeepNullable = keepNullable,
            MapFile = mapFile,
        }).Run();

    private async Task<List<string>> Exercise(string directory)
    {
        var (library, friend) = Load(directory);
        var results = new List<string>();

        var calculator = Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Calculator")!)!;
        results.Add($"add {await Call<int>(calculator, "AddAsync", 5)} {await Call<int>(calculator, "AddAsync", 2)}");
        results.Add($"history {Get(calculator, "HistoryCount")} total {Get(calculator, "Total")}");
        results.Add($"doubles {string.Join(",", ((IEnumerable)Invoke(calculator, "Doubles", 3)!).Cast<object>())}");
        results.Add($"count {string.Join(",", await Drain(Invoke(calculator, "CountAsync", 3, CancellationToken.None)!))}");
        results.Add($"multiplier {((Delegate)Invoke(calculator, "Multiplier", 3)!).DynamicInvoke(2)}");
        results.Add($"sum {Invoke(calculator, "SumWhere", new[] { 1, 2, 3 }, 2)}");
        var parse = new object?[] { "7", null };
        results.Add($"parse {calculator.GetType().GetMethod("TryParse")!.Invoke(calculator, parse)} {parse[1]}");
        calculator.GetType().GetProperty("Label")!.SetValue(calculator, "L");
        results.Add($"label {Get(calculator, "Label")}");
        results.Add($"reflection {Invoke(calculator, "CallByReflection")}");
        calculator.GetType().GetProperty("Note")!.SetValue(calculator, "noted");
        var noteAccess = library.GetType("Bit.Minifier.Tests.Library.NoteAccess")!;
        results.Add($"note {noteAccess.GetMethod("ReadThroughAccessor")!.Invoke(null, [calculator])} {noteAccess.GetMethod("ReadThroughReflection")!.Invoke(null, [calculator])}");

        var point = Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Point")!, 3, 4)!;
        results.Add($"point {point} {Get(point, "Length")} {point.Equals(Activator.CreateInstance(point.GetType(), 3, 4))}");

        var box = Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Box`1")!.MakeGenericType(typeof(string)))!;
        box.GetType().GetProperty("Value")!.SetValue(box, "boxed");
        results.Add($"box {await Call<string>(box, "GetAsync")}");
        results.Add($"shade {Enum.Parse(library.GetType("Bit.Minifier.Tests.Library.Shade")!, "Dark")}");

        var friendCalculator = Activator.CreateInstance(friend.GetType("Bit.Minifier.Tests.Friend.FriendCalculator")!)!;
        results.Add($"friend square {Invoke(friendCalculator, "SquareThroughInternals", 3)}");
        results.Add($"friend add {await Call<int>(friendCalculator, "AddTwiceAsync", 2)}");
        results.Add($"friend sum {Invoke(friendCalculator, "SumWhere", new[] { 1, 2, 3 }, 2)}");
        return results;
    }

    private string[] FailureStackTrace(string directory)
    {
        var (library, _) = Load(directory);
        var calculator = Activator.CreateInstance(library.GetType("Bit.Minifier.Tests.Library.Calculator")!)!;
        var error = Assert.ThrowsExactly<TargetInvocationException>(() => Invoke(calculator, "Fail"));
        return error.InnerException!.StackTrace!.Split('\n').Select(l => l.Trim()).Where(l => l.Contains("Calculator.cs", StringComparison.Ordinal)).ToArray();
    }

    private static string LineOf(string frame) => frame[(frame.LastIndexOf(" in ", StringComparison.Ordinal) + 4)..];

    private (Assembly library, Assembly friend) Load(string directory)
    {
        var context = new DirectoryLoadContext(directory);
        contexts.Add(context);
        return (context.LoadFromAssemblyName(new AssemblyName(Library)), context.LoadFromAssemblyName(new AssemblyName(Friend)));
    }

    // the fixtures also sit next to the tests, so the default context must never be asked for them
    private sealed class DirectoryLoadContext(string directory) : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name is not (Library or Friend)) return null;
            var path = Path.Combine(directory, name.Name + ".dll");
            using var assembly = new MemoryStream(File.ReadAllBytes(path));
            using var symbols = new MemoryStream(File.ReadAllBytes(Path.ChangeExtension(path, ".pdb")));
            return LoadFromStream(assembly, symbols);
        }
    }

    private static object? Invoke(object target, string method, params object?[] arguments)
        => target.GetType().GetMethod(method)!.Invoke(target, arguments);

    private static object? Get(object target, string property) => target.GetType().GetProperty(property)!.GetValue(target);

    private static async Task<T> Call<T>(object target, string method, params object?[] arguments)
        => await (Task<T>)Invoke(target, method, arguments)!;

    private static async Task<List<int>> Drain(object asyncEnumerable)
    {
        var values = new List<int>();
        await foreach (var value in (IAsyncEnumerable<int>)asyncEnumerable) values.Add(value);
        return values;
    }

    private static List<string> AllNames(ModuleDefinition module)
        => module.GetTypes().SelectMany(t => new[] { t.Name }
            .Concat(t.Methods.Select(m => m.Name))
            .Concat(t.Fields.Select(f => f.Name))
            .Concat(t.Properties.Select(p => p.Name))).ToList();

    private static bool HasNullableMetadata(ModuleDefinition module)
        => module.GetTypes().Cast<ICustomAttributeProvider>()
            .Concat(module.GetTypes().SelectMany(t => t.Properties))
            .Concat(module.GetTypes().SelectMany(t => t.Methods))
            .Any(p => HasAttribute(p, "NullableAttribute") || HasAttribute(p, "NullableContextAttribute"));

    private static bool HasAttribute(ICustomAttributeProvider provider, string name)
        => provider.CustomAttributes.Any(a => a.AttributeType.Name == name);

    private static List<string> Snapshot(string directory)
        => Directory.GetFiles(directory).Order().Select(f => $"{Path.GetFileName(f)}:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f)))}").ToList();

    private static DebugInfo ReadDebugInfo(string directory, string name)
    {
        using var pe = new PEReader(File.OpenRead(Path.Combine(directory, name + ".dll")));
        var metadata = pe.GetMetadataReader();
        var codeView = pe.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.CodeView);
        var codeViewData = pe.ReadCodeViewDebugDirectoryData(codeView);

        using var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.Combine(directory, name + ".pdb")));
        var pdb = provider.GetMetadataReader();
        var id = new BlobContentId(pdb.DebugMetadataHeader!.Id);

        int methods = 0, points = 0, outside = 0;
        foreach (var handle in pdb.MethodDebugInformation)
        {
            var info = pdb.GetMethodDebugInformation(handle);
            if (info.SequencePointsBlob.IsNil) continue;
            var definition = metadata.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(MetadataTokens.GetRowNumber(handle)));
            var size = definition.RelativeVirtualAddress == 0 ? 0 : pe.GetMethodBody(definition.RelativeVirtualAddress).Size;
            methods++;
            foreach (var point in info.GetSequencePoints())
            {
                points++;
                if (point.Offset >= size) outside++;
            }
        }

        var locals = pdb.LocalVariables.Select(h => pdb.GetString(pdb.GetLocalVariable(h).Name)).Order().ToList();
        return new DebugInfo(id.Guid == codeViewData.Guid && id.Stamp == codeView.Stamp, methods, points, outside, locals, pdb.GetTableRowCount(TableIndex.StateMachineMethod));
    }

    private sealed record DebugInfo(bool IdMatches, int MethodsWithSequencePoints, int SequencePoints, int SequencePointsOutsideBody, List<string> LocalNames, int StateMachineMethods);
}
