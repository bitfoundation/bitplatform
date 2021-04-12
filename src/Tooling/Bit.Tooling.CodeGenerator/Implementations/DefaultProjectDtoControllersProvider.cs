using Bit.Tooling.Core.Contracts;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeGenerator.Implementations
{
    public class DefaultProjectDtoControllersProvider : IProjectDtoControllersProvider
    {
        public virtual async Task<IList<DtoController>> GetProjectDtoControllersWithTheirOperations(Project project, IList<Project>? allSourceProjects = null)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            IList<DtoController> dtoControllers = new List<DtoController>();

            foreach (Document doc in project.Documents)
            {
                if (!doc.SupportsSemanticModel)
                    continue;

                SemanticModel semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("SemanticModel is null");

                SyntaxNode root = await doc.GetSyntaxRootAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("SyntaxRoot is null");

                List<ClassDeclarationSyntax> dtoControllersClassDecs = new List<ClassDeclarationSyntax>();

                foreach (ClassDeclarationSyntax classDeclarationSyntax in root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>())
                {
                    if (classDeclarationSyntax.BaseList == null)
                        continue;

                    INamedTypeSymbol controllerSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(classDeclarationSyntax);

                    bool isController = controllerSymbol.IsDtoController();

                    if (isController == true)
                        dtoControllersClassDecs.Add(classDeclarationSyntax);
                }

                if (!dtoControllersClassDecs.Any())
                    continue;

                foreach (ClassDeclarationSyntax dtoControllerClassDec in dtoControllersClassDecs)
                {
                    INamedTypeSymbol controllerSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(dtoControllerClassDec);

                    DtoController dtoController = new DtoController
                    {
                        ControllerSymbol = controllerSymbol,
                        Name = controllerSymbol.Name.Replace("Controller", string.Empty, StringComparison.InvariantCultureIgnoreCase),
                        Operations = new List<ODataOperation>(),
                        ModelSymbol = controllerSymbol.BaseType.TypeArguments.ExtendedSingleOrDefault($"Looking for model of ${controllerSymbol.Name}", t => t.IsDto()) ?? await BuildAutoDto(controllerSymbol).ConfigureAwait(false)
                    };

                    if (dtoController.ModelSymbol is ITypeParameterSymbol symbol)
                    {
                        dtoController.ModelSymbol = symbol.ConstraintTypes.ExtendedSingleOrDefault($"Looking for model on generic model {symbol.Name}", t => t.IsDto());
                    }

                    if (dtoController.ControllerSymbol.IsGenericType || dtoController.ControllerSymbol.IsAbstract)
                        continue;

                    dtoControllers.Add(dtoController);

                    foreach (MethodDeclarationSyntax methodDecSyntax in dtoControllerClassDec.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        IMethodSymbol methodSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodDecSyntax);

                        if (!methodSymbol.IsOperation(out AttributeData operationAttribute))
                            continue;

                        ODataOperation operation = new ODataOperation
                        {
                            Method = methodSymbol,
                            Kind = operationAttribute.AttributeClass.Name == "ActionAttribute" ? ODataOperationKind.Action : ODataOperationKind.Function,
                            ReturnType = methodSymbol.ReturnType
                        };

                        if (operation.Kind == ODataOperationKind.Function)
                        {
                            operation.Parameters = operation.Method.Parameters
                                .Where(p => p.Type.Name != "CancellationToken" && p.Type.Name != "ODataQueryOptions")
                                .Select(parameter => new ODataOperationParameter
                                {
                                    Name = parameter.Name,
                                    Type = parameter.Type
                                }).ToList();
                        }
                        else if (operation.Kind == ODataOperationKind.Action && operation.Method.Parameters.Any())
                        {
                            IParameterSymbol actionParameterContainer = operation.Method.Parameters
                                   .Where(p => p.Type.Name != "CancellationToken" && p.Type.Name != "ODataQueryOptions")
                                   .ExtendedSingleOrDefault($"Finding parameter of {operation.Method.ContainingType.Name}.{operation.Method.Name}. It's expected to see 0 or 1 parameter only.");

                            if (actionParameterContainer != null)
                            {
                                if (actionParameterContainer.Type.IsDto() || actionParameterContainer.Type.IsComplexType() || actionParameterContainer.Type.IsCollectionType())
                                {
                                    operation.Parameters = new List<ODataOperationParameter>
                                    {
                                        new ODataOperationParameter
                                        {
                                            Name = actionParameterContainer.Name,
                                            Type = actionParameterContainer.Type
                                        }
                                    };
                                }
                                // ToDo: else if (parameter is string or primitive or enum or date time or date time offset) { throw an exception; }
                                else
                                {
                                    operation.Parameters = actionParameterContainer.Type.GetMembers()
                                           .OfType<IPropertySymbol>()
                                           .Select(prop => new ODataOperationParameter
                                           {
                                               Name = prop.Name,
                                               Type = prop.Type
                                           }).ToList();
                                }
                            }
                        }

                        dtoController.Operations.Add(operation);
                    }
                }
            }

            return dtoControllers;
        }

        async Task<ITypeSymbol> BuildAutoDto(INamedTypeSymbol controllerSymbol)
        {
            string className = $"Auto{controllerSymbol.Name.Replace("Controller", string.Empty)}Dto";

            ProjectId projectId = ProjectId.CreateNewId(debugName: $"{className}Project");
            DocumentId classSrc = DocumentId.CreateNewId(projectId, debugName: $"{className}.cs");

            using AdhocWorkspace workspace = new AdhocWorkspace();
            Solution solution = workspace
                .CurrentSolution
                .AddProject(projectId, $"{className}Project", $"{className}_{Guid.NewGuid().ToString("N")}_Assembly", LanguageNames.CSharp)
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(bool).Assembly.Location))
                .AddDocument(classSrc, $"{className}.cs", SourceText.From($@"
namespace Bit.Model.Contracts
{{
    public interface IDto {{ }}
}}

namespace Auto
{{
    public class {className} : Bit.Model.Contracts.IDto
    {{
        public Guid Id {{ get; set; }}
    }}
}}"
                ));

            Document classDoc = solution.Projects.Single().GetDocument(classSrc)!;
            ClassDeclarationSyntax classClassDec = (await classDoc.GetSyntaxRootAsync().ConfigureAwait(false)).DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            ITypeSymbol classTypeSymbol = (ITypeSymbol)(await classDoc.GetSemanticModelAsync()!.ConfigureAwait(false)).GetDeclaredSymbol(classClassDec);
            return classTypeSymbol;
        }
    }
}
