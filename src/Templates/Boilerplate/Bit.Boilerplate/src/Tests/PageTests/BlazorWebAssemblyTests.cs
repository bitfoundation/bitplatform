using Boilerplate.Client.Web;

namespace Boilerplate.Tests.PageTests.BlazorWebAssembly;

[TestClass]
public partial class IdentityPagesTests : BlazorServer.IdentityPagesTests
{
    public override BlazorWebAppMode BlazorRenderMode => BlazorWebAppMode.BlazorWebAssembly;
}

[TestClass]
public partial class LocalizationTests : BlazorServer.LocalizationTests
{
    public override BlazorWebAppMode BlazorRenderMode => BlazorWebAppMode.BlazorWebAssembly;
}
