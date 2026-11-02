using Bit.Utilities.Testing;
using Microsoft.JSInterop;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// What Bink takes from the application's container. An app that lazy loads Bit.Bink on WebAssembly cannot register
/// anything of Bink's: <c>Program.cs</c> runs before the assembly is there. The checks are the ones every library that
/// lazy loads this way shares.
/// </summary>
[TestClass]
public sealed class ComponentServicesTests
{
    [TestMethod]
    public void Components_inject_only_what_every_Blazor_host_registers()
    {
        LazyLoadingAssert.ComponentsInjectOnlyWhatEveryHostRegisters(typeof(BinkCanvas).Assembly, (typeof(BinkCanvas), typeof(IJSRuntime)));
    }

    [TestMethod]
    public void The_library_has_nothing_for_startup_to_call()
    {
        LazyLoadingAssert.HasNothingForStartupToCall(typeof(BinkCanvas).Assembly);
    }
}
