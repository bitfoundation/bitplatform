using System;
using System.Collections.Generic;
using System.Linq;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;

namespace BitCodeGenerator.Implementations
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

                Document isSyncedPropDoc = solution.Projects.Single().GetDocument(isSyncedPropDocId);
                ClassDeclarationSyntax isSyncedPropClassDec = (await isSyncedPropDoc.GetSyntaxRootAsync()).DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
                ITypeSymbol isSyncedPropTypeSymbol = (await isSyncedPropDoc.GetSemanticModelAsync()).GetDeclaredSymbol(isSyncedPropClassDec);
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

            IList<Dto> dtos = new List<Dto>();

            foreach (DtoController dtoController in dtoControllers)
            {
                Dto dto = new Dto
                {
                    DtoSymbol = (INamedTypeSymbol)dtoController.ModelSymbol,
                    Properties = dtoController.ModelSymbol.GetMembers().OfType<IPropertySymbol>().ToList()
                };

                if (dto.DtoSymbol.Interfaces.Any(i => i.Name == "ISyncableDto") && !dto.Properties.Any(p => p.Name == "IsSynced"))
                {
                    dto.Properties.Add(await _isSyncedPropertyForISyncableDtos.Value);
                }

                dtos.Add(dto);
            }

            List<Compilation> sourceProjectsCompilations = new List<Compilation>();

            foreach (var p in allSourceProjects)
            {
                sourceProjectsCompilations.Add(await p.GetCompilationAsync());
            }

            dtos.SelectMany(d => d.Properties)
                .Where(p => p.Type.IsComplexType())
                .Select(p => p.Type.GetUnderlyingComplexType())
                .Union(dtoControllers.SelectMany(dtoController => dtoController.Operations.SelectMany(operation => operation.Parameters.Select(p => p.Type).Union(new[] { operation.ReturnType }))).Where(t => t.IsComplexType()).Select(t => t.GetUnderlyingComplexType()))
                .Where(complexType => sourceProjectsCompilations.Any(c => c.Assembly.TypeNames.Any(tName => tName == complexType.Name)))
                .Distinct()
                .ToList()
                .ForEach(complexType =>
                {
                    dtos.Add(new Dto
                    {
                        DtoSymbol = (INamedTypeSymbol)complexType,
                        Properties = complexType.GetMembers().OfType<IPropertySymbol>().ToList()
                    });
                });

            dtos.ToList()
                .ForEach(dto =>
                {
                    if (dto.DtoSymbol.BaseType.IsDto())
                    {
                        dto.BaseDtoSymbol = dto.DtoSymbol.BaseType;
                    }
                });

            List<Dto> orderedDtos = new List<Dto>();

            dtos.ToList()
                .ForEach(dto =>
                {
                    int insertIndex = 0;

                    var orderedDtosWithIndex = orderedDtos.Select((d, i) => new { Dto = d, Index = i }).ToList();

                    foreach (var dWithIndex in orderedDtosWithIndex)
                    {
                        if (dto.BaseDtoSymbol?.Equals(dWithIndex.Dto.DtoSymbol) == true)
                            insertIndex = dWithIndex.Index + 1;
                    }

                    orderedDtos.Insert(insertIndex, dto);
                });

            return orderedDtos;
        }
    }
}
