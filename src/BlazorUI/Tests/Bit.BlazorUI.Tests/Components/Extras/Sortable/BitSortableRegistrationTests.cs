using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// A list's registration with its script while it is still out - a slow lazy-module download on Server, a call that
/// times out: an option changed meanwhile is never sent as if the list were registered, it reaches the script once the
/// registration is taken, and a registration that never arrived is made again on the next render.
/// </summary>
[TestClass]
public class BitSortableRegistrationTests : BitSortableTestContext
{
    [TestMethod]
    public void ARegistrationThatFailsAfterAnOptionChangedIsMadeAgain()
    {
        var js = UseRuntime();
        var list = RenderList(["A"]);

        // An option changes while the registration is out: the list is not registered yet, so nothing is configured.
        list.Render(p => p.Add(c => c.Animation, 300));
        Assert.IsEmpty(js.Calls.Where(c => c.Identifier == Configure));

        // The registration never reaches the browser (the call timed out).
        js.FirstRegister.SetCanceled();

        list.WaitForAssertion(() => Assert.HasCount(1, js.Calls.Where(c => c.Identifier == Register)));
        list.Render();

        var registers = js.Calls.Where(c => c.Identifier == Register).ToList();
        Assert.HasCount(2, registers, "The list was left without its script.");
        Assert.AreEqual(300, ((BitSortableJsOptions)registers[^1].Arguments[^1]!).Animation);
    }

    [TestMethod]
    public void AnOptionChangedWhileTheRegistrationIsOutReachesTheScriptOnceItIsTaken()
    {
        var js = UseRuntime();
        var list = RenderList(["A"]);

        list.Render(p => p.Add(c => c.Animation, 300));
        Assert.IsEmpty(js.Calls.Where(c => c.Identifier == Configure));

        js.FirstRegister.SetResult();

        list.WaitForAssertion(() =>
        {
            var configure = js.Calls.Where(c => c.Identifier == Configure).ToList();
            Assert.HasCount(1, configure);
            Assert.AreEqual(300, ((BitSortableJsOptions)configure[0].Arguments[^1]!).Animation);
        });
        Assert.ContainsSingle(js.Calls.Where(c => c.Identifier == Register));
    }

    private PendingRegisterJSRuntime UseRuntime()
    {
        var js = new PendingRegisterJSRuntime();
        Services.AddSingleton<IJSRuntime>(js);
        return js;
    }

    private sealed record Call(string Identifier, object?[] Arguments);

    private sealed class PendingRegisterJSRuntime : IJSRuntime
    {
        // The first registration stays out until the test completes or cancels it, as a call does until the browser
        // answers; every other call answers at once.
        public TaskCompletionSource FirstRegister { get; } = new();

        private int _registers;

        public List<Call> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(new(identifier, args ?? []));
            if (identifier == Register && ++_registers == 1) return Pending<TValue>();
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        private async ValueTask<TValue> Pending<TValue>()
        {
            await FirstRegister.Task;
            return default!;
        }
    }
}
