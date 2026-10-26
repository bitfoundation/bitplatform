using System.Net;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.SourceGenerators.Tests.ProxyTestHost;

namespace Bit.SourceGenerators.Tests;

[TestClass]
public class HttpClientProxySourceGeneratorTests
{
    private const string BoilerplateControllers = """
        global using Boilerplate.Shared.Features.Products;
        global using Boilerplate.Shared.Features.Identity;
        global using Boilerplate.Shared.Features.Statistics;
        global using Boilerplate.Shared.Features.MinimalApiSample;
        global using Boilerplate.Shared.Features.Diagnostic;

        namespace Boilerplate.Shared.Features.Products
        {
            public class ProductDto
            {
                public Guid Id { get; set; }
                public string? Name { get; set; }
                public decimal Price { get; set; }
            }

            public class PagedResponse<T>
            {
                public List<T> Items { get; set; } = [];
                public long TotalCount { get; set; }
            }

            [Route("api/v1/[controller]/[action]/")]
            [AuthorizedApi]
            public interface IProductController : IAppController
            {
                [HttpGet]
                Task<List<ProductDto>> Get(CancellationToken cancellationToken) => default!;

                [HttpGet]
                Task<PagedResponse<ProductDto>> GetProducts(CancellationToken cancellationToken) => default!;

                [HttpGet("{searchQuery}")]
                Task<PagedResponse<ProductDto>> SearchProducts(string searchQuery, CancellationToken cancellationToken) => default!;

                [HttpGet("{id}")]
                Task<ProductDto> Get(Guid id, CancellationToken cancellationToken);

                [HttpPost]
                Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken);

                [HttpPut]
                Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken);

                [HttpDelete("{id}/{version}")]
                Task Delete(Guid id, long version, CancellationToken cancellationToken);
            }
        }

        namespace Boilerplate.Shared.Features.Identity
        {
            public class SignInRequestDto
            {
                public string? UserName { get; set; }
                public string? Password { get; set; }
            }

            public class TokenResponseDto
            {
                public string? AccessToken { get; set; }
            }

            public class IdentityRequestDto
            {
                public string? Email { get; set; }
            }

            public class TwoFactorAuthRequestDto
            {
                public bool? Enable { get; set; }
            }

            public class TwoFactorAuthResponseDto
            {
                public bool IsTwoFactorEnabled { get; set; }
            }

            [Route("api/v1/[controller]/[action]/")]
            public interface IIdentityController : IAppController
            {
                public const string RefreshUri = "api/v1/Identity/Refresh";

                [HttpPost]
                Task<TokenResponseDto> SignIn(SignInRequestDto request, CancellationToken cancellationToken) => default!;

                [HttpPost("{?returnUrl}")]
                Task SendOtp(IdentityRequestDto request, string? returnUrl = null, CancellationToken cancellationToken = default);

                [HttpGet("{?provider,returnUrl,localHttpPort}")]
                Task<string> GetExternalSignInUri(string provider, string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default);

                [HttpPost]
                Task<JsonElement> VerifyWebAuthAssertion(JsonElement clientResponse, CancellationToken cancellationToken) => default!;

                [HttpGet]
                Task<string[]> GetSupportedExternalAuthSchemes(CancellationToken cancellationToken);
            }

            [Route("api/v1/[controller]/[action]/"), AuthorizedApi]
            public interface IUserController : IAppController
            {
                [HttpPost, NoRetryPolicy]
                Task SignOut(CancellationToken cancellationToken);

                [HttpPost]
                [Route("~/api/v1/[controller]/2fa")]
                Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken) => default!;

                [HttpPost("{enabled}")]
                Task SetNotificationEnabled(bool enabled, CancellationToken cancellationToken);
            }
        }

        namespace Boilerplate.Shared.Features.Statistics
        {
            public class GitHubStats
            {
                public int StargazersCount { get; set; }
            }

            [Route("api/v1/[controller]/[action]/")]
            public interface IStatisticsController : IAppController
            {
                [HttpGet, Route("https://api.github.com/repos/bitfoundation/bitplatform"), ExternalApi]
                Task<GitHubStats> GetGitHubStats(CancellationToken cancellationToken) => default!;
            }
        }

        namespace Boilerplate.Shared.Features.MinimalApiSample
        {
            public interface IMinimalApiSampleController : IAppController
            {
                [HttpGet("api/minimal-api-sample/{routeParameter}{?queryStringParameter}")]
                Task<JsonElement> MinimalApiSample(string routeParameter, string? queryStringParameter, CancellationToken cancellationToken);
            }
        }

        namespace Boilerplate.Shared.Features.Diagnostic
        {
            [Route("api/v1/[controller]/[action]/")]
            public interface IDiagnosticController : IAppController
            {
                [HttpGet("{?signalRConnectionId,pushNotificationSubscriptionDeviceId}")]
                IAsyncEnumerable<string> PerformDiagnostic(string? signalRConnectionId, string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken);

                [HttpPost("{deviceId}"), NoRetryPolicy]
                Task<bool> SendTestPushNotification(string deviceId, CancellationToken cancellationToken);
            }
        }
        """;

