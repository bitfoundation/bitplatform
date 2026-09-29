using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitBarsLoadingTests : BitLoadingTestsBase<BitBarsLoading>
{
    protected override string RootClass => "bit-ldn-bar";

    protected override int ChildCount => 3;
}
