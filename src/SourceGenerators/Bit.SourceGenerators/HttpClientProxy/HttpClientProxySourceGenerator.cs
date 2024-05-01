using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Bit.SourceGenerators;

[Generator]
public class HttpClientProxySourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new HttpClientProxySyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not HttpClientProxySyntaxReceiver receiver || receiver.IControllers.Any() is false)
        {
            return;
        }

        StringBuilder generatedClasses = new();

        foreach (var iController in receiver.IControllers)
        {
            StringBuilder generatedMethods = new();

            foreach (var action in iController.Actions)
            {
                string parameters = string.Join(", ", action.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

                var hasQueryString = action.Url.Contains('?');

                generatedMethods.AppendLine($@"
        public async {action.ReturnType.ToDisplayString()} {action.Method.Name}({parameters})
        {{
            {$@"var url = $""{action.Url}"";"}
            var dynamicQS = GetDynamicQueryString();
            if (dynamicQS is not null)
            {{
                url += {(action.Url.Contains('?') ? "'&'" : "'?'")} + dynamicQS;
            }}
            {(action.DoesReturnSomething ? $@"return (await prerenderStateService.GetValue(url, async () =>
            {{" : string.Empty)}
                using var request = new HttpRequestMessage(HttpMethod.{action.HttpMethod}, url);
                {(action.BodyParameter is not null ? $@"request.Content = JsonContent.Create({action.BodyParameter.Name}, options.GetTypeInfo<{action.BodyParameter.Type.ToDisplayString()}>());" : string.Empty)}
                {(action.DoesReturnIAsyncEnumerable ? "" : "using ")}var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead {(action.HasCancellationToken ? $", {action.CancellationTokenParameterName}" : string.Empty)});
                {(action.DoesReturnSomething ? ($"return {(action.DoesReturnIAsyncEnumerable ? "" : "await")} response.Content.{(action.DoesReturnIAsyncEnumerable ? "ReadFromJsonAsAsyncEnumerable" : "ReadFromJsonAsync")}(options.GetTypeInfo<{action.ReturnType.GetUnderlyingType().ToDisplayString()}>(){(action.HasCancellationToken ? $", {action.CancellationTokenParameterName}" : string.Empty)});" +
          $"}}))!;") : string.Empty)}
        }}
");
            }

            generatedClasses.AppendLine($@"
    internal class {iController.ClassName}(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, {iController.Symbol.ToDisplayString()}
    {{
        {generatedMethods}
    }}");
        }

        StringBuilder finalSource = new(@$"
using System.Text.Json;
using System.Net.Http.Json;
using System.Web;

namespace Microsoft.Extensions.DependencyInjection;

[global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]
[global::System.Diagnostics.DebuggerNonUserCode]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class IHttpClientServiceCollectionExtensions
{{
    public static void AddTypedHttpClients(this IServiceCollection services)
    {{
{string.Join(Environment.NewLine, receiver.IControllers.Select(i => $"        services.TryAddTransient<{i.Symbol.ToDisplayString()}, {i.ClassName}>();"))}
    }}

    internal class AppControllerBase
    {{
        Dictionary<string, object?> queryString = [];

        public void AddQueryString(string key, object? value)
        {{
            queryString.Add(key, value);    
        }}

        public void AddQueryStrings(Dictionary<string, object?> queryString)
        {{
            foreach (var key in queryString.Keys)
            {{
                AddQueryString(key, queryString[key]);
            }}
        }}

        protected string? GetDynamicQueryString()
        {{
            if (queryString is not {{ Count: > 0 }})
                return null;

            var collection = HttpUtility.ParseQueryString(string.Empty);

            foreach (var key in queryString.Keys)
            {{
                collection.Add(key, queryString[key]?.ToString());
            }}

            queryString.Clear();

            return collection.ToString();
        }}
    }}

{generatedClasses}

}}
");
        context.AddSource($"HttpClientProxy.cs", finalSource.ToString());
    }
}
