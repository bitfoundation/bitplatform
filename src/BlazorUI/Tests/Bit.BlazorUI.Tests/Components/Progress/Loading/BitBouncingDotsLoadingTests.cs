using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitBouncingDotsLoadingTests : BitLoadingTestsBase<BitBouncingDotsLoading>
{
    protected override string RootClass => "bit-ldn-bnd";

    protected override int ChildCount => 3;

    protected override string[] ScaledVariables => ["--bit-ldn-bnd-6", "--bit-ldn-bnd-15"];
}
