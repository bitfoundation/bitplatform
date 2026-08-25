using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitRollingSquareLoadingTests : BitLoadingTestsBase<BitRollingSquareLoading>
{
    protected override string RootClass => "bit-ldn-rsq";

    protected override int ChildCount => 1;

    protected override string[] ScaledVariables => ["--bit-ldn-rsq-4", "--bit-ldn-rsq-20"];
}
