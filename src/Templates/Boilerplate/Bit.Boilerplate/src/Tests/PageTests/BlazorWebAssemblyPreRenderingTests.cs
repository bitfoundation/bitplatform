using Boilerplate.Client.Web;

namespace Boilerplate.Tests.PageTests.BlazorWebAssembly.PreRendering;

[TestClass]
public partial class IdentityPagesTests : BlazorWebAssembly.IdentityPagesTests
{
    public override bool PreRenderEnabled => true;
}

[TestClass]
public partial class LocalizationTests : BlazorWebAssembly.LocalizationTests
{
    public override bool PreRenderEnabled => true;
}

[TestClass]
public partial class PreRenderingTests : BlazorServer.PreRendering.PreRenderingTests
{
    public override BlazorWebAppMode BlazorRenderMode => BlazorWebAppMode.BlazorWebAssembly;
}
