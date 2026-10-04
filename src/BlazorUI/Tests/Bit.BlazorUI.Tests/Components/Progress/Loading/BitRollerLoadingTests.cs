using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitRollerLoadingTests : BitLoadingTestsBase<BitRollerLoading>
{
    protected override string RootClass => "bit-ldn-rol";

    protected override int ChildCount => 8;
}
