using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

[TestClass]
public class BitXboxLoadingTests : BitLoadingTestsBase<BitXboxLoading>
{
    protected override string RootClass => "bit-ldn-xbx";

    protected override int ChildCount => 3;

    protected override string[] ScaledVariables => ["--bit-ldn-xbx-3", "--bit-ldn-xbx-25f", "--bit-ldn-xbx-50f", "--bit-ldn-xbx-125f", "--bit-ldn-xbx-75f"];
}
