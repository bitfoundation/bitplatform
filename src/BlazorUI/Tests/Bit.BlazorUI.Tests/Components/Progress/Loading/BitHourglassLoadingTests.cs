using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitHourglassLoadingTests : BitLoadingTestsBase<BitHourglassLoading>
{
    protected override string RootClass => "bit-ldn-hgl";

    protected override int ChildCount => 0;
}
