using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using DoLess.UriTemplates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bit.SourceGenerators;

[Generator]
public class HttpClientProxySourceGenerator : IIncrementalGenerator
{
    // ASCII control-character separators (never appear in C# identifiers or type display strings)
    private const char ActionSep = '\x1E';   // RS – between action records
    private const char FieldSep = '\x1F';    // US – between fields inside one action record
    private const char ParamSep = '\x1D';    // GS – between parameters inside one action record
    private const char SubFieldSep = '\x1C'; // FS – between sub-fields inside one parameter entry

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var controllerProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is InterfaceDeclarationSyntax iface &&
                    iface.BaseList is not null &&
                    iface.BaseList.Types.Any(t => t.Type.ToString() == "IAppController"),
                transform: static (ctx, ct) => TransformController(ctx, ct))
            .Where(static c => c is not null)
            .Select(static (c, _) => c!.Value);

        context.RegisterSourceOutput(controllerProvider.Collect(), static (spc, controllers) => Execute(spc, controllers));
    }

    // ── Transform ─────────────────────────────────────────────────────────────

    private static ControllerEntry? TransformController(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var interfaceDecl = (InterfaceDeclarationSyntax)ctx.Node;
        var model = ctx.SemanticModel;
        var controllerSymbol = model.GetDeclaredSymbol(interfaceDecl, ct) as ITypeSymbol;
        if (controllerSymbol is null) return null;
        if (!controllerSymbol.IsIController()) return null;

        var interfaceNameWithoutPrefix = controllerSymbol.Name[1..];
        var controllerName = interfaceNameWithoutPrefix.EndsWith("Controller", StringComparison.Ordinal)
            ? interfaceNameWithoutPrefix[..^"Controller".Length]
            : interfaceNameWithoutPrefix;

        var route = controllerSymbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("Route") is true)?
            .ConstructorArguments
            .FirstOrDefault()
            .Value?
            .ToString()
            ?.Replace("[controller]", controllerName) ?? string.Empty;

        var stringSpecialType = model.Compilation.GetSpecialType(SpecialType.System_String);
        var taskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var genericTaskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var genericValueTaskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        var asyncEnumerableType = model.Compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");

        var actionBuilders = new List<string>();

        foreach (var method in controllerSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary))
        {
            ct.ThrowIfCancellationRequested();

            var httpMethod = method.GetHttpMethod();

            // Build URL from route template: method Route replaces [controller], strips "~/" (ASP.NET root),
            // and uses (actionRoute ?? controllerRoute). HTTP verb templates are merged with a "/" unless absolute.
            var actionSpecificRoute = method
                .GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("Route") is true)?
                .ConstructorArguments
                .FirstOrDefault()
                .Value?
                .ToString()
                ?.Replace("[controller]", controllerName)
                ?.Replace("~/", string.Empty);

            var resolvedRoute = actionSpecificRoute ?? route;

            var httpVerbAttribute = method.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("Http") is true);
            var actionTemplate = httpVerbAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();

            string combinedTemplate;
            if (string.IsNullOrEmpty(actionTemplate))
            {
                combinedTemplate = resolvedRoute;
            }
            else
            {
                var t = actionTemplate!;
                if (t.StartsWith("/", StringComparison.Ordinal) || t.StartsWith("~/", StringComparison.Ordinal))
                {
                    combinedTemplate = t.StartsWith("~/", StringComparison.Ordinal)
                        ? t["~/".Length..]
                        : t.TrimStart('/');
                }
                else
                {
                    combinedTemplate = CombineRouteTemplates(resolvedRoute, t);
                }
            }

            var uriTemplate = UriTemplate.For(combinedTemplate.Replace("[action]", method.Name));

            var rawParameters = method.Parameters.Select(y => (y.Name, Type: y.Type)).ToList();
            foreach (var (pName, _) in rawParameters)
                uriTemplate.WithParameter(pName, $"{{{pName}}}");

            string url = HttpUtility.UrlDecode(uriTemplate.ExpandToString()).TrimEnd('/');

            var ctParam = rawParameters.FirstOrDefault(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
            var ctName = ctParam == default ? null : ctParam.Name;

            var bodyParam = rawParameters.FirstOrDefault(p =>
                p.Type.ToDisplayString() is not "System.Threading.CancellationToken" &&
                !url.Contains($"{{{p.Name}}}"));

            var returnType = method.ReturnType;
            var returnDisplay = returnType.ToDisplayString();
            var unwrappedReturnType = returnType;
            var isTaskLikeWithoutResult = false;

            if (returnType is INamedTypeSymbol namedReturnType)
            {
                if ((taskType is not null && SymbolEqualityComparer.Default.Equals(namedReturnType, taskType))
                    || (valueTaskType is not null && SymbolEqualityComparer.Default.Equals(namedReturnType, valueTaskType)))
                {
                    isTaskLikeWithoutResult = true;
                }
                else if (namedReturnType.IsGenericType
                         && ((genericTaskType is not null && SymbolEqualityComparer.Default.Equals(namedReturnType.OriginalDefinition, genericTaskType))
                             || (genericValueTaskType is not null && SymbolEqualityComparer.Default.Equals(namedReturnType.OriginalDefinition, genericValueTaskType))))
                {
                    unwrappedReturnType = namedReturnType.TypeArguments[0];
                }
            }

            bool doesReturnSomething = isTaskLikeWithoutResult is false;
            bool doesReturnIAsyncEnum = doesReturnSomething
                                        && unwrappedReturnType is INamedTypeSymbol namedUnwrappedReturnType
                                        && namedUnwrappedReturnType.IsGenericType
                                        && asyncEnumerableType is not null
                                        && SymbolEqualityComparer.Default.Equals(namedUnwrappedReturnType.OriginalDefinition, asyncEnumerableType);
            bool doesReturnString = doesReturnSomething
                                    && doesReturnIAsyncEnum is false
                                    && SymbolEqualityComparer.Default.Equals(unwrappedReturnType, stringSpecialType);
            var returnUnderlyingNoNull = returnType.GetUnderlyingType().ToDisplayString(NullableFlowState.None);

            // Encode parameters: "name\x1CfullType\x1CtypeNoNull\x1CisString" joined by \x1D
            var encodedParams = string.Join(
                ParamSep.ToString(),
                rawParameters.Select(p =>
                    $"{p.Name}{SubFieldSep}{p.Type.ToDisplayString()}{SubFieldSep}{p.Type.ToDisplayString(NullableFlowState.None)}{SubFieldSep}{(SymbolEqualityComparer.Default.Equals(p.Type, stringSpecialType) ? "1" : "0")}"));

            // Action fields joined by \x1F
            actionBuilders.Add(string.Join(
                FieldSep.ToString(),
                method.Name,
                returnDisplay,
                returnUnderlyingNoNull,
                doesReturnSomething ? "1" : "0",
                doesReturnString ? "1" : "0",
                doesReturnIAsyncEnum ? "1" : "0",
                httpMethod,
                url,
                ctName is not null ? "1" : "0",
                ctName ?? "",
                encodedParams,
                bodyParam == default ? "" : bodyParam.Name,
                bodyParam == default ? "" : bodyParam.Type.ToDisplayString(NullableFlowState.None)));
        }

        return new ControllerEntry(
            SymbolDisplay: controllerSymbol.ToDisplayString(),
            SymbolDisplayNoNull: controllerSymbol.ToDisplayString(NullableFlowState.None),
            ClassName: BuildProxyClassName(controllerSymbol),
            EncodedActions: string.Join(ActionSep.ToString(), actionBuilders));
    }

    // Generated proxy class name: full type display (namespace + nesting + interface), sanitized.
    private static string BuildProxyClassName(ITypeSymbol controllerSymbol)
    {
        var display = controllerSymbol.ToDisplayString(NullableFlowState.None);
        return SanitizeDisplayStringToTypeIdentifier(display);
    }

    private static string SanitizeDisplayStringToTypeIdentifier(string display)
    {
        if (string.IsNullOrEmpty(display))
            return "_HttpClientProxy";

        var sb = new StringBuilder(display.Length);
        var pendingUnderscore = false;
        foreach (var c in display)
        {
            var isIdChar = c == '_' || char.IsLetter(c) || char.IsDigit(c);
            if (isIdChar)
            {
                if (pendingUnderscore && sb.Length > 0)
                    sb.Append('_');
                pendingUnderscore = false;
                sb.Append(c);
            }
            else
            {
                if (sb.Length > 0)
                    pendingUnderscore = true;
            }
        }

        var result = sb.ToString().TrimEnd('_');
        if (result.Length == 0)
            return "_HttpClientProxy";
        if (char.IsDigit(result[0]))
            return "_" + result;
        return result;
    }

    private static string CombineRouteTemplates(string prefix, string suffix)
    {
        prefix = prefix.TrimEnd('/');
        suffix = suffix.TrimStart('/');
        if (string.IsNullOrEmpty(prefix)) return suffix;
        if (string.IsNullOrEmpty(suffix)) return prefix;
        return $"{prefix}/{suffix}";
    }

    // ── Code generation ───────────────────────────────────────────────────────

    private static void Execute(SourceProductionContext spc, ImmutableArray<ControllerEntry> controllers)
    {
        if (controllers.IsEmpty) return;

        StringBuilder generatedClasses = new();

        foreach (var controller in controllers)
        {
            StringBuilder generatedMethods = new();

            foreach (var actionEncoded in controller.EncodedActions.Split(ActionSep))
            {
                if (string.IsNullOrEmpty(actionEncoded)) continue;

                var fields = actionEncoded.Split(FieldSep);
                // fields[0]  methodName
                // fields[1]  returnTypeDisplay
                // fields[2]  returnTypeUnderlyingNoNull
                // fields[3]  doesReturnSomething
                // fields[4]  doesReturnString
                // fields[5]  doesReturnIAsyncEnumerable
                // fields[6]  httpMethod
                // fields[7]  url
                // fields[8]  hasCancellationToken
                // fields[9]  ctParamName
                // fields[10] encodedParams
                // fields[11] bodyParamName
                // fields[12] bodyParamTypeNoNull

                if (fields.Length < 13) continue;

                var methodName = fields[0];
                var returnTypeDisplay = fields[1];
                var returnUnderlyingNoNull = fields[2];
                var doesReturnSomething = fields[3] == "1";
                var doesReturnString = fields[4] == "1";
                var doesReturnIAsyncEnum = fields[5] == "1";
                var httpMethod = fields[6];
                var url = fields[7];
                var hasCt = fields[8] == "1";
                var ctName = fields[9];
                var bodyParamName = string.IsNullOrEmpty(fields[11]) ? null : fields[11];
                var bodyParamTypeNoNull = string.IsNullOrEmpty(fields[12]) ? null : fields[12];

                // Decode parameters
                var parameters = new List<(string Name, string TypeDisplay, string TypeDisplayNoNull, bool IsString)>();
                if (!string.IsNullOrEmpty(fields[10]))
                {
                    foreach (var pEnc in fields[10].Split(ParamSep))
                    {
                        var sf = pEnc.Split(SubFieldSep);
                        if (sf.Length < 4) continue;
                        parameters.Add((sf[0], sf[1], sf[2], sf[3] == "1"));
                    }
                }

                string parameterList = string.Join(", ", parameters.Select(p => $"{p.TypeDisplay} {p.Name}"));

                List<string> jsonReadParametersList = new();
                if (doesReturnSomething && !doesReturnString)
                    jsonReadParametersList.Add($"options.GetTypeInfo<{returnUnderlyingNoNull}>()");
                if (hasCt)
                    jsonReadParametersList.Add(ctName!);
                var jsonReadParameters = string.Join(", ", jsonReadParametersList);

                var requestOptions = new StringBuilder();
                requestOptions.AppendLine($"__request.Options.TryAdd(\"IControllerType\", typeof({controller.SymbolDisplayNoNull}));");
                requestOptions.AppendLine($"__request.Options.TryAdd(\"ActionName\", \"{methodName}\");");
                requestOptions.AppendLine($@"__request.Options.TryAdd(""ActionParametersInfo"", new Dictionary<string, Type>
                {{
                    {string.Join(", ", parameters.Select(p => $"{{ \"{p.Name}\", typeof({p.TypeDisplayNoNull})  }}"))}
                }});");
                if (bodyParamName is not null)
                    requestOptions.AppendLine($"__request.Options.TryAdd(\"RequestType\", typeof({bodyParamTypeNoNull}));");
                if (doesReturnSomething)
                    requestOptions.AppendLine($"__request.Options.TryAdd(\"ResponseType\", typeof({returnUnderlyingNoNull}));");

                var jsonStreamReturn = doesReturnIAsyncEnum
                    ? $"return WrapWithResponseDisposal(__response.Content.ReadFromJsonAsAsyncEnumerable({jsonReadParameters}), __response);"
                    : $"return await __response.Content.{(doesReturnString ? "ReadAsStringAsync" : "ReadFromJsonAsync")}({jsonReadParameters});";

                var encodeStringRouteParameters = string.Join(
                    Environment.NewLine,
                    parameters
                        .Where(p => p.IsString && url.Contains($"{{{p.Name}}}", StringComparison.Ordinal))
                        .Select(p => $"{p.Name} = Uri.EscapeDataString(Uri.UnescapeDataString({p.Name} ?? string.Empty));"));

                generatedMethods.AppendLine($@"
        public async {returnTypeDisplay} {methodName}({parameterList})
        {{
            {encodeStringRouteParameters}
            {$@"var __url = $""{url}"";"}
            var dynamicQS = GetDynamicQueryString();
            if (dynamicQS is not null)
            {{
                __url += {(url.Contains('?') ? "'&'" : "'?'")} + dynamicQS;
            }}
            {(doesReturnSomething ? $@"return (await prerenderStateService.GetValue(__url, async () =>
            {{" : string.Empty)}
                using var __request = new HttpRequestMessage(HttpMethod.{httpMethod}, __url);
                {requestOptions}
                {(bodyParamName is not null ? $@"__request.Content = JsonContent.Create({bodyParamName}, options.GetTypeInfo<{bodyParamTypeNoNull}>());" : string.Empty)}
                {(doesReturnIAsyncEnum ? "" : "using ")}var __response = await httpClient.SendAsync(__request, HttpCompletionOption.ResponseHeadersRead {(hasCt ? $", {ctName}" : string.Empty)});
                {(doesReturnSomething ? ($"{jsonStreamReturn}" +
          $"}}))!;") : string.Empty)}
        }}
");
            }

            generatedClasses.AppendLine($@"
    internal class {controller.ClassName}(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, {controller.SymbolDisplay}
    {{
        {generatedMethods}
    }}");
        }

        StringBuilder finalSource = new(@$"
using System.Web;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

[global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]
[global::System.Diagnostics.DebuggerNonUserCode]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class IHttpClientServiceCollectionExtensions
{{
    public static void AddTypedHttpClients(this IServiceCollection services)
    {{
{string.Join(Environment.NewLine, controllers.Select(i => $"        services.TryAddTransient<{i.SymbolDisplay}, {i.ClassName}>();"))}
    }}

internal class AppControllerBase
{{
    AppQueryStringCollection queryString = [];

    public void AddQueryString(string key, object? value)
    {{
        queryString.Add(key, value?.ToString());
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
        var result = queryString.ToString();

        queryString.Clear();

        return result;
    }}

    /// <summary>Disposes <paramref name=""response""/> after the JSON stream is fully consumed, faulted, canceled, or the enumerator is disposed.</summary>
    protected static async System.Collections.Generic.IAsyncEnumerable<T> WrapWithResponseDisposal<T>(
        System.Collections.Generic.IAsyncEnumerable<T> source,
        HttpResponseMessage response,
        [EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
    {{
        try
        {{
            await foreach (var item in source.WithCancellation(cancellationToken))
                yield return item;
        }}
        finally
        {{
            response.Dispose();
        }}
    }}
}}

{generatedClasses}

}}
");
        spc.AddSource("HttpClientProxy.cs", SourceText.From(finalSource.ToString(), Encoding.UTF8));
    }
}
