using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitSlickBarsLoadingTests : BitLoadingTestsBase<BitSlickBarsLoading>
{
    protected override string RootClass => "bit-ldn-sbr";

    protected override int ChildCount => 6;

    protected override string[] ScaledVariables => ["--bit-ldn-sbr-2", "--bit-ldn-sbr-8"];
}
