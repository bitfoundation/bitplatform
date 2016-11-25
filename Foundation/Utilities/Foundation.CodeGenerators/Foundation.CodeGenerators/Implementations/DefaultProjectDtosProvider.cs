using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.CodeGenerators.Contracts;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Foundation.CodeGenerators.Implementations
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
            _isvPropertyForISyncableDtos = new Lazy<IPropertySymbol>(() =>
            {
                ProjectId projectId = ProjectId.CreateNewId(debugName: "ISVPropertyProject");
                DocumentId isvPropDocId = DocumentId.CreateNewId(projectId, debugName: $"ISVProp.cs");

                Solution solution = new AdhocWorkspace()
                    .CurrentSolution
                    .AddProject(projectId, "ISVPropertyProject", "ISVPropertyProject", LanguageNames.CSharp)
                    .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(bool).Assembly.Location))
                    .AddDocument(isvPropDocId, $"ISVProp.cs", SourceText.From(@"
public class ISVClass
{
    public bool ISV { get; set; }
}"
                    ));

                Document isvPropDoc = solution.Projects.Single().Documents.Single();
                ClassDeclarationSyntax isvPropClassDec = isvPropDoc.GetSyntaxRootAsync().Result.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
                ITypeSymbol isvPropTypeSymbol = (ITypeSymbol)isvPropDoc.GetSemanticModelAsync().Result.GetDeclaredSymbol(isvPropClassDec);
                return isvPropTypeSymbol.GetMembers().OfType<IPropertySymbol>().Single();

            });
        }

        private static Lazy<IPropertySymbol> _isvPropertyForISyncableDtos;

        public virtual IList<Dto> GetProjectDtos(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            IList<Dto> dtos = _projectDtoControllersProvider
                .GetProjectDtoControllersWithTheirOperations(project)
                .Select(dtoController =>
                {
                    Dto dto = new Dto
                    {
                        DtoSymbol = (INamedTypeSymbol)dtoController.ModelSymbol,
                        Properties = dtoController.ModelSymbol.GetMembers().OfType<IPropertySymbol>().ToList()
                    };

                    if (dto.DtoSymbol.Interfaces.Any(i => i.Name == "ISyncableDto") && !dto.Properties.Any(p => p.Name == "ISV"))
                    {
                        dto.Properties.Add(_isvPropertyForISyncableDtos.Value);
                    }

                    return dto;
                })
                .ToList();

            return dtos;
        }
    }
}
