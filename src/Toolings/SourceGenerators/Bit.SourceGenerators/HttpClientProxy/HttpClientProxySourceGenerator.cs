using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using DoLess.UriTemplates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    private static readonly DiagnosticDescriptor InvalidProxyNameError = new(
        id: "BITGEN002",
        title: "BitHttpClientProxyName is not a valid C# identifier",
        messageFormat: "BitHttpClientProxyName '{0}' is not a valid C# identifier. Set it to a name such as Access, which generates AddAccessTypedHttpClients, or leave it empty.",
        category: "Bit.SourceGenerators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidRouteTemplateError = new(
        id: "BITGEN003",
        title: "The route template is not valid",
        messageFormat: "The route of {0} is not a valid route template: {1}",
        category: "Bit.SourceGenerators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly Dictionary<string, string> HttpMethods = new(StringComparer.Ordinal)
    {
        ["HttpGetAttribute"] = "Get",
        ["HttpPostAttribute"] = "Post",
        ["HttpPutAttribute"] = "Put",
        ["HttpDeleteAttribute"] = "Delete",
        ["HttpPatchAttribute"] = "Patch",
        ["HttpHeadAttribute"] = "Head",
        ["HttpOptionsAttribute"] = "Options"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var controllerProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is InterfaceDeclarationSyntax { BaseList: not null },
                transform: static (ctx, ct) => TransformController(ctx, ct))
            .Where(static c => c is not null)
            .Select(static (c, _) => c!.Value);

        var proxyNameProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => options.GlobalOptions.TryGetValue("build_property.BitHttpClientProxyName", out var name) ? name.Trim() : string.Empty);

        context.RegisterSourceOutput(controllerProvider.Collect().Combine(proxyNameProvider), static (spc, input) => Execute(spc, input.Left, input.Right));
    }

    // ── Transform ─────────────────────────────────────────────────────────────

    private static ControllerEntry? TransformController(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var interfaceDecl = (InterfaceDeclarationSyntax)ctx.Node;
        var model = ctx.SemanticModel;
        var controllerSymbol = model.GetDeclaredSymbol(interfaceDecl, ct);
        if (controllerSymbol is null) return null;
        if (!IsIController(controllerSymbol)) return null;
        if (controllerSymbol.IsGenericType || BitSourceGeneratorUtil.HasNamedContainingTypes(controllerSymbol) is false) return null;

        var interfaceNameWithoutPrefix = controllerSymbol.Name.Length > 1 && controllerSymbol.Name[0] == 'I' && char.IsUpper(controllerSymbol.Name[1])
            ? controllerSymbol.Name[1..]
            : controllerSymbol.Name;
        var controllerName = interfaceNameWithoutPrefix.EndsWith("Controller", StringComparison.Ordinal)
            ? interfaceNameWithoutPrefix[..^"Controller".Length]
            : interfaceNameWithoutPrefix;

        var controllerRouteAttribute = FindRouteAttribute(controllerSymbol);
        var route = TrimAppRoot(GetTemplate(controllerRouteAttribute)
            ?.Replace("[controller]", controllerName) ?? string.Empty);

        var stringSpecialType = model.Compilation.GetSpecialType(SpecialType.System_String);
        var taskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var genericTaskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var genericValueTaskType = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        var asyncEnumerableType = model.Compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");

        var actionBuilders = new List<string>();
        var errors = new List<string>();

        bool IsProxyable(IMethodSymbol method)
        {
            if (method.MethodKind != MethodKind.Ordinary || method.IsStatic || (method.IsAbstract || method.IsVirtual) is false ||
                string.IsNullOrEmpty(method.Name) || method.ReturnType is not INamedTypeSymbol returnType)
                return false;

            return SymbolEqualityComparer.Default.Equals(returnType, taskType) ||
                   SymbolEqualityComparer.Default.Equals(returnType, valueTaskType) ||
                   SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, genericTaskType) ||
                   SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, genericValueTaskType) ||
                   SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, asyncEnumerableType);
        }

        var methods = controllerSymbol.GetMembers()
            .Concat(controllerSymbol.AllInterfaces.Where(IsIController).SelectMany(baseController => baseController.GetMembers()))
            .OfType<IMethodSymbol>()
            .Where(IsProxyable);

        foreach (var method in methods)
        {
            ct.ThrowIfCancellationRequested();

            var httpVerbAttribute = method.GetAttributes().FirstOrDefault(a => a.AttributeClass is not null && HttpMethods.ContainsKey(a.AttributeClass.Name));
            var httpMethod = httpVerbAttribute is null ? "Get" : HttpMethods[httpVerbAttribute.AttributeClass!.Name];

            // Build URL from route template: method Route replaces [controller], strips "~/" (ASP.NET root),
            // and uses (actionRoute ?? controllerRoute). HTTP verb templates are merged with a "/" unless absolute.
            var actionRouteAttribute = FindRouteAttribute(method);
            var actionSpecificRoute = GetTemplate(actionRouteAttribute)
                ?.Replace("[controller]", controllerName)
                ?.Replace("~/", string.Empty);

            var resolvedRoute = actionSpecificRoute ?? route;

            var actionTemplate = GetTemplate(httpVerbAttribute);

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

            var actionName = method.Name.Length > "Async".Length && method.Name.EndsWith("Async", StringComparison.Ordinal)
                ? method.Name[..^"Async".Length]
                : method.Name;

            string url;
            try
            {
                url = ExpandRouteTemplate(combinedTemplate.Replace("[action]", actionName), method.Parameters);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var templateAttribute = string.IsNullOrEmpty(actionTemplate) ? actionRouteAttribute ?? controllerRouteAttribute : httpVerbAttribute;
                errors.Add(EncodeError($"{controllerSymbol.Name}.{method.Name}", exception.Message, GetTemplateLocation(templateAttribute, method, ct)));
                continue;
            }

            var rawParameters = method.Parameters.Select(y => (y.Name, Type: y.Type)).ToList();

            var ctParam = rawParameters.FirstOrDefault(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
            var ctName = ctParam == default ? null : ctParam.Name;

            var bodyParam = rawParameters.FirstOrDefault(p =>
                p.Type.ToDisplayString() is not "System.Threading.CancellationToken" &&
                IsRouteValue(url, p.Name) is false);

            var returnType = method.ReturnType;
            var returnDisplay = returnType.ToDisplayString(BitSourceGeneratorUtil.FullyQualifiedFormat);
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
            // Only a method whose own return type is the stream can be an iterator. Task<IAsyncEnumerable<T>> unwraps
            // to one but still has to hand its value back.
            bool returnsAsyncStream = doesReturnIAsyncEnum
                                      && SymbolEqualityComparer.Default.Equals(returnType, unwrappedReturnType);
            bool doesReturnString = doesReturnSomething
                                    && doesReturnIAsyncEnum is false
                                    && SymbolEqualityComparer.Default.Equals(unwrappedReturnType, stringSpecialType);
            var responseType = doesReturnIAsyncEnum ? ((INamedTypeSymbol)unwrappedReturnType).TypeArguments[0] : unwrappedReturnType;
            var returnUnderlyingNoNull = responseType.ToDisplayString(NullableFlowState.None, BitSourceGeneratorUtil.FullyQualifiedFormat);

            // Encode parameters: "name\x1CfullType\x1CtypeNoNull\x1CisString" joined by \x1D
            var encodedParams = string.Join(
                ParamSep.ToString(),
                rawParameters.Select(p =>
                    $"{p.Name}{SubFieldSep}{p.Type.ToDisplayString(BitSourceGeneratorUtil.FullyQualifiedFormat)}{SubFieldSep}{p.Type.ToDisplayString(NullableFlowState.None, BitSourceGeneratorUtil.FullyQualifiedFormat)}{SubFieldSep}{(SymbolEqualityComparer.Default.Equals(p.Type, stringSpecialType) ? "1" : "0")}"));

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
                bodyParam == default ? "" : bodyParam.Type.ToDisplayString(NullableFlowState.None, BitSourceGeneratorUtil.FullyQualifiedFormat),
                returnsAsyncStream ? "1" : "0"));
        }

        return new ControllerEntry(
            SymbolDisplay: controllerSymbol.ToDisplayString(BitSourceGeneratorUtil.FullyQualifiedFormat),
            SymbolDisplayNoNull: controllerSymbol.ToDisplayString(NullableFlowState.None, BitSourceGeneratorUtil.FullyQualifiedFormat),
            ClassName: BuildProxyClassName(controllerSymbol),
            EncodedErrors: string.Join(ActionSep.ToString(), errors),
            EncodedActions: string.Join(ActionSep.ToString(), actionBuilders));
    }

    private static bool IsIController(ITypeSymbol type)
    {
        return type.AllInterfaces.Any(x => x.Name == "IAppController");
    }

    private static AttributeData? FindRouteAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("Route") is true);
    }

    private static string? GetTemplate(AttributeData? attribute)
    {
        return attribute?.ConstructorArguments is [{ Kind: not TypedConstantKind.Array, Value: string template }, ..] ? template : null;
    }

    private static string TrimAppRoot(string template)
    {
        return template.StartsWith("~/", StringComparison.Ordinal) ? template["~/".Length..] : template.TrimStart('/');
    }

    private static bool IsRouteValue(string url, string parameterName)
    {
        return url.Contains($"{{{parameterName}}}") || url.Contains($"{{**{parameterName}}}");
    }

    private static string ExpandRouteTemplate(string template, ImmutableArray<IParameterSymbol> parameters)
    {
        var variables = new Dictionary<string, int>(StringComparer.Ordinal);
        var catchAllParameters = new HashSet<int>();
        var uriTemplate = UriTemplate.For(NormalizeRouteTemplate(template, parameters, variables, catchAllParameters));

        foreach (var variable in variables)
        {
            uriTemplate.WithParameter(variable.Key, GetPlaceholder(variable.Value));
        }

        var url = uriTemplate.ExpandToString();

        for (var i = 0; i < parameters.Length; i++)
        {
            var name = parameters[i].Name;
            url = url.Replace(GetPlaceholder(i), catchAllParameters.Contains(i) ? $"{{**{name}}}" : $"{{{name}}}");
        }

        return url.TrimEnd('/');
    }

    private static string GetPlaceholder(int index)
    {
        return $"__bitparam{index}__";
    }

    private static string NormalizeRouteTemplate(string template, ImmutableArray<IParameterSymbol> parameters, Dictionary<string, int> variables, HashSet<int> catchAllParameters)
    {
        var normalized = new StringBuilder(template.Length);

        for (var i = 0; i < template.Length; i++)
        {
            if (template[i] != '{')
            {
                normalized.Append(template[i]);
                continue;
            }

            var end = FindExpressionEnd(template, i);
            var expression = template.Substring(i + 1, end - i - 1);
            i = end;

            if (expression.Length > 0 && "+#./;?&".IndexOf(expression[0]) >= 0)
            {
                normalized.Append('{').Append(expression[0]).Append(MapVariables(expression.Substring(1), parameters, variables)).Append('}');
            }
            else if (expression.StartsWith("*", StringComparison.Ordinal) || expression.IndexOfAny([':', '=', '?']) >= 0)
            {
                var name = expression.TrimStart('*');
                var nameEnd = name.IndexOfAny([':', '=', '?']);
                name = nameEnd >= 0 ? name.Substring(0, nameEnd) : name;
                var parameterIndex = MapVariable(name, parameters, variables);

                if (parameterIndex >= 0 && expression.StartsWith("*", StringComparison.Ordinal))
                {
                    catchAllParameters.Add(parameterIndex);
                }

                normalized.Append('{').Append(name).Append('}');
            }
            else
            {
                normalized.Append('{').Append(MapVariables(expression, parameters, variables)).Append('}');
            }
        }

        return normalized.ToString();
    }

    private static int FindExpressionEnd(string template, int start)
    {
        for (var i = start + 1; i < template.Length; i++)
        {
            if (template[i] is '{' or '}' && i + 1 < template.Length && template[i + 1] == template[i])
            {
                i++;
                continue;
            }

            if (template[i] == '}')
                return i;
        }

        throw new FormatException($"The '{{' at position {start} of '{template}' is never closed.");
    }

    private static string MapVariables(string variableList, ImmutableArray<IParameterSymbol> parameters, Dictionary<string, int> variables)
    {
        return string.Join(",", variableList.Split(',').Select(variable =>
        {
            var explode = variable.EndsWith("*", StringComparison.Ordinal);
            var name = variable.TrimEnd('*');
            var prefixStart = name.IndexOf(':');
            name = prefixStart >= 0 ? name.Substring(0, prefixStart) : name;
            MapVariable(name, parameters, variables);
            return explode ? $"{name}*" : name;
        }));
    }

    private static int MapVariable(string name, ImmutableArray<IParameterSymbol> parameters, Dictionary<string, int> variables)
    {
        var parameterIndex = FindParameter(name, parameters, StringComparison.Ordinal);

        if (parameterIndex < 0)
        {
            parameterIndex = FindParameter(name, parameters, StringComparison.OrdinalIgnoreCase);
        }

        if (parameterIndex >= 0)
        {
            variables[name] = parameterIndex;
        }

        return parameterIndex;
    }

    private static int FindParameter(string name, ImmutableArray<IParameterSymbol> parameters, StringComparison comparison)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (string.Equals(parameters[i].Name, name, comparison))
                return i;
        }

        return -1;
    }

    private static Location GetTemplateLocation(AttributeData? attribute, IMethodSymbol method, CancellationToken ct)
    {
        if (attribute?.ApplicationSyntaxReference?.GetSyntax(ct) is AttributeSyntax attributeSyntax)
        {
            return attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression.GetLocation() ?? attributeSyntax.GetLocation();
        }

        return method.Locations.FirstOrDefault() ?? Location.None;
    }

    private static string EncodeError(string target, string message, Location location)
    {
        var lineSpan = location.GetLineSpan();

        return string.Join(
            FieldSep.ToString(),
            target,
            message,
            location.SourceTree?.FilePath ?? string.Empty,
            location.SourceSpan.Start,
            location.SourceSpan.Length,
            lineSpan.StartLinePosition.Line,
            lineSpan.StartLinePosition.Character,
            lineSpan.EndLinePosition.Line,
            lineSpan.EndLinePosition.Character);
    }

    private static Diagnostic DecodeError(string encodedError)
    {
        var fields = encodedError.Split(FieldSep);
        var location = fields[2].Length == 0
            ? Location.None
            : Location.Create(
                fields[2],
                new TextSpan(int.Parse(fields[3]), int.Parse(fields[4])),
                new LinePositionSpan(new LinePosition(int.Parse(fields[5]), int.Parse(fields[6])), new LinePosition(int.Parse(fields[7]), int.Parse(fields[8]))));

        return Diagnostic.Create(InvalidRouteTemplateError, location, fields[0], fields[1]);
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

    private static void Execute(SourceProductionContext spc, ImmutableArray<ControllerEntry> controllers, string proxyName)
    {
        if (controllers.IsEmpty) return;

        if (proxyName.Length > 0 && SyntaxFacts.IsValidIdentifier(proxyName) is false)
        {
            spc.ReportDiagnostic(Diagnostic.Create(InvalidProxyNameError, Location.None, proxyName));
            return;
        }

        var validControllers = new List<ControllerEntry>();

        foreach (var controller in controllers.GroupBy(c => c.SymbolDisplayNoNull).Select(g => g.First()))
        {
            if (controller.EncodedErrors.Length == 0)
            {
                validControllers.Add(controller);
                continue;
            }

            foreach (var encodedError in controller.EncodedErrors.Split(ActionSep))
            {
                spc.ReportDiagnostic(DecodeError(encodedError));
            }
        }

        StringBuilder generatedClasses = new();

        foreach (var controller in validControllers)
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
                // fields[13] returnsAsyncStream

                if (fields.Length < 14) continue;

                var methodName = fields[0];
                var returnTypeDisplay = fields[1];
                var returnUnderlyingNoNull = fields[2];
                var doesReturnSomething = fields[3] == "1";
                var doesReturnString = fields[4] == "1";
                var doesReturnIAsyncEnum = fields[5] == "1";
                var httpMethod = fields[6];
                var url = fields[7];
                var hasCt = fields[8] == "1";
                var ctName = BitSourceGeneratorUtil.EscapeIdentifier(fields[9]);
                var bodyParamName = string.IsNullOrEmpty(fields[11]) ? null : BitSourceGeneratorUtil.EscapeIdentifier(fields[11]);
                var bodyParamTypeNoNull = string.IsNullOrEmpty(fields[12]) ? null : fields[12];
                var returnsAsyncStream = fields[13] == "1";

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

                // An async iterator takes its token through the enumerator, not the call.
                string parameterList = string.Join(", ", parameters.Select(p =>
                    $"{(returnsAsyncStream && p.Name == fields[9] ? "[global::System.Runtime.CompilerServices.EnumeratorCancellation] " : string.Empty)}{p.TypeDisplay} {BitSourceGeneratorUtil.EscapeIdentifier(p.Name)}"));

                List<string> jsonReadParametersList = new();
                if (doesReturnSomething && !doesReturnString)
                    jsonReadParametersList.Add($"__options.GetTypeInfo<{returnUnderlyingNoNull}>()");
                if (hasCt)
                    jsonReadParametersList.Add(ctName);
                var jsonReadParameters = string.Join(", ", jsonReadParametersList);

                var requestOptions = new StringBuilder();
                requestOptions.AppendLine($"__request.Options.TryAdd(\"IControllerType\", typeof({controller.SymbolDisplayNoNull}));");
                requestOptions.AppendLine($"__request.Options.TryAdd(\"ActionName\", \"{methodName}\");");
                requestOptions.AppendLine($@"__request.Options.TryAdd(""ActionParametersInfo"", new global::System.Collections.Generic.Dictionary<string, global::System.Type>
                {{
                    {string.Join(", ", parameters.Select(p => $"{{ \"{p.Name}\", typeof({p.TypeDisplayNoNull})  }}"))}
                }});");
                if (bodyParamName is not null)
                    requestOptions.AppendLine($"__request.Options.TryAdd(\"RequestType\", typeof({bodyParamTypeNoNull}));");
                if (doesReturnSomething)
                    requestOptions.AppendLine($"__request.Options.TryAdd(\"ResponseType\", typeof({returnUnderlyingNoNull}));");

                var readBody = returnsAsyncStream
                    ? $@"await foreach (var __item in __response.Content.ReadFromJsonAsAsyncEnumerable({jsonReadParameters}))
                {{
                    yield return __item!;
                }}"
                    : doesReturnIAsyncEnum
                        ? $"return WrapWithResponseDisposal(__response.Content.ReadFromJsonAsAsyncEnumerable({jsonReadParameters}), __response);"
                        : $"return await __response.Content.{(doesReturnString ? "ReadAsStringAsync" : "ReadFromJsonAsync")}({jsonReadParameters});";

                // The prerender state stores one resolved value per url, which an iterator is not.
                var usesPrerenderState = doesReturnSomething && returnsAsyncStream is false;

                generatedMethods.AppendLine($@"
        public async {returnTypeDisplay} {BitSourceGeneratorUtil.EscapeIdentifier(methodName)}({parameterList})
        {{
            var __url = {BuildUrlExpression(url)};
            var __dynamicQueryString = GetDynamicQueryString();
            if (__dynamicQueryString is not null)
            {{
                __url += {(url.Contains('?') ? "'&'" : "'?'")} + __dynamicQueryString;
            }}
            {(usesPrerenderState ? $@"return (await __prerenderStateService.GetValue(__url, async () =>
            {{" : string.Empty)}
                using var __request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.{httpMethod}, __url);
                {requestOptions}
                {(bodyParamName is not null ? $@"__request.Content = global::System.Net.Http.Json.JsonContent.Create({bodyParamName}, __options.GetTypeInfo<{bodyParamTypeNoNull}>());" : string.Empty)}
                {(doesReturnIAsyncEnum && returnsAsyncStream is false ? "" : "using ")}var __response = await __httpClient.SendAsync(__request, global::System.Net.Http.HttpCompletionOption.ResponseHeadersRead {(hasCt ? $", {ctName}" : string.Empty)});
                {(doesReturnSomething ? ($"{readBody}" +
          $"{(usesPrerenderState ? "}))!;" : string.Empty)}") : string.Empty)}
        }}
");
            }

            generatedClasses.AppendLine($@"
    internal class {controller.ClassName}(global::System.Net.Http.HttpClient httpClient, global::System.Text.Json.JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase(httpClient, options, prerenderStateService), {controller.SymbolDisplay}
    {{
        {generatedMethods}
    }}");
        }

        StringBuilder finalSource = new(@$"#nullable enable

using System;
using System.Web;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

[global::System.CodeDom.Compiler.GeneratedCode(""Bit.SourceGenerators"",""{BitSourceGeneratorUtil.GetPackageVersion()}"")]
[global::System.Diagnostics.DebuggerNonUserCode]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class {(proxyName.Length > 0 ? proxyName : "I")}HttpClientServiceCollectionExtensions
{{
    public static void Add{proxyName}TypedHttpClients(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {{
{string.Join(Environment.NewLine, validControllers.Select(i => $"        services.TryAddTransient<{i.SymbolDisplay}, {i.ClassName}>();"))}
    }}

internal class AppControllerBase(global::System.Net.Http.HttpClient httpClient, global::System.Text.Json.JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : global::System.ICloneable
{{
    protected readonly global::System.Net.Http.HttpClient __httpClient = httpClient;
    protected readonly global::System.Text.Json.JsonSerializerOptions __options = options;
    protected readonly IPrerenderStateService __prerenderStateService = prerenderStateService;

    AppQueryStringCollection queryString = [];

    object global::System.ICloneable.Clone()
    {{
        var clone = (AppControllerBase)MemberwiseClone();
        clone.queryString = [];
        foreach (var item in queryString)
        {{
            clone.queryString.Add(item.Key, item.Value);
        }}
        return clone;
    }}

    public void AddQueryString(string key, object? value)
    {{
        queryString[key] = value is null ? null : FormatValue(value);
    }}

    public void AddQueryStrings(global::System.Collections.Generic.Dictionary<string, object?> queryString)
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

    protected static string EscapeRouteValue(string? value)
    {{
        return global::System.Uri.EscapeDataString(global::System.Uri.UnescapeDataString(value ?? string.Empty));
    }}

    protected static string EscapeRouteValue<T>(T value)
    {{
        return global::System.Uri.EscapeDataString(FormatValue(value));
    }}

    protected static string EscapeRouteSegments<T>(T value)
    {{
        var segments = FormatValue(value).Split('/');
        for (var i = 0; i < segments.Length; i++)
        {{
            segments[i] = EscapeRouteValue(segments[i]);
        }}
        return string.Join(""/"", segments);
    }}

    private static string FormatValue(object? value)
    {{
        return value is global::System.IFormattable formattable
            ? formattable.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture)
            : value?.ToString() ?? string.Empty;
    }}

    /// <summary>Disposes <paramref name=""response""/> after the JSON stream is fully consumed, faulted, canceled, or the enumerator is disposed.</summary>
    protected static async global::System.Collections.Generic.IAsyncEnumerable<T> WrapWithResponseDisposal<T>(
        global::System.Collections.Generic.IAsyncEnumerable<T?> source,
        global::System.Net.Http.HttpResponseMessage response,
        [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken = default)
    {{
        try
        {{
            await foreach (var item in source.WithCancellation(cancellationToken))
                yield return item!;
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

    private static string BuildUrlExpression(string url)
    {
        var expression = new StringBuilder("$\"");

        for (var i = 0; i < url.Length; i++)
        {
            var c = url[i];

            if (c == '{')
            {
                var end = url.IndexOf('}', i);
                var hole = url.Substring(i + 1, end - i - 1);
                var isCatchAll = hole.StartsWith("**", StringComparison.Ordinal);
                expression.Append('{')
                    .Append(isCatchAll ? "EscapeRouteSegments" : "EscapeRouteValue")
                    .Append('(')
                    .Append(BitSourceGeneratorUtil.EscapeIdentifier(isCatchAll ? hole.Substring(2) : hole))
                    .Append(")}");
                i = end;
                continue;
            }

            expression.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '}' => "}}",
                _ => c.ToString()
            });
        }

        return expression.Append('"').ToString();
    }
}
