using System.Collections.Generic;
using System.Linq;
using DoLess.UriTemplates;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.SourceGenerators;

public class HttpClientProxySyntaxReceiver : ISyntaxContextReceiver
{
    public List<IController> IControllers { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext syntaxNode)
    {
        if (syntaxNode.Node is InterfaceDeclarationSyntax interfaceDeclarationSyntax
                                                  && interfaceDeclarationSyntax.BaseList is not null
                                                                                   && interfaceDeclarationSyntax.BaseList.Types.Any(t => t.Type.ToString() == "IAppController"))
        {
            var model = syntaxNode.SemanticModel.Compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree);
            var controllerSymbol = (ITypeSymbol)model.GetDeclaredSymbol(interfaceDeclarationSyntax)!;
            bool isController = controllerSymbol.IsIController();

            if (isController == true)
            {
                var controllerName = controllerSymbol.Name[1..].Replace("Controller", string.Empty);

                var route = controllerSymbol
                    .GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("Route") is true)?
                    .ConstructorArguments
                    .FirstOrDefault()
                    .Value?
                    .ToString()
                    ?.Replace("[controller]", controllerName) ?? string.Empty;

                var actions = controllerSymbol.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(m => m.MethodKind == MethodKind.Ordinary)
                        .Select(m => new ControllerAction
                        {
                            Method = m,
                            ReturnType = m.ReturnType,
                            HttpMethod = m.GetHttpMethod(),
                            Url = m.Name,
                            Parameters = m.Parameters.Select(y => new ActionParameter
                            {
                                Name = y.Name,
                                Type = y.Type
                            }).ToList()
                        }).ToList();

                foreach (var action in actions)
                {
                    var actionSpecificRoute = action.Method
                        .GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("Route") is true)?
                        .ConstructorArguments
                        .FirstOrDefault()
                        .Value?
                        .ToString()
                        ?.Replace("[controller]", controllerName)
                        ?.Replace("~/", string.Empty); // https://stackoverflow.com/a/34712201

                    var uriTemplate = UriTemplate.For($"{actionSpecificRoute ?? route}{action.Method.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name.StartsWith("Http") is true)?
                        .ConstructorArguments.FirstOrDefault().Value?.ToString()}".Replace("[action]", action.Method.Name));

                    foreach (var parameter in action.Parameters)
                    {
                        uriTemplate.WithParameter(parameter.Name, $"{{{parameter.Name}}}");
                    }

                    string url = HttpUtility.UrlDecode(uriTemplate.ExpandToString()).TrimEnd('/');

                    // if there is a parameter that is not a cancellation token and is not in the route template, then it is the body parameter
                    action.BodyParameter = action.Parameters.FirstOrDefault(p => p.Type.ToDisplayString() is not "System.Threading.CancellationToken" && url.Contains($"{{{p.Name}}}") is false);
                    action.CancellationTokenParameterName = action.Parameters.FirstOrDefault(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken")?.Name;
                    action.Url = url;
                }

                IControllers.Add(new IController
                {
                    Actions = actions,
                    Name = controllerName,
                    ClassName = controllerSymbol.Name[1..],
                    Symbol = controllerSymbol,
                    Syntax = interfaceDeclarationSyntax
                });
            }
        }
    }
}
