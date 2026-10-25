using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitXboxLoadingTests : BitLoadingTestsBase<BitXboxLoading>
{
    protected override string RootClass => "bit-ldn-xbx";

    protected override int ChildCount => 3;
}
