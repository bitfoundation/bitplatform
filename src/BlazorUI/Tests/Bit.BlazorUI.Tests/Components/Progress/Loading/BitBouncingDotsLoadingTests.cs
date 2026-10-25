using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitBouncingDotsLoadingTests : BitLoadingTestsBase<BitBouncingDotsLoading>
{
    protected override string RootClass => "bit-ldn-bnd";

    protected override int ChildCount => 3;
}
