using System.Net;
using System.Text;

namespace Bit.SourceGenerators.Tests;

internal sealed record CapturedRequest(HttpMethod Method, Uri? Uri, string? Content, string? ContentType, IReadOnlyDictionary<string, object?> Options);

internal sealed record ProxyRun(GeneratorRun Run, IReadOnlyList<CapturedRequest> Requests, object? Result)
{
    public CapturedRequest Request => Requests.Single();
}

internal static class ProxyTestHost
{
    public const string BaseAddress = "https://api.test/app/";

    public const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Text.Json;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Microsoft.Extensions.DependencyInjection;
        global using Boilerplate.Shared.Infrastructure.Attributes;
        global using Boilerplate.Shared.Infrastructure.Services.Contracts;
        """;

    public const string Contracts = """
        using System.Runtime.CompilerServices;
        using System.Text.Json.Serialization.Metadata;

        namespace Boilerplate.Shared.Infrastructure.Services.Contracts
        {
            public interface IAppController
            {
                void AddQueryString(string key, object? value) { }
                void AddQueryStrings(Dictionary<string, object?> queryString) { }
            }

            public static class IAppControllerExtensions
            {
                public static TAppController WithQuery<TAppController>(this TAppController controller, string? existingQueryString)
                    where TAppController : IAppController
                {
                    return controller.WithQuery(queryString: AppQueryStringCollection.Parse(existingQueryString));
                }

                public static TAppController WithQuery<TAppController>(this TAppController controller, string key, object? value)
                    where TAppController : IAppController
                {
                    var copy = Copy(controller);
                    copy.AddQueryString(key, value);
                    return copy;
                }

                public static TAppController WithQuery<TAppController>(this TAppController controller, Dictionary<string, object?> queryString)
                    where TAppController : IAppController
                {
                    var copy = Copy(controller);
                    copy.AddQueryStrings(queryString);
                    return copy;
                }

                private static TAppController Copy<TAppController>(TAppController controller)
                    where TAppController : IAppController
                {
                    return controller is ICloneable cloneable ? (TAppController)cloneable.Clone() : controller;
                }
            }

            public interface IPrerenderStateService : IAsyncDisposable
            {
                Task<T?> GetValue<T>(Func<Task<T?>> factory,
                    [CallerLineNumber] int lineNumber = 0,
                    [CallerMemberName] string memberName = "",
                    [CallerFilePath] string filePath = "");

                Task<T?> GetValue<T>(string key, Func<Task<T?>> factory);
            }

            public sealed class RecordingPrerenderStateService : IPrerenderStateService
            {
                public List<string> Keys { get; } = [];

                public Task<T?> GetValue<T>(Func<Task<T?>> factory, int lineNumber = 0, string memberName = "", string filePath = "")
                {
                    return factory();
                }

                public Task<T?> GetValue<T>(string key, Func<Task<T?>> factory)
                {
                    Keys.Add(key);
                    return factory();
                }

                public ValueTask DisposeAsync()
                {
                    return default;
                }
            }
        }

        namespace Boilerplate.Shared.Infrastructure.Attributes
        {
            [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
            internal partial class RouteAttribute(string template) : Attribute
            {
                public string Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method)]
            internal partial class HttpGetAttribute(string? template = null) : Attribute
            {
                public string? Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method)]
            internal partial class HttpPostAttribute(string? template = null) : Attribute
            {
                public string? Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method)]
            internal partial class HttpPutAttribute(string? template = null) : Attribute
            {
                public string? Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method)]
            internal partial class HttpDeleteAttribute(string? template = null) : Attribute
            {
                public string? Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method)]
            internal partial class HttpPatchAttribute(string? template = null) : Attribute
            {
                public string? Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method)]
            internal partial class HttpHeadAttribute(string? template = null) : Attribute
            {
                public string? Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method)]
            internal partial class HttpOptionsAttribute(string? template = null) : Attribute
            {
                public string? Template { get; } = template;
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = false)]
            public partial class NoRetryPolicyAttribute : Attribute
            {
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = false)]
            public partial class AuthorizedApiAttribute : Attribute
            {
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = false)]
            public partial class ExternalApiAttribute : Attribute
            {
            }
        }

        namespace System
        {
            public class AppQueryStringCollection() : Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                public override string? ToString()
                {
                    if (Count == 0)
                        return null;

                    return string.Join("&", this.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value?.ToString() ?? "")}"));
                }

                public static AppQueryStringCollection Parse(string? query)
                {
                    var qsCollection = new AppQueryStringCollection();

                    if (string.IsNullOrWhiteSpace(query))
                        return qsCollection;

                    query = query.TrimStart('?');

                    foreach (var pair in query.Split(['&'], StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = pair.Split(['='], 2);
                        qsCollection[Uri.UnescapeDataString(parts.ElementAt(0))] = Uri.UnescapeDataString(parts.ElementAtOrDefault(1) ?? string.Empty);
                    }

                    return qsCollection;
                }
            }
        }

        namespace System.Text.Json
        {
            public static class JsonSerializerOptionsExtensions
            {
                public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
                {
                    return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
                }
            }
        }
        """;

    public static GeneratorRun Generate(string controllers, IReadOnlyDictionary<string, string>? globalOptions = null)
    {
        return GeneratorTestHarness.Run(new HttpClientProxySourceGenerator(), globalOptions ?? new Dictionary<string, string>(), GlobalUsings, Contracts, controllers);
    }

    public static async Task<ProxyRun> RunAsync(string controllers, string scenario, Func<CapturedRequest, CancellationToken, Task<HttpResponseMessage>>? respond = null)
    {
        var run = GeneratorTestHarness.Run(new HttpClientProxySourceGenerator(), GlobalUsings, Contracts, controllers, ScenarioSource(scenario)).AssertSucceeded();
        var handler = new CapturingHandler(respond);
        var entryPoint = run.Load().GetType("TestApp.Scenario")!.GetMethod("RunAsync")!;
        var result = await (Task<object?>)entryPoint.Invoke(null, [handler])!;
        return new ProxyRun(run, handler.Requests, result);
    }

    public static Task<HttpResponseMessage> Json(string json)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }

    private static string ScenarioSource(string scenario)
    {
        return $$"""
            using System.Text.Json.Serialization.Metadata;

            namespace TestApp;

            public static class Scenario
            {
                public static async Task<object?> RunAsync(HttpMessageHandler handler)
                {
                    var services = new ServiceCollection();
                    services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("{{BaseAddress}}") });
                    services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() });
                    services.AddSingleton<RecordingPrerenderStateService>();
                    services.AddSingleton<IPrerenderStateService>(sp => sp.GetRequiredService<RecordingPrerenderStateService>());
                    services.AddTypedHttpClients();
                    await using var sp = services.BuildServiceProvider();
                    var prerenderKeys = sp.GetRequiredService<RecordingPrerenderStateService>().Keys;
            {{scenario}}
                }
            }
            """;
    }

    private sealed class CapturingHandler(Func<CapturedRequest, CancellationToken, Task<HttpResponseMessage>>? respond) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var captured = new CapturedRequest(request.Method, request.RequestUri, content, request.Content?.Headers.ContentType?.MediaType, new Dictionary<string, object?>(request.Options));
            Requests.Add(captured);
            return await (respond?.Invoke(captured, cancellationToken) ?? Json("null"));
        }
    }
}
