using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitSpinnerLoadingTests : BitLoadingTestsBase<BitSpinnerLoading>
{
    protected override string RootClass => "bit-ldn-spn";

    protected override int ChildCount => 12;
}
