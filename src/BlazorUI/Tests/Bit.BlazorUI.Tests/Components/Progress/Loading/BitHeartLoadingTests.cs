using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitHeartLoadingTests : BitLoadingTestsBase<BitHeartLoading>
{
    protected override string RootClass => "bit-ldn-hrt";

    protected override int ChildCount => 1;
}
