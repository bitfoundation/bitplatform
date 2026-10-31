using Bit.Utilities.Testing;
using Microsoft.JSInterop;

namespace Bit.Blow.Tests;

/// <summary>
/// What keeps Bit.Blow lazy loadable on WebAssembly: the guide's "Lazy loading on WebAssembly" says an app can download
/// it only when a page with a canvas opens, with nothing of Blow's in <c>Program.cs</c>. The checks are the ones every
/// library that says so shares.
/// </summary>
[TestClass]
public class LazyLoadingTests
{
    [TestMethod]
    public void ComponentsInjectOnlyWhatEveryBlazorHostRegisters()
    {
        // IJSRuntime is what the canvas measures through.
        LazyLoadingAssert.ComponentsInjectOnlyWhatEveryHostRegisters(typeof(BlowCanvas).Assembly, (typeof(BlowCanvas), typeof(IJSRuntime)));
    }

    [TestMethod]
    public void TheLibraryHasNoRegistrationToCall()
    {
        // The guide says there is no AddBlow... method, and lazy loading depends on there being none to call.
        LazyLoadingAssert.HasNothingForStartupToCall(typeof(BlowCanvas).Assembly);
    }
}
