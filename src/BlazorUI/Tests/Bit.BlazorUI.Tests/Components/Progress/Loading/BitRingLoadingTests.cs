using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitRingLoadingTests : BitLoadingTestsBase<BitRingLoading>
{
    protected override string RootClass => "bit-ldn-rng";

    protected override int ChildCount => 4;

    protected override string[] ScaledVariables => ["--bit-ldn-rng-8", "--bit-ldn-rng-64"];
}
