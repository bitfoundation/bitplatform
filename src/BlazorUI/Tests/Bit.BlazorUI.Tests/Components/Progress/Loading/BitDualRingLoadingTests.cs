using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitDualRingLoadingTests : BitLoadingTestsBase<BitDualRingLoading>
{
    protected override string RootClass => "bit-ldn-dur";

    protected override int ChildCount => 0;

    protected override string[] ScaledVariables => ["--bit-ldn-dur-6", "--bit-ldn-dur-8", "--bit-ldn-dur-64"];
}
