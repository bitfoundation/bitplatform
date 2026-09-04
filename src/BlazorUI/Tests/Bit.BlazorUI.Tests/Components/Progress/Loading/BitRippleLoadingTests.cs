using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitRippleLoadingTests : BitLoadingTestsBase<BitRippleLoading>
{
    protected override string RootClass => "bit-ldn-rpl";

    protected override int ChildCount => 2;

    protected override string[] ScaledVariables => ["--bit-ldn-rpl-4", "--bit-ldn-rpl-8", "--bit-ldn-rpl-80"];
}
