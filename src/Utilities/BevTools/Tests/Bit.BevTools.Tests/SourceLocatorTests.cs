using System.Reflection;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Exact source locations from this test assembly's own PDB. The expected lines are looked up in this very file, at the
/// path the symbols give, so the test cannot drift from the code it describes.
/// </summary>
[TestClass]
public class SourceLocatorTests : BunitContext
{
    public SourceLocatorTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    /// <summary>The line of this file that is exactly <paramref name="text"/>, trimmed (one-based), read from where the PDB says the file is.</summary>
    private static int LineOf(string text)
    {
        var path = BevSourceLocator.Of(typeof(LocatedComponent), null)[0].Path;
        var line = Array.FindIndex(File.ReadAllLines(path), l => l.Trim() == text);
        Assert.IsTrue(line >= 0, text);
        return line + 1;
    }

    [TestMethod]
    public void A_components_file_and_first_line_come_from_the_symbols()
    {
        var sources = BevSourceLocator.Of(typeof(LocatedComponent), "vscode://file/{path}:{line}:{column}");
        var source = Assert.ContainsSingle(sources);
        Assert.AreEqual("SourceLocatorTests.cs", source.FileName);
        Assert.IsTrue(Path.IsPathRooted(source.Path), source.Path);
        // The first line written in the type: its declaration (where an implicit constructor is mapped) at the earliest.
        Assert.IsTrue(source.Line >= LineOf("public class LocatedComponent : ComponentBase") && source.Line <= LineOf("builder.OpenElement(0, \"button\");"), source.Line.ToString());
        StringAssert.StartsWith(source.Url, "vscode://file/");
        StringAssert.EndsWith(source.Url, "SourceLocatorTests.cs:" + source.Line + ":" + source.Column);
    }

    [TestMethod]
    public void A_method_and_an_async_methods_body_are_located()
    {
        var handler = BevSourceLocator.Of(typeof(LocatedComponent).GetMethod(nameof(LocatedComponent.Save))!, null);
        // A debug build's first sequence point is the opening brace.
        Assert.AreEqual(LineOf("public void Save()") + 1, handler!.Line);
        Assert.IsNull(handler.Url, "no template, no link");

        // The kickoff method of an async method has no sequence points; its state machine has them.
        var async = BevSourceLocator.Of(typeof(LocatedComponent).GetMethod(nameof(LocatedComponent.LoadAsync))!, null);
        Assert.AreEqual(LineOf("public async Task LoadAsync()") + 1, async!.Line);
    }

