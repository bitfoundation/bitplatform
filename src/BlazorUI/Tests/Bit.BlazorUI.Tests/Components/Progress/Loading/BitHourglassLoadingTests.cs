using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitHourglassLoadingTests : BitLoadingTestsBase<BitHourglassLoading>
{
    protected override string RootClass => "bit-ldn-hgl";

    protected override int ChildCount => 0;

    protected override string[] ScaledVariables => ["--bit-ldn-hgl-8", "--bit-ldn-hgl-32"];
}
