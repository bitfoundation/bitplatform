using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitDotsRingLoadingTests : BitLoadingTestsBase<BitDotsRingLoading>
{
    protected override string RootClass => "bit-ldn-dor";

    protected override int ChildCount => 12;
}