    [TestMethod]
    public void A_pdb_of_another_build_is_refused()
    {
        static Guid BuildId(string assemblyPath)
        {
            using var pe = new System.Reflection.PortableExecutable.PEReader(File.OpenRead(assemblyPath));
            return pe.ReadCodeViewDebugDirectoryData(pe.ReadDebugDirectory().First(e => e.Type == System.Reflection.PortableExecutable.DebugDirectoryEntryType.CodeView)).Guid;
        }

        var tests = typeof(SourceLocatorTests).Assembly.Location;
        var bevTools = typeof(BevSourceLocator).Assembly.Location;
        using var testsPdb = System.Reflection.Metadata.MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.ChangeExtension(tests, ".pdb")));
        using var bevToolsPdb = System.Reflection.Metadata.MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.ChangeExtension(bevTools, ".pdb")));

        Assert.IsTrue(BevSourceLocator.IsBuildOf(testsPdb, BuildId(tests)));
        Assert.IsFalse(BevSourceLocator.IsBuildOf(bevToolsPdb, BuildId(tests)), "another assembly's PDB, as a PDB of another build would be");
    }

    [TestMethod]
    public void An_assembly_without_a_file_outside_WebAssembly_says_so_instead_of_downloading()
    {
        var context = new System.Runtime.Loader.AssemblyLoadContext("bev-from-memory", isCollectible: true);
        try
        {
            var fromMemory = context.LoadFromStream(new MemoryStream(File.ReadAllBytes(typeof(SourceLocatorTests).Assembly.Location)));
            Assert.AreEqual("", fromMemory.Location);

            Assert.IsEmpty(BevSourceLocator.Of(fromMemory.GetType(typeof(LocatedComponent).FullName!)!, null));
            Assert.IsFalse(BevSourceLocator.NeedsDownload(fromMemory), "the app serves no PDB list outside WebAssembly");
            StringAssert.Contains(BevSourceLocator.WhyUnavailable(fromMemory), "has no file on disk");
        }
        finally
        {
            context.Unload();
        }
    }

    [TestMethod]
    public void Locations_in_an_assembly_edited_with_hot_reload_may_have_moved()
    {
        try
        {
            Assert.IsFalse(Assert.ContainsSingle(BevSourceLocator.Of(typeof(LocatedComponent), null)).MayHaveMoved);
            typeof(BevSourceLocator).GetMethod("ClearCache", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [new[] { typeof(LocatedComponent) }]);

            Assert.IsTrue(Assert.ContainsSingle(BevSourceLocator.Of(typeof(LocatedComponent), null)).MayHaveMoved);
            Assert.IsTrue(BevSourceLocator.Of(typeof(LocatedComponent).GetMethod(nameof(LocatedComponent.Save))!, null)!.MayHaveMoved);
            Assert.Contains(a => a.HandlerType == typeof(BevSourceLocator), typeof(BevSourceLocator).Assembly.GetCustomAttributes<System.Reflection.Metadata.MetadataUpdateHandlerAttribute>());
        }
        finally
        {
            BevSourceLocator.ForgetEdits();
        }
    }

    [TestMethod]
    public void Symbols_that_are_absent_or_unreadable_are_said_to_be_so()
    {
        var empty = AssemblyBuilderFor("Bev.Missing");
        BevSourceLocator.Provide(empty, null);
        StringAssert.Contains(BevSourceLocator.WhyUnavailable(empty), "serves no symbols");

        var broken = AssemblyBuilderFor("Bev.Broken");
        BevSourceLocator.Provide(broken, [1, 2, 3, 4]);
        StringAssert.Contains(BevSourceLocator.WhyUnavailable(broken), "could not be read");
    }

    private static Assembly AssemblyBuilderFor(string name) =>
        System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), System.Reflection.Emit.AssemblyBuilderAccess.Run);

    [TestMethod]
    public void A_file_name_is_read_from_paths_of_either_platform()
    {
        // WebAssembly runs on a Unix-like runtime and reads PDBs built on Windows.
        Assert.AreEqual("Counter.razor", new BevSourceLocation(@"D:\app\Pages\Counter.razor", 1, 1, null).FileName);
        Assert.AreEqual("Counter.razor", new BevSourceLocation("/src/app/Pages/Counter.razor", 1, 1, null).FileName);
    }

    [TestMethod]
    public void Editor_links_escape_the_path_and_never_run_script()
    {
        Assert.AreEqual("vscode://file/D:/My%20Projects/App/Pages/Counter.razor:12:5",
            BevSourceLocator.EditorUrl("vscode://file/{path}:{line}:{column}", @"D:\My Projects\App\Pages\Counter.razor", 12, 5));
        Assert.AreEqual("idea://open?file=/src/app/Counter.razor&line=3",
            BevSourceLocator.EditorUrl("idea://open?file={path}&line={line}", "/src/app/Counter.razor", 3, 1));
        Assert.IsNull(BevSourceLocator.EditorUrl(" javascript:alert(1)//{path}", "/x", 1, 1));
        Assert.IsNull(BevSourceLocator.EditorUrl("data:text/html,{path}", "/x", 1, 1));
        Assert.IsNull(BevSourceLocator.EditorUrl(null, "/x", 1, 1));

        // Browsers drop tabs and newlines inside a scheme and control characters before it: "java\tscript:" runs script.
        Assert.IsNull(BevSourceLocator.EditorUrl("java\tscript:alert(1)//{path}", "/x", 1, 1));
        Assert.IsNull(BevSourceLocator.EditorUrl("\u0001javascript:alert(1)//{path}", "/x", 1, 1));
        Assert.IsNull(BevSourceLocator.EditorUrl("JavaScript:alert(1)//{path}", "/x", 1, 1));
        Assert.IsNull(BevSourceLocator.EditorUrl("{path}", "javascript:alert(1)", 1, 1), "no scheme of its own: not an editor link");
    }

    [TestMethod]
    public void The_inspector_links_the_component_and_its_handlers_to_the_editor()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { TargetId = "t1" });
        var panel = Render<BevToolsPanel>();
        Render<LocatedComponent>();
        var record = Session.Components.All.Single(r => r.Type == typeof(LocatedComponent));
        panel.InvokeAsync(() => panel.Instance.SelectComponent(record.InstanceId));

        var first = BevSourceLocator.Of(typeof(LocatedComponent), null)[0].Line;
        var save = LineOf("public void Save()") + 1;
        var links = panel.FindAll(".bev-right a[href^='vscode://file/']").Select(a => a.TextContent).ToList();
        Assert.Contains("SourceLocatorTests.cs:" + first, links);
        Assert.Contains("SourceLocatorTests.cs:" + save, links, "the onclick handler's own line");

        using var inspected = JsonDocument.Parse(BevToolsProtocol.Handle(Session, "inspect", JsonSerializer.Serialize(new { instanceId = record.InstanceId })));
        var source = inspected.RootElement.GetProperty("sources")[0];
        Assert.AreEqual(first, source.GetProperty("line").GetInt32());
        StringAssert.StartsWith(source.GetProperty("url").GetString(), "vscode://file/");
        Assert.AreEqual(save, inspected.RootElement.GetProperty("wiring").GetProperty("bindings")[0].GetProperty("handlerSource")[0].GetProperty("line").GetInt32());
    }
}

public class LocatedComponent : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, Save));
        builder.CloseElement();
    }

    public void Save()
    {
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        await Task.Yield();
    }

}
