namespace Boilerplate.Tests.PageTests.BlazorServer.PreRendering;

[TestClass]
public partial class IdentityPagesTests : BlazorServer.IdentityPagesTests
{
    public override bool PreRenderEnabled => true;
}

[TestClass]
public partial class LocalizationTests : BlazorServer.LocalizationTests
{
    public override bool PreRenderEnabled => true;
}
