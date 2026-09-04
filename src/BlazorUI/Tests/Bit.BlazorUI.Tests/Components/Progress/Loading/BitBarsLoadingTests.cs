using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitBarsLoadingTests : BitLoadingTestsBase<BitBarsLoading>
{
    protected override string RootClass => "bit-ldn-bar";

    protected override int ChildCount => 3;

    protected override string[] ScaledVariables => ["--bit-ldn-bar-8", "--bit-ldn-bar-16", "--bit-ldn-bar-24", "--bit-ldn-bar-32", "--bit-ldn-bar-56", "--bit-ldn-bar-64"];
}