    private static string Url(string path) => BaseAddress + path;

    private static Task<HttpResponseMessage> Text(string text)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "text/plain") });
    }

    [TestMethod]
    public async Task Boilerplate_controllers_compile_cleanly_and_are_registered()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    Type[] controllers = [typeof(IProductController), typeof(IIdentityController), typeof(IUserController), typeof(IStatisticsController), typeof(IMinimalApiSampleController), typeof(IDiagnosticController)];
                    return string.Join(",", controllers.Select(controller => sp.GetService(controller) is ICloneable ? "ok" : "missing"));
            """);

        Assert.AreEqual("ok,ok,ok,ok,ok,ok", proxy.Result);
    }

    [TestMethod]
    public async Task Get_puts_the_route_value_in_the_url_and_sends_the_request_options()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var product = await sp.GetRequiredService<IProductController>().Get(Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"), default);
                    return product.Name + "|" + string.Join(",", prerenderKeys);
            """, (_, _) => Json("""{"name":"Laptop"}"""));

        Assert.AreEqual("Laptop|api/v1/Product/Get/6f9619ff-8b86-d011-b42d-00c04fc964ff", proxy.Result);
        var request = proxy.Request;
        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual(Url("api/v1/Product/Get/6f9619ff-8b86-d011-b42d-00c04fc964ff"), request.Uri!.AbsoluteUri);
        Assert.IsNull(request.Content);
        Assert.AreEqual("IProductController", ((Type)request.Options["IControllerType"]!).Name);
        Assert.AreEqual("Get", request.Options["ActionName"]);
        var parameters = (Dictionary<string, Type>)request.Options["ActionParametersInfo"]!;
        Assert.AreEqual(typeof(Guid), parameters["id"]);
        Assert.AreEqual(typeof(CancellationToken), parameters["cancellationToken"]);
        Assert.AreEqual("ProductDto", ((Type)request.Options["ResponseType"]!).Name);
        Assert.IsFalse(request.Options.ContainsKey("RequestType"));
    }

    [TestMethod]
    public async Task Post_sends_the_remaining_parameter_as_the_json_body()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var created = await sp.GetRequiredService<IProductController>().Create(new ProductDto { Name = "Phone", Price = 12.5m }, default);
                    return created.Name;
            """, (request, _) => Json(request.Content!));

        Assert.AreEqual("Phone", proxy.Result);
        var request = proxy.Request;
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual(Url("api/v1/Product/Create"), request.Uri!.AbsoluteUri);
        Assert.AreEqual("application/json", request.ContentType);
        StringAssert.Contains(request.Content, "\"name\":\"Phone\"");
        StringAssert.Contains(request.Content, "\"price\":12.5");
        Assert.AreEqual("ProductDto", ((Type)request.Options["RequestType"]!).Name);
        Assert.AreEqual("ProductDto", ((Type)request.Options["ResponseType"]!).Name);
    }

    [TestMethod]
    public async Task Put_and_delete_use_their_methods_and_a_task_skips_the_prerender_state()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var controller = sp.GetRequiredService<IProductController>();
                    await controller.Update(new ProductDto { Name = "x" }, default);
                    await controller.Delete(Guid.Empty, 42, default);
                    return string.Join(",", prerenderKeys);
            """);

        Assert.AreEqual("api/v1/Product/Update", proxy.Result);
        Assert.AreEqual(HttpMethod.Put, proxy.Requests[0].Method);
        Assert.AreEqual(Url("api/v1/Product/Update"), proxy.Requests[0].Uri!.AbsoluteUri);
        Assert.AreEqual(HttpMethod.Delete, proxy.Requests[1].Method);
        Assert.AreEqual(Url("api/v1/Product/Delete/00000000-0000-0000-0000-000000000000/42"), proxy.Requests[1].Uri!.AbsoluteUri);
        Assert.IsNull(proxy.Requests[1].Content);
    }

    [TestMethod]
    public async Task Query_expression_adds_query_parameters_and_a_string_result_is_read_as_text()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var controller = sp.GetRequiredService<IIdentityController>();
                    var uri = await controller.GetExternalSignInUri("Google", "/home?a=b", 5000);
                    await controller.GetExternalSignInUri("Google");
                    return uri;
            """, (_, _) => Text("https://accounts.example/o?x=1"));

        Assert.AreEqual("https://accounts.example/o?x=1", proxy.Result);
        Assert.AreEqual(Url("api/v1/Identity/GetExternalSignInUri/?provider=Google&returnUrl=%2Fhome%3Fa%3Db&localHttpPort=5000"), proxy.Requests[0].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/v1/Identity/GetExternalSignInUri/?provider=Google&returnUrl=&localHttpPort="), proxy.Requests[1].Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Body_and_query_parameters_can_be_combined()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    await sp.GetRequiredService<IIdentityController>().SendOtp(new IdentityRequestDto { Email = "a@b.c" }, "/settings");
                    return null;
            """);

        Assert.AreEqual(HttpMethod.Post, proxy.Request.Method);
        Assert.AreEqual(Url("api/v1/Identity/SendOtp/?returnUrl=%2Fsettings"), proxy.Request.Uri!.AbsoluteUri);
        StringAssert.Contains(proxy.Request.Content, "\"email\":\"a@b.c\"");
    }

    [TestMethod]
    public async Task Minimal_api_route_needs_no_controller_route()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var element = await sp.GetRequiredService<IMinimalApiSampleController>().MinimalApiSample("abc", "x y", default);
                    return element.GetProperty("ok").GetBoolean();
            """, (_, _) => Json("""{"ok":true}"""));

        Assert.IsTrue((bool)proxy.Result!);
        Assert.AreEqual(Url("api/minimal-api-sample/abc?queryStringParameter=x%20y"), proxy.Request.Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Absolute_and_app_root_routes_replace_the_controller_route()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var stats = await sp.GetRequiredService<IStatisticsController>().GetGitHubStats(default);
                    await sp.GetRequiredService<IUserController>().TwoFactorAuth(new TwoFactorAuthRequestDto { Enable = true }, default);
                    await sp.GetRequiredService<IUserController>().SetNotificationEnabled(true, default);
                    return stats.StargazersCount;
            """, (request, _) => Json(request.Uri!.Host == "api.github.com" ? """{"stargazersCount":7}""" : "null"));

        Assert.AreEqual(7, proxy.Result);
        Assert.AreEqual("https://api.github.com/repos/bitfoundation/bitplatform", proxy.Requests[0].Uri!.AbsoluteUri);
        Assert.AreEqual(HttpMethod.Post, proxy.Requests[1].Method);
        Assert.AreEqual(Url("api/v1/User/2fa"), proxy.Requests[1].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/v1/User/SetNotificationEnabled/True"), proxy.Requests[2].Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task String_route_values_are_escaped()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    await sp.GetRequiredService<IProductController>().SearchProducts("a/b c?#%", default);
                    return null;
            """);

        Assert.AreEqual(Url("api/v1/Product/SearchProducts/a%2Fb%20c%3F%23%25"), proxy.Request.Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Route_and_query_values_are_formatted_with_the_invariant_culture()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public interface IPriceController : IAppController
            {
                [HttpGet("{price}/{?ratio,when}")]
                Task Find(decimal price, double? ratio, DateTime when, CancellationToken cancellationToken);
            }
            """, """
                    System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                    await sp.GetRequiredService<IPriceController>().Find(1.5m, 0.25, new DateTime(2026, 1, 2, 3, 4, 5), default);
                    return null;
            """);

        Assert.AreEqual(Url("api/Price/Find/1.5/?ratio=0.25&when=01%2F02%2F2026%2003%3A04%3A05"), proxy.Request.Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task WithQuery_adds_the_query_string_to_one_request_only()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var products = sp.GetRequiredService<IProductController>();
                    await products.WithQuery("$top", 10).Get(default);
                    await products.Get(default);
                    await sp.GetRequiredService<IIdentityController>().WithQuery("culture", "fa-IR").GetExternalSignInUri("Google");
                    return null;
            """);

        Assert.AreEqual(Url("api/v1/Product/Get?%24top=10"), proxy.Requests[0].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/v1/Product/Get"), proxy.Requests[1].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/v1/Identity/GetExternalSignInUri/?provider=Google&returnUrl=&localHttpPort=&culture=fa-IR"), proxy.Requests[2].Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task WithQuery_with_a_repeated_key_keeps_the_last_value()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var products = sp.GetRequiredService<IProductController>();
                    await products.WithQuery("page", 1).WithQuery("page", 2).Get(default);
                    await products.WithQuery("?page=1&size=5").WithQuery("PAGE", 3).Get(default);
                    return null;
            """);

        Assert.AreEqual(Url("api/v1/Product/Get?page=2"), proxy.Requests[0].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/v1/Product/Get?page=3&size=5"), proxy.Requests[1].Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task WithQuery_formats_values_with_the_invariant_culture()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                    await sp.GetRequiredService<IProductController>().WithQuery("price", 1.5m).Get(default);
                    return null;
            """);

        Assert.AreEqual(Url("api/v1/Product/Get?price=1.5"), proxy.Request.Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Async_stream_is_streamed_and_skips_the_prerender_state()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    var items = new List<string>();
                    await foreach (var item in sp.GetRequiredService<IDiagnosticController>().PerformDiagnostic("conn", null, default))
                    {
                        items.Add(item);
                    }
                    return string.Join(",", items) + "|" + prerenderKeys.Count;
            """, (_, _) => Json("""["a","b"]"""));

        Assert.AreEqual("a,b|0", proxy.Result);
        Assert.AreEqual(Url("api/v1/Diagnostic/PerformDiagnostic/?signalRConnectionId=conn&pushNotificationSubscriptionDeviceId="), proxy.Request.Uri!.AbsoluteUri);
        Assert.AreEqual(typeof(string), proxy.Request.Options["ResponseType"]);
    }

    [TestMethod]
    public async Task Task_of_async_enumerable_and_value_tasks_are_supported()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]")]
            public interface IStreamController : IAppController
            {
                [HttpGet]
                Task<IAsyncEnumerable<string>> Numbers(CancellationToken cancellationToken);

                [HttpGet]
                ValueTask<int> Count(CancellationToken cancellationToken);

                [HttpGet]
                ValueTask Ping(CancellationToken cancellationToken);
            }
            """, """
                    var controller = sp.GetRequiredService<IStreamController>();
                    var numbers = new List<string>();
                    await foreach (var number in await controller.Numbers(default))
                    {
                        numbers.Add(number);
                    }
                    var count = await controller.Count(default);
                    await controller.Ping(default);
                    return string.Join(",", numbers) + "|" + count;
            """, (request, _) => Json(request.Uri!.AbsolutePath.EndsWith("Numbers") ? """["1","2","3"]""" : request.Uri.AbsolutePath.EndsWith("Count") ? "3" : "null"));

        Assert.AreEqual("1,2,3|3", proxy.Result);
        Assert.AreEqual(Url("api/Stream/Ping"), proxy.Requests[2].Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Task_of_async_enumerable_skips_the_prerender_state()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]")]
            public interface ILazyStreamController : IAppController
            {
                [HttpGet]
                Task<IAsyncEnumerable<string>> Numbers(CancellationToken cancellationToken);
            }
            """, """
                    var numbers = new List<string>();
                    await foreach (var number in await sp.GetRequiredService<ILazyStreamController>().Numbers(default))
                    {
                        numbers.Add(number);
                    }
                    return string.Join(",", numbers) + "|" + prerenderKeys.Count;
            """, (_, _) => Json("""["1","2"]"""));

        Assert.AreEqual("1,2|0", proxy.Result);
        Assert.AreEqual(Url("api/LazyStream/Numbers"), proxy.Request.Uri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Cancellation_token_reaches_the_request()
    {
        var proxy = await RunAsync(BoilerplateControllers, """
                    async Task<string> Run(Func<CancellationToken, Task> call)
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
                        try
                        {
                            await call(cts.Token);
                            return "completed";
                        }
                        catch (OperationCanceledException)
                        {
                            return "canceled";
                        }
                    }

                    var task = await Run(token => sp.GetRequiredService<IProductController>().Get(Guid.Empty, token));
                    var stream = await Run(async token =>
                    {
                        await foreach (var _ in sp.GetRequiredService<IDiagnosticController>().PerformDiagnostic(null, null, default).WithCancellation(token)) { }
                    });
                    return task + "," + stream;
            """, async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                return await Json("null");
            });

        Assert.AreEqual("canceled,canceled", proxy.Result);
    }

    [TestMethod]
    public async Task Route_constraints_defaults_optional_and_catch_all_parameters_are_supported()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public interface IConstraintController : IAppController
            {
                [HttpGet("{id:int:min(1)}")]
                Task ById(int id, CancellationToken cancellationToken);

                [HttpGet("{id:guid}/{version:long?}")]
                Task ByVersion(Guid id, long? version, CancellationToken cancellationToken);

                [HttpGet("{name=home}")]
                Task ByName(string name, CancellationToken cancellationToken);

                [HttpGet("{**path}")]
                Task ByPath(string path, CancellationToken cancellationToken);

                [HttpGet("{code:regex(^\\d{{3}}$)}")]
                Task ByCode(string code, CancellationToken cancellationToken);
            }
            """, """
                    var controller = sp.GetRequiredService<IConstraintController>();
                    await controller.ById(5, default);
                    await controller.ByVersion(Guid.Empty, 7, default);
                    await controller.ByName("a b", default);
                    await controller.ByPath("docs/read me", default);
                    await controller.ByCode("123", default);
                    return null;
            """);

        Assert.AreEqual(Url("api/Constraint/ById/5"), proxy.Requests[0].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/Constraint/ByVersion/00000000-0000-0000-0000-000000000000/7"), proxy.Requests[1].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/Constraint/ByName/a%20b"), proxy.Requests[2].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/Constraint/ByPath/docs/read%20me"), proxy.Requests[3].Uri!.AbsoluteUri);
        Assert.AreEqual(Url("api/Constraint/ByCode/123"), proxy.Requests[4].Uri!.AbsoluteUri);
        Assert.IsTrue(proxy.Requests.All(request => request.Content is null));
    }

    [TestMethod]
    public async Task Route_parameter_names_match_the_method_parameters_ignoring_case()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public interface ICaseController : IAppController
            {
                [HttpGet("{Id}")]
                Task Get(int id, CancellationToken cancellationToken);

                [HttpGet("{?Filter}")]
                Task Find(string filter, CancellationToken cancellationToken);
            }
            """, """
                    var controller = sp.GetRequiredService<ICaseController>();
                    await controller.Get(5, default);
                    await controller.Find("x", default);
                    return null;
            """);

        Assert.AreEqual(Url("api/Case/Get/5"), proxy.Requests[0].Uri!.AbsoluteUri);
        Assert.IsNull(proxy.Requests[0].Content);
        Assert.AreEqual(Url("api/Case/Find/?Filter=x"), proxy.Requests[1].Uri!.AbsoluteUri);
        Assert.IsNull(proxy.Requests[1].Content);
    }

    [TestMethod]
    public async Task Every_http_verb_attribute_is_recognized_by_its_exact_name()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [AttributeUsage(AttributeTargets.Method)]
            public sealed class HttpCacheableAttribute : Attribute { }

            public class PatchDto
            {
                public string? Name { get; set; }
            }

            public interface IVerbController : IAppController
            {
                [HttpHead("api/verbs/head")]
                Task Head(CancellationToken cancellationToken);

                [HttpOptions("api/verbs/options")]
                Task Options(CancellationToken cancellationToken);

                [HttpCacheable, HttpPost("api/verbs/post/{id}")]
                Task Post(int id, CancellationToken cancellationToken);

                [HttpPatch("api/verbs/patch")]
                Task Patch(PatchDto dto, CancellationToken cancellationToken);
            }
            """, """
                    var controller = sp.GetRequiredService<IVerbController>();
                    await controller.Head(default);
                    await controller.Options(default);
                    await controller.Post(5, default);
                    await controller.Patch(new PatchDto { Name = "n" }, default);
                    return null;
            """);

        Assert.AreEqual("HEAD, OPTIONS, POST, PATCH", string.Join(", ", proxy.Requests.Select(r => r.Method)));
        Assert.AreEqual(Url("api/verbs/post/5"), proxy.Requests[2].Uri!.AbsoluteUri);
        Assert.IsNull(proxy.Requests[2].Content);
        StringAssert.Contains(proxy.Requests[3].Content, "\"name\":\"n\"");
    }

    [TestMethod]
    public async Task Action_token_drops_the_async_suffix_like_aspnetcore_does()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]")]
            public interface IOrderController : IAppController
            {
                [HttpGet]
                Task<int> GetCountAsync(CancellationToken cancellationToken);
            }
            """, """
                    return await sp.GetRequiredService<IOrderController>().GetCountAsync(default);
            """, (_, _) => Json("4"));

        Assert.AreEqual(4, proxy.Result);
        Assert.AreEqual(Url("api/Order/GetCount"), proxy.Request.Uri!.AbsoluteUri);
        Assert.AreEqual("GetCountAsync", proxy.Request.Options["ActionName"]);
    }

    [TestMethod]
    public async Task Controller_interfaces_can_inherit_from_other_controller_interfaces()
    {
        var proxy = await RunAsync("""
            global using TestApp.Crud;

            namespace TestApp.Crud
            {
                public class CustomerDto
                {
                    public string? Name { get; set; }
                }

                public interface ICrudController<TDto> : IAppController
                {
                    [HttpGet("{id}")]
                    Task<TDto> Get(int id, CancellationToken cancellationToken);

                    [HttpPost]
                    Task<TDto> Create(TDto dto, CancellationToken cancellationToken);
                }

                [Route("api/[controller]/[action]/")]
                public interface ICustomerController : ICrudController<CustomerDto>
                {
                    [HttpGet]
                    Task<int> Count(CancellationToken cancellationToken);
                }

                [Route("api/[controller]/[action]/")]
                public interface IHealthController : Boilerplate.Shared.Infrastructure.Services.Contracts.IAppController
                {
                    [HttpGet]
                    Task<string> Ping(CancellationToken cancellationToken);
                }
            }
            """, """
                    var customers = sp.GetRequiredService<ICustomerController>();
                    var customer = await customers.Get(3, default);
                    await customers.Create(new CustomerDto { Name = "n" }, default);
                    await customers.Count(default);
                    await sp.GetRequiredService<IHealthController>().Ping(default);
                    return customer.Name;
            """, (request, _) => Json(request.Uri!.AbsolutePath.EndsWith("Count") ? "1" : """{"name":"c"}"""));

        Assert.AreEqual("c", proxy.Result);
        Assert.AreEqual(
            string.Join(" | ", Url("api/Customer/Get/3"), Url("api/Customer/Create"), Url("api/Customer/Count"), Url("api/Health/Ping")),
            string.Join(" | ", proxy.Requests.Select(r => r.Uri!.AbsoluteUri)));
        Assert.AreEqual("ICustomerController", ((Type)proxy.Requests[1].Options["IControllerType"]!).Name);
    }

    [TestMethod]
    public async Task Members_that_are_not_http_actions_keep_their_default_implementation()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public interface IReportController : IAppController
            {
                [HttpGet]
                Task<int> Count(CancellationToken cancellationToken);

                string Describe() => "report" + Secret();

                static int Parse(string value) => int.Parse(value);

                private string Secret() => "!";
            }
            """, """
                    var controller = sp.GetRequiredService<IReportController>();
                    return controller.Describe() + IReportController.Parse("2");
            """);

        Assert.AreEqual("report!2", proxy.Result);
        Assert.IsEmpty(proxy.Requests);
    }

    [TestMethod]
    public async Task Keyword_parameter_names_are_escaped()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            public class KeywordDto
            {
                public string? Name { get; set; }
            }

            [Route("api/[controller]/[action]/")]
            public interface IKeywordController : IAppController
            {
                [HttpGet("{class}/{?event}")]
                Task Find(string @class, int @event, CancellationToken cancellationToken);

                [HttpPost]
                Task Save(KeywordDto @object, CancellationToken cancellationToken);
            }
            """, """
                    var controller = sp.GetRequiredService<IKeywordController>();
                    await controller.Find("a", 2, default);
                    await controller.Save(new KeywordDto { Name = "k" }, default);
                    return null;
            """);

        Assert.AreEqual(Url("api/Keyword/Find/a/?event=2"), proxy.Requests[0].Uri!.AbsoluteUri);
        StringAssert.Contains(proxy.Requests[1].Content, "\"name\":\"k\"");
    }

    [TestMethod]
    public void Generated_code_does_not_bind_to_names_of_the_consumer()
    {
        Generate("""
            global using Logging.Contracts;

            namespace Logging.Contracts
            {
                public class Type { }
                public class Uri { }
                public class HttpMethod { }
            }

            namespace Hosting.Shared.Dtos
            {
                public class StatusDto
                {
                    public string? State { get; set; }
                }
            }

            namespace Hosting.Shared.Controllers
            {
                [Route("api/[controller]/[action]/")]
                public interface IStatusController : IAppController
                {
                    [HttpGet("{id}")]
                    Task<Hosting.Shared.Dtos.StatusDto> Get(int id, CancellationToken cancellationToken);

                    [HttpPost]
                    Task Save(Hosting.Shared.Dtos.StatusDto dto, CancellationToken cancellationToken);
                }
            }
            """).AssertSucceeded();
    }

    [TestMethod]
    public void Invalid_route_template_is_reported_without_failing_the_other_controllers()
    {
        var run = Generate("""
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public interface IBrokenController : IAppController
            {
                [HttpGet("{id")]
                Task<int> Get(int id, CancellationToken cancellationToken);
            }

            [Route("api/[controller]/[action]/")]
            public interface IFineController : IAppController
            {
                [HttpGet]
                Task<int> Get(CancellationToken cancellationToken);
            }
            """);

        Assert.IsNull(run.Result.Exception, run.Result.Exception?.ToString());
        var diagnostic = run.GeneratorDiagnostics.Single();
        Assert.AreEqual("BITGEN003", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("\"{id\"", run.TextAt(diagnostic));
        Assert.IsEmpty(run.CompilationDiagnostics, run.Describe("The compilation has errors or warnings", run.CompilationDiagnostics));
        StringAssert.Contains(run.Source("HttpClientProxy"), "IFineController");
    }

    [TestMethod]
    public void Incomplete_interface_while_typing_does_not_fail_the_generator()
    {
        var run = Generate("""
            namespace TestApp;

            public interface : IAppController
            {
                [HttpGet]
                Task<int> Get(CancellationToken cancellationToken);
            }

            [Route("api/[controller]/[action]/")]
            public interface IFineController : IAppController
            {
                [HttpGet]
                Task<int> Get(CancellationToken cancellationToken);
            }
            """);

        Assert.IsNull(run.Result.Exception, run.Result.Exception?.ToString());
        StringAssert.Contains(run.Source("HttpClientProxy"), "IFineController");
    }

    [TestMethod]
    public void Proxy_name_option_names_the_registration_method()
    {
        const string controller = """
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public interface IFineController : IAppController
            {
                [HttpGet]
                Task<int> Get(CancellationToken cancellationToken);
            }
            """;

        var named = Generate(controller, new Dictionary<string, string> { ["build_property.BitHttpClientProxyName"] = " Access " }).AssertSucceeded();
        var invalid = Generate(controller, new Dictionary<string, string> { ["build_property.BitHttpClientProxyName"] = "1st" });

        StringAssert.Contains(named.Source("HttpClientProxy"), "public static class AccessHttpClientServiceCollectionExtensions");
        StringAssert.Contains(named.Source("HttpClientProxy"), "public static void AddAccessTypedHttpClients(");
        Assert.AreEqual("BITGEN002", invalid.GeneratorDiagnostics.Single().Id);
        Assert.IsEmpty(invalid.Result.GeneratedSources);
    }

    [TestMethod]
    public void Partial_controller_interface_gets_one_proxy()
    {
        Generate("""
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public partial interface ISplitController : IAppController
            {
                [HttpGet]
                Task<int> First(CancellationToken cancellationToken);
            }

            public partial interface ISplitController : IAppController
            {
                [HttpGet]
                Task<int> Second(CancellationToken cancellationToken);
            }
            """).AssertSucceeded();
    }

    [TestMethod]
    public async Task Controller_route_is_relative_to_the_base_address()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("/api/[controller]/[action]/")]
            public interface ISlashController : IAppController
            {
                [HttpGet]
                Task Get(CancellationToken cancellationToken);
            }

            [Route("~/api/[controller]/[action]/")]
            public interface IRootController : IAppController
            {
                [HttpGet]
                Task Get(CancellationToken cancellationToken);
            }

            [Route("api/[controller]/[action]/")]
            public interface OrdersController : IAppController
            {
                [HttpGet]
                Task Get(CancellationToken cancellationToken);
            }
            """, """
                    await sp.GetRequiredService<ISlashController>().Get(default);
                    await sp.GetRequiredService<IRootController>().Get(default);
                    await sp.GetRequiredService<OrdersController>().Get(default);
                    return null;
            """);

        Assert.AreEqual(
            string.Join(" | ", Url("api/Slash/Get"), Url("api/Root/Get"), Url("api/Orders/Get")),
            string.Join(" | ", proxy.Requests.Select(r => r.Uri!.AbsoluteUri)));
    }

    [TestMethod]
    public async Task Action_route_is_relative_to_the_base_address()
    {
        var proxy = await RunAsync("""
            namespace TestApp;

            [Route("api/[controller]/[action]/")]
            public interface IRoutedController : IAppController
            {
                [HttpGet, Route("/api/v1/[controller]/rooted")]
                Task Rooted(CancellationToken cancellationToken);

                [HttpGet, Route("~/api/v1/[controller]/app-root")]
                Task AppRoot(CancellationToken cancellationToken);

                [HttpGet, Route("https://api.example.test/absolute")]
                Task Absolute(CancellationToken cancellationToken);

                [HttpGet]
                Task Conventional(CancellationToken cancellationToken);
            }
            """, """
                    var controller = sp.GetRequiredService<IRoutedController>();
                    await controller.Rooted(default);
                    await controller.AppRoot(default);
                    await controller.Absolute(default);
                    await controller.Conventional(default);
                    return null;
            """);

        Assert.AreEqual(
            string.Join(" | ", Url("api/v1/Routed/rooted"), Url("api/v1/Routed/app-root"), "https://api.example.test/absolute", Url("api/Routed/Conventional")),
            string.Join(" | ", proxy.Requests.Select(r => r.Uri!.AbsoluteUri)));
    }

    [TestMethod]
    public async Task Controller_in_the_global_namespace_gets_a_proxy()
    {
        var proxy = await RunAsync("""
            [Route("api/[controller]/[action]/")]
            public interface IGlobalController : IAppController
            {
                [HttpGet("{id}")]
                Task<int> Get(int id, CancellationToken cancellationToken);
            }
            """, """
                    return await sp.GetRequiredService<IGlobalController>().Get(1, default);
            """, (_, _) => Json("5"));

        Assert.AreEqual(5, proxy.Result);
        Assert.AreEqual(Url("api/Global/Get/1"), proxy.Request.Uri!.AbsoluteUri);
    }

    [TestMethod]
    public void Unrelated_edit_keeps_the_generated_output_cached()
    {
        var first = Generate(BoilerplateControllers).AssertSucceeded();

        var second = first.RunAgain(first.OutputCompilation.RemoveSyntaxTrees(first.Result.GeneratedSources.Select(s => s.SyntaxTree))
            .AddSyntaxTrees(GeneratorTestHarness.ParseSource("namespace TestApp { public class Unrelated { } }", "Unrelated.cs")));

        var reasons = second.Result.TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();
        Assert.IsNotEmpty(reasons);
        Assert.IsTrue(reasons.All(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged), string.Join(", ", reasons));
    }
}
