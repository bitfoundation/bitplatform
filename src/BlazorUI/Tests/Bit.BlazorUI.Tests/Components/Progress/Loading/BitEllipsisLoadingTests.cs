using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitEllipsisLoadingTests : BitLoadingTestsBase<BitEllipsisLoading>
{
    protected override string RootClass => "bit-ldn-elp";

    protected override int ChildCount => 4;
}
