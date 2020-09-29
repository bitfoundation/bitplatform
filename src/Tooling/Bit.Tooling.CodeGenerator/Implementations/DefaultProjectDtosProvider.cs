using Bit.Tooling.Core.Contracts;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeGenerator.Implementations
{
    public class DefaultProjectDtosProvider : IProjectDtosProvider
    {
        private readonly IProjectDtoControllersProvider _projectDtoControllersProvider;

        public DefaultProjectDtosProvider(IProjectDtoControllersProvider projectDtoControllersProvider)
        {
            if (projectDtoControllersProvider == null)
                throw new ArgumentNullException(nameof(projectDtoControllersProvider));

            _projectDtoControllersProvider = projectDtoControllersProvider;
        }

        static DefaultProjectDtosProvider()
        {
            _isSyncedPropertyForISyncableDtos = new Lazy<Task<IPropertySymbol>>(async () =>
            {
                ProjectId projectId = ProjectId.CreateNewId(debugName: "IsSyncedPropertyProject");
                DocumentId isSyncedPropDocId = DocumentId.CreateNewId(projectId, debugName: "IsSyncedProp.cs");

                Solution solution = new AdhocWorkspace()
                    .CurrentSolution
                    .AddProject(projectId, "IsSyncedPropertyProject", "IsSyncedPropertyProject", LanguageNames.CSharp)
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(bool).Assembly.Location))
                    .AddDocument(isSyncedPropDocId, "IsSyncedProp.cs", SourceText.From(@"
public class IsSyncedClass
{
    public bool IsSynced { get; set; }
}"
                    ));

                Document isSyncedPropDoc = solution.Projects.Single().GetDocument(isSyncedPropDocId)!;
                ClassDeclarationSyntax isSyncedPropClassDec = (await isSyncedPropDoc.GetSyntaxRootAsync()).DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
                ITypeSymbol isSyncedPropTypeSymbol = (await isSyncedPropDoc.GetSemanticModelAsync()!).GetDeclaredSymbol(isSyncedPropClassDec);
                return isSyncedPropTypeSymbol.GetMembers().OfType<IPropertySymbol>().Single();

            });
        }

        private static readonly Lazy<Task<IPropertySymbol>> _isSyncedPropertyForISyncableDtos;

        public virtual async Task<IList<Dto>> GetProjectDtos(Project project, IList<Project>? allSourceProjects = null)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (allSourceProjects == null)
                allSourceProjects = new List<Project> { project };

            List<DtoController> dtoControllers = (await _projectDtoControllersProvider
                .GetProjectDtoControllersWithTheirOperations(project)).ToList();

            List<Dto> dtosAndComplexTypes = new List<Dto>();

            foreach (var dtoOrComplexType in dtoControllers.Select(c => c.ModelSymbol).Union(dtoControllers.SelectMany(dtoController => dtoController.Operations.SelectMany(operation => operation.Parameters.Select(p => p.Type).Union(new[] { operation.ReturnType }))))
                .Select(x => x.GetUnderlyingTypeSymbol())
                .Select(x => x.IsCollectionType() || x.IsQueryableType() ? x.GetElementType() : x)
                .Where(t => t.IsComplexType() || t.IsDto())
                .SelectMany(t => new[] { t }.Union(t.BaseDtoClasses())))
            {
                FindDtoAndComplexTypes(dtoOrComplexType, ref dtosAndComplexTypes);
            }

            foreach (var dtoOrComplexType in dtosAndComplexTypes)
            {
                if (dtoOrComplexType.DtoSymbol.Interfaces.Any(i => i.Name == "ISyncableDto") && !dtoOrComplexType.Properties.Any(p => p.Name == "IsSynced"))
                {
                    dtoOrComplexType.Properties.Add(await _isSyncedPropertyForISyncableDtos.Value);
                }
            }

            dtosAndComplexTypes.ToList()
                .ForEach(dOrC =>
                {
                    if (dOrC.DtoSymbol.BaseType.IsDto())
                    {
                        dOrC.BaseDtoSymbol = dOrC.DtoSymbol.BaseType;
                    }
                });

            List<Dto> orderedFinalResult = new List<Dto>();

            dtosAndComplexTypes
                .ToList()
                .ForEach(dto =>
                {
                    int insertIndex = 0;

                    var orderedDtosWithIndex = orderedFinalResult.Select((d, i) => new { Dto = d, Index = i }).ToList();

                    foreach (var dWithIndex in orderedDtosWithIndex)
                    {
                        if (SymbolEqualityComparer.Default.Equals(dto.BaseDtoSymbol, dWithIndex.Dto.DtoSymbol) == true)
                            insertIndex = dWithIndex.Index + 1;
                    }

                    orderedFinalResult.Insert(insertIndex, dto);
                });

            return orderedFinalResult;
        }

        void FindDtoAndComplexTypes(ITypeSymbol dtoOrComplexTypeSymbol, ref List<Dto> dtosAndComplexTypes)
        {
            if (dtosAndComplexTypes.Any(dOrC => SymbolEqualityComparer.Default.Equals(dOrC.DtoSymbol, dtoOrComplexTypeSymbol)))
                return;

            Dto dtoOrComplexType = new Dto
            {
                DtoSymbol = (INamedTypeSymbol)dtoOrComplexTypeSymbol,
                Properties = dtoOrComplexTypeSymbol.GetMembers().OfType<IPropertySymbol>().ToList()
            };

            dtosAndComplexTypes.Add(dtoOrComplexType);

            foreach (var innerDtoOrComplexType in dtoOrComplexType.Properties
                .Select(p => p.Type.GetUnderlyingTypeSymbol())
                .Select(t => t.IsCollectionType() || t.IsQueryableType() ? t.GetElementType() : t)
                .Where(pType => pType.IsComplexType() || pType.IsDto()))
            {
                FindDtoAndComplexTypes(innerDtoOrComplexType, ref dtosAndComplexTypes);
            }
        }
    }
}
