using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitGridLoadingTests : BitLoadingTestsBase<BitGridLoading>
{
    protected override string RootClass => "bit-ldn-grd";

    protected override int ChildCount => 9;

    protected override string[] ScaledVariables => ["--bit-ldn-grd-8", "--bit-ldn-grd-16", "--bit-ldn-grd-32", "--bit-ldn-grd-56"];
}
