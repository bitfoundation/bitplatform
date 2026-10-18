using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Scripts;

/// <summary>
/// The .NET side of the lazy scripts (BitBlazorUIScripts): a call into the packages' scripts imports the module exporting
/// it first - after its package's boot module and what it uses of another package - once per runtime, and a teardown of a
/// script the page never ran is skipped rather than download the script for nothing. The switch is process-wide, so every
/// test sets it and puts it back.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class BitLazyScriptsTests
{
    private const string CoreBoot = "./_content/Bit.BlazorUI/scripts/bit.blazorui.boot.js";

    [TestCleanup]
    public void Cleanup()
    {
        BitBlazorUIScripts.UseBundledScripts();
        typeof(BitBlazorUIScripts).GetMethod("SetContentPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                                  .Invoke(null, ["./_content/"]);
    }

    [TestMethod]
    public async Task InBundleModeACallShouldImportNothing()
    {
        BitBlazorUIScripts.UseBundledScripts();
        var js = new RecordingJsRuntime();

        await js.InvokeVoid("BitBlazorUI.Callouts.toggle", "id");

        CollectionAssert.AreEqual(new[] { "BitBlazorUI.Callouts.toggle" }, js.Calls);
    }

    [TestMethod]
    public async Task AFirstCallShouldImportTheBootModuleAndItsScriptOnce()
    {
        BitBlazorUIScripts.UseLazyScripts();
        var js = new RecordingJsRuntime();

        await js.InvokeVoid("BitBlazorUI.Callouts.toggle", "id");
        await js.InvokeVoid("BitBlazorUI.Callouts.toggle", "id");
        await js.InvokeVoid("BitBlazorUI.Callouts.reposition");

        CollectionAssert.AreEqual(new[]
        {
            $"import {CoreBoot}",
            "import ./_content/Bit.BlazorUI/scripts/modules/Callouts.js",
            "BitBlazorUI.Callouts.toggle",
            "BitBlazorUI.Callouts.toggle",
            "BitBlazorUI.Callouts.reposition",
        }, js.Calls);
    }

    [TestMethod]
    public async Task EachRuntimeShouldImportForItself()
    {
        BitBlazorUIScripts.UseLazyScripts();
        var first = new RecordingJsRuntime();
        var second = new RecordingJsRuntime();

        await first.InvokeVoid("BitBlazorUI.Callouts.toggle");
        await second.InvokeVoid("BitBlazorUI.Callouts.toggle");

        // Another circuit's page, which has loaded nothing.
        CollectionAssert.Contains(second.Calls, "import ./_content/Bit.BlazorUI/scripts/modules/Callouts.js");
    }

    [TestMethod]
    public async Task ATeardownOfAScriptThePageNeverRanShouldBeSkipped()
    {
        BitBlazorUIScripts.UseLazyScripts();
        var js = new RecordingJsRuntime();

        // A dropdown disposed without ever having opened: nothing to clear, and no script to download for it.
        await js.InvokeVoid("BitBlazorUI.Callouts.clear", "id");
        Assert.AreEqual(0, js.Calls.Count, string.Join(", ", js.Calls));

        await js.InvokeVoid("BitBlazorUI.Callouts.toggle", "id");
        await js.InvokeVoid("BitBlazorUI.Callouts.clear", "id");
        Assert.AreEqual("BitBlazorUI.Callouts.clear", js.Calls.Last());
    }

    [TestMethod]
    public async Task AScriptOfTheExtrasShouldHaveWhatItUsesOfTheCoreImportedFirst()
    {
        _ = typeof(BitDataGrid<>).Assembly;
        BitBlazorUIScripts.UseLazyScripts();
        var js = new RecordingJsRuntime();

        await js.InvokeVoid("BitBlazorUI.DataGrid.focusFirst");

        var imports = js.Calls.Where(c => c.StartsWith("import ", StringComparison.Ordinal)).ToList();
        Assert.AreEqual("import ./_content/Bit.BlazorUI.Extras/scripts/modules/BitDataGrid.js", imports.Last());
        CollectionAssert.Contains(imports, "import ./_content/Bit.BlazorUI.Extras/scripts/bit.blazorui.extras.boot.js");
        CollectionAssert.Contains(imports, $"import {CoreBoot}");
        CollectionAssert.Contains(imports, "import ./_content/Bit.BlazorUI/scripts/modules/Utils.Focus.js");
        Assert.AreEqual("BitBlazorUI.DataGrid.focusFirst", js.Calls.Last());
    }

    [TestMethod]
    public async Task ACallOfAnythingButThePackagesShouldImportNothing()
    {
        BitBlazorUIScripts.UseLazyScripts();
        var js = new RecordingJsRuntime();

        await js.InvokeVoid("app.doSomething");
        await js.InvokeVoid("BitBlazorUI.NoSuchScript.fn");

        CollectionAssert.AreEqual(new[] { "app.doSomething", "BitBlazorUI.NoSuchScript.fn" }, js.Calls);
    }

    [TestMethod]
    public async Task AFailedImportShouldBeTriedAgainOnTheNextCall()
    {
        BitBlazorUIScripts.UseLazyScripts();
        var js = new RecordingJsRuntime { FailImports = 1 };

        // A lost connection or a 404 during a deploy is not the script broken for the rest of the session.
        await Assert.ThrowsExactlyAsync<JSException>(async () => await js.InvokeVoid("BitBlazorUI.Callouts.toggle", "id"));
        Assert.AreEqual(0, js.Calls.Count);

        await js.InvokeVoid("BitBlazorUI.Callouts.toggle", "id");
        CollectionAssert.AreEqual(new[] { $"import {CoreBoot}", "import ./_content/Bit.BlazorUI/scripts/modules/Callouts.js", "BitBlazorUI.Callouts.toggle" }, js.Calls);
    }

    [TestMethod]
    [DataRow("cdn/assets", "./cdn/assets/Bit.BlazorUI/")]
    [DataRow("/cdn/", "/cdn/Bit.BlazorUI/")]
    [DataRow("https://cdn.example.com/c", "https://cdn.example.com/c/Bit.BlazorUI/")]
    public async Task TheModulesShouldBeImportedFromWhereTheAppServesThem(string contentPath, string expected)
    {
        BitBlazorUIScripts.UseLazyScripts(contentPath);
        var js = new RecordingJsRuntime();

        await js.InvokeVoid("BitBlazorUI.Callouts.toggle");

        Assert.AreEqual($"import {expected}scripts/modules/Callouts.js", js.Calls[1]);
    }

    [TestMethod]
    public async Task ASynchronousCallShouldImportItsModuleBeforeItRuns()
    {
        BitBlazorUIScripts.UseLazyScripts();
        var js = new RecordingInProcessJsRuntime();

        var rtl = await js.FastInvoke<bool>("BitBlazorUI.Utils.isRtl");
        await js.FastInvokeVoid("BitBlazorUI.Utils.isRtl");

        Assert.IsTrue(rtl);
        CollectionAssert.AreEqual(new[]
        {
            $"import {CoreBoot}",
            "sync BitBlazorUI.Utils.isRtl",
            "sync BitBlazorUI.Utils.isRtl",
        }, js.Calls, "Utils.isRtl is in a script the core boots, so its boot module is all there is to import.");
    }

    [TestMethod]
    public void TheOptionsShouldSetTheMode()
    {
        new ServiceCollection().AddBitBlazorUIServices(o => o.LazyScripts = true);
        Assert.IsTrue(BitBlazorUIScripts.LazyScriptsEnabled);

        new ServiceCollection().AddBitBlazorUIServices(o => o.LazyScripts = false);
        Assert.IsFalse(BitBlazorUIScripts.LazyScriptsEnabled);

        // Left unset, the mode stays; the path still applies.
        BitBlazorUIScripts.UseLazyScripts();
        new ServiceCollection().AddBitBlazorUIServices(o => o.ContentPath = "/assets");
        Assert.IsTrue(BitBlazorUIScripts.LazyScriptsEnabled);
        Assert.AreEqual("/assets/", BitBlazorUIScripts.ContentPath);
    }

    [TestMethod]
    [DataRow(typeof(BitNav<BitNavItem>), "BitNav")]
    [DataRow(typeof(BitButtonGroup<BitButtonGroupItem>), "BitButtonGroup")]
    public void AComponentShouldLoadTheScriptActingOnItsKeysAsItFirstRenders(Type component, string script)
    {
        BitBlazorUIScripts.UseLazyScripts();
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        context.Render(builder =>
        {
            builder.OpenComponent(0, component);
            builder.CloseComponent();
        });

        Assert.IsTrue(Imported(context, script), $"{component.Name} did not load {script}: {Invocations(context)}");
    }

    [TestMethod]
    public void ATagShouldLoadItsKeysScriptOnceItDismissesOnThem()
    {
        BitBlazorUIScripts.UseLazyScripts();
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        // A tag nothing dismisses renders no data-bit-tag-dsm, which is all the script acts on.
        var tag = context.Render<BitTag>();
        Assert.IsFalse(Imported(context, "BitTag"), $"A tag that is not dismissible loaded its keys script: {Invocations(context)}");

        tag.Render(parameters => parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => { })));
        Assert.IsTrue(Imported(context, "BitTag"), $"A dismissible tag did not load its keys script: {Invocations(context)}");
    }

    [TestMethod]
    public void ATimelineShouldLoadItsKeysScriptOnceAnItemIsClickable()
    {
        BitBlazorUIScripts.UseLazyScripts();
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        // A read-only timeline renders no bit-tln-btn, which is all the script acts on.
        var timeline = context.Render<BitTimeline<BitTimelineItem>>(parameters => parameters.Add(p => p.Items, [new BitTimelineItem { PrimaryText = "One" }]));
        Assert.IsFalse(Imported(context, "Timelines"), $"A read-only timeline loaded its keys script: {Invocations(context)}");

        timeline.Render(parameters => parameters.Add(p => p.OnItemClick, (BitTimelineItem _) => { }));
        Assert.IsTrue(Imported(context, "Timelines"), $"A clickable timeline did not load its keys script: {Invocations(context)}");
    }

    [TestMethod]
    public void AScriptAComponentFailsToImportShouldNotFailItsRender()
    {
        BitBlazorUIScripts.UseLazyScripts();
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupVoid("import", _ => true).SetException(new JSException("Failed to fetch dynamically imported module"));

        // Thrown out of OnAfterRenderAsync, a lost connection or a 404 during a deploy would end a Server circuit.
        var tag = context.Render<BitTag>(parameters => parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => { })));

        Assert.IsTrue(context.JSInterop.Invocations.Any(i => i.Identifier == "import"), $"The tag asked for no script: {Invocations(context)}");
        Assert.IsNotNull(tag.Find(".bit-tag"));
    }

    private static bool Imported(BunitContext context, string script)
        => context.JSInterop.Invocations.Any(i => i.Identifier == "import" && Equals(i.Arguments[0], $"./_content/Bit.BlazorUI/scripts/modules/{script}.js"));

    private static string Invocations(BunitContext context) => string.Join(", ", context.JSInterop.Invocations.Select(i => i.Identifier));

    [TestMethod]
    public void InBundleModeAComponentShouldLoadNothing()
    {
        BitBlazorUIScripts.UseBundledScripts();
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        context.Render<BitTag>();

        Assert.IsFalse(context.JSInterop.Invocations.Any(i => i.Identifier == "import"));
    }

    private class RecordingJsRuntime : IJSRuntime
    {
        public List<string> Calls { get; } = [];

        public int FailImports { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import")
            {
                if (FailImports > 0)
                {
                    FailImports--;
                    return ValueTask.FromException<TValue>(new JSException("Failed to fetch dynamically imported module"));
                }

                Calls.Add($"import {args![0]}");
            }
            else
            {
                Calls.Add(identifier);
            }

            return new(default(TValue)!);
        }
    }

    private sealed class RecordingInProcessJsRuntime : RecordingJsRuntime, IJSInProcessRuntime
    {
        public TResult Invoke<TResult>(string identifier, params object?[]? args)
        {
            Calls.Add($"sync {identifier}");
            return typeof(TResult) == typeof(bool) ? (TResult)(object)true : default!;
        }
    }
}
