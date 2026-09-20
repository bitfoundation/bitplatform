using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitEllipsisLoadingTests : BitLoadingTestsBase<BitEllipsisLoading>
{
    protected override string RootClass => "bit-ldn-elp";

    protected override int ChildCount => 4;

    protected override string[] ScaledVariables => ["--bit-ldn-elp-8", "--bit-ldn-elp-13", "--bit-ldn-elp-24", "--bit-ldn-elp-32", "--bit-ldn-elp-33", "--bit-ldn-elp-56"];
}
