using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitSpinnerLoadingTests : BitLoadingTestsBase<BitSpinnerLoading>
{
    protected override string RootClass => "bit-ldn-spn";

    protected override int ChildCount => 12;

    protected override string[] ScaledVariables => ["--bit-ldn-spn-3", "--bit-ldn-spn-6", "--bit-ldn-spn-18", "--bit-ldn-spn-40"];
}
