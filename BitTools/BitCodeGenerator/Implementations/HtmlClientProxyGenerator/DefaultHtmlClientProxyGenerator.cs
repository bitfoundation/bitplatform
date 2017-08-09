using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BitTools.Core.Contracts;
using BitTools.Core.Contracts.HtmlClientProxyGenerator;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace BitCodeGenerator.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientProxyGenerator : IDefaultHtmlClientProxyGenerator
    {
        private readonly IBitCodeGeneratorOrderedProjectsProvider _solutionProjectsSelector;
        private readonly IBitCodeGeneratorMappingsProvider _bitCodeGeneratorMappingsProvider;
        private readonly IProjectDtosProvider _dtosProvider;
        private readonly IProjectEnumTypesProvider _projectEnumTypesProvider;
        private readonly IProjectDtoControllersProvider _dtoControllersProvider;
        private readonly IHtmlClientProxyDtosGenerator _dtoGenerator;
        private readonly IHtmlClientContextGenerator _contextGenerator;

        public DefaultHtmlClientProxyGenerator(IBitCodeGeneratorOrderedProjectsProvider solutionProjectsSelector, IBitCodeGeneratorMappingsProvider contextMappingsProvider, IProjectDtosProvider dtosProvider, IHtmlClientProxyDtosGenerator dtoGenerator, IHtmlClientContextGenerator contextGenerator, IProjectDtoControllersProvider dtoControllersProvider, IProjectEnumTypesProvider projectEnumTypesProvider)
        {
            if (solutionProjectsSelector == null)
                throw new ArgumentNullException(nameof(solutionProjectsSelector));

            if (contextMappingsProvider == null)
                throw new ArgumentNullException(nameof(contextMappingsProvider));

            if (dtosProvider == null)
                throw new ArgumentNullException(nameof(dtosProvider));

            if (dtosProvider == null)
                throw new ArgumentNullException(nameof(dtosProvider));

            if (projectEnumTypesProvider == null)
                throw new ArgumentNullException(nameof(projectEnumTypesProvider));

            if (contextGenerator == null)
                throw new ArgumentNullException(nameof(contextGenerator));

            if (dtoControllersProvider == null)
                throw new ArgumentNullException(nameof(dtoControllersProvider));

            _solutionProjectsSelector = solutionProjectsSelector;
            _bitCodeGeneratorMappingsProvider = contextMappingsProvider;
            _dtosProvider = dtosProvider;
            _dtoGenerator = dtoGenerator;
            _contextGenerator = contextGenerator;
            _dtoControllersProvider = dtoControllersProvider;
            _projectEnumTypesProvider = projectEnumTypesProvider;
        }

        public virtual async Task GenerateCodes(Workspace workspace, IList<string> projectNames)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (projectNames == null)
                throw new ArgumentNullException(nameof(projectNames));

            foreach (BitCodeGeneratorMapping proxyGeneratorMapping in _bitCodeGeneratorMappingsProvider.GetBitCodeGeneratorMappings(workspace, projectNames))
            {
                string generatedContextName = proxyGeneratorMapping.DestinationFileName;

                StringBuilder generatedJs = new StringBuilder();
                StringBuilder generatedTs = new StringBuilder();

                string generatedJsContextExtension = ".js";
                string generatedTsContextExtension = ".d.ts";

                Project destProject = workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                    .ExtendedSingle($"Trying to find project with name: {proxyGeneratorMapping.DestinationProject.Name}", p => p.Name == proxyGeneratorMapping.DestinationProject.Name);

                IList<Project> involveableProjects = _solutionProjectsSelector.GetInvolveableProjects(workspace, workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp).ToList(), proxyGeneratorMapping);

                List<Dto> dtos = new List<Dto>();

                foreach (Project p in involveableProjects)
                {
                    dtos.AddRange(await _dtosProvider.GetProjectDtos(p, involveableProjects));
                }

                List<EnumType> enumTypes = new List<EnumType>();

                foreach (Project p in involveableProjects)
                {
                    enumTypes.AddRange(await _projectEnumTypesProvider.GetProjectEnumTypes(p, involveableProjects));
                }

                List<DtoController> dtoControllers = new List<DtoController>();

                foreach (Project p in involveableProjects)
                {
                    dtoControllers.AddRange(await _dtoControllersProvider.GetProjectDtoControllersWithTheirOperations(p));
                }

                generatedJs.AppendLine(_dtoGenerator.GenerateJavaScriptDtos(dtos, enumTypes));
                generatedJs.AppendLine(_contextGenerator.GenerateJavaScriptContext(dtoControllers, proxyGeneratorMapping));
                generatedTs.AppendLine(_dtoGenerator.GenerateTypeScriptDtos(dtos, enumTypes, proxyGeneratorMapping.TypingsPath));
                generatedTs.AppendLine(_contextGenerator.GenerateTypeScriptContext(dtoControllers, proxyGeneratorMapping));

                WriteFiles(workspace.CurrentSolution, generatedJs.ToString(), generatedContextName, generatedJsContextExtension, destProject);

                WriteFiles(workspace.CurrentSolution, generatedTs.ToString(), generatedContextName, generatedTsContextExtension, destProject);
            }
        }

        private static void WriteFiles(Solution solution, String generatedCodes, string fileName, string extension, Project destProject)
        {
            string fullPath = Path.Combine(Directory.GetParent(destProject.FilePath).FullName, $"{fileName}{extension}");

            File.WriteAllText(fullPath, generatedCodes, Encoding.UTF8);
        }
    }
}
