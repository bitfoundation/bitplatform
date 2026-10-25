using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitGridLoadingTests : BitLoadingTestsBase<BitGridLoading>
{
    protected override string RootClass => "bit-ldn-grd";

    protected override int ChildCount => 9;
}
