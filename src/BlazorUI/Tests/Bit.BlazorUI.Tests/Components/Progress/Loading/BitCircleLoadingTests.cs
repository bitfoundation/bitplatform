using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitCircleLoadingTests : BitLoadingTestsBase<BitCircleLoading>
{
    protected override string RootClass => "bit-ldn-cir";

    protected override int ChildCount => 1;

    protected override int OriginalSize => 64;
}
