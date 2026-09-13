using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitRollingDashesLoadingTests : BitLoadingTestsBase<BitRollingDashesLoading>
{
    protected override string RootClass => "bit-ldn-rld";

    protected override int ChildCount => 1;

    protected override string[] ScaledVariables => ["--bit-ldn-rld-8", "--bit-ldn-rld-15"];
}
