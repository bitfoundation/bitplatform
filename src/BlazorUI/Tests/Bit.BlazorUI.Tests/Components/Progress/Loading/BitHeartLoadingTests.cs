using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitHeartLoadingTests : BitLoadingTestsBase<BitHeartLoading>
{
    protected override string RootClass => "bit-ldn-hrt";

    protected override int ChildCount => 1;

    protected override string[] ScaledVariables => ["--bit-ldn-hrt-24", "--bit-ldn-hrt-28", "--bit-ldn-hrt-32", "--bit-ldn-hrt-40"];
}
