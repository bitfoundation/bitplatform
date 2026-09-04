using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitRollerLoadingTests : BitLoadingTestsBase<BitRollerLoading>
{
    protected override string RootClass => "bit-ldn-rol";

    protected override int ChildCount => 8;

    protected override string[] ScaledVariables => ["--bit-ldn-rol-4", "--bit-ldn-rol-8", "--bit-ldn-rol-12", "--bit-ldn-rol-17", "--bit-ldn-rol-24", "--bit-ldn-rol-32", "--bit-ldn-rol-40", "--bit-ldn-rol-48", "--bit-ldn-rol-56", "--bit-ldn-rol-63", "--bit-ldn-rol-68", "--bit-ldn-rol-71", "--bit-ldn-rol-72"];
}
