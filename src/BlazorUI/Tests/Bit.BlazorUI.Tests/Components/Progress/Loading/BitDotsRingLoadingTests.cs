using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitDotsRingLoadingTests : BitLoadingTestsBase<BitDotsRingLoading>
{
    protected override string RootClass => "bit-ldn-dor";

    protected override int ChildCount => 12;

    protected override string[] ScaledVariables => ["--bit-ldn-dor-6", "--bit-ldn-dor-7", "--bit-ldn-dor-11", "--bit-ldn-dor-22", "--bit-ldn-dor-37", "--bit-ldn-dor-52", "--bit-ldn-dor-62", "--bit-ldn-dor-66"];
}
