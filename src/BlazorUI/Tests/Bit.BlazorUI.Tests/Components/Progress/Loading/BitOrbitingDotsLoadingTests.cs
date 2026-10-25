using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitOrbitingDotsLoadingTests : BitLoadingTestsBase<BitOrbitingDotsLoading>
{
    protected override string RootClass => "bit-ldn-ord";

    protected override int ChildCount => 2;
}
