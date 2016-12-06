using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Foundation.CodeGenerators.Contracts;
using Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using System.IO;

namespace Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientProxyGenerator : IDefaultHtmlClientProxyGenerator
    {
        private readonly IHtmlClientProxyGeneratorSolutionProjectsSelector _solutionProjectsSelector;
        private readonly IHtmlClientProxyGeneratorMappingsProvider _contextMappingsProvider;
        private readonly IProjectDtosProvider _dtosProvider;
        private readonly IProjectDtoControllersProvider _dtoControllersProvider;
        private readonly IHtmlClientProxyDtosGenerator _dtoGenerator;
        private readonly IHtmlClientContextGenerator _contextGenerator;

        public DefaultHtmlClientProxyGenerator(IHtmlClientProxyGeneratorSolutionProjectsSelector solutionProjectsSelector, IHtmlClientProxyGeneratorMappingsProvider contextMappingsProvider, IProjectDtosProvider dtosProvider, IHtmlClientProxyDtosGenerator dtoGenerator, IHtmlClientContextGenerator contextGenerator, IProjectDtoControllersProvider dtoControllersProvider)
        {
            if (solutionProjectsSelector == null)
                throw new ArgumentNullException(nameof(solutionProjectsSelector));

            if (contextMappingsProvider == null)
                throw new ArgumentNullException(nameof(contextMappingsProvider));

            if (dtosProvider == null)
                throw new ArgumentNullException(nameof(dtosProvider));

            if (dtoGenerator == null)
                throw new ArgumentNullException(nameof(dtoGenerator));

            if (contextGenerator == null)
                throw new ArgumentNullException(nameof(contextGenerator));

            if (dtoControllersProvider == null)
                throw new ArgumentNullException(nameof(dtoControllersProvider));

            _solutionProjectsSelector = solutionProjectsSelector;
            _contextMappingsProvider = contextMappingsProvider;
            _dtosProvider = dtosProvider;
            _dtoGenerator = dtoGenerator;
            _contextGenerator = contextGenerator;
            _dtoControllersProvider = dtoControllersProvider;
        }

        public virtual void GenerateCodes(Workspace worksapce, Solution solution,
            IList<Project> projects)
        {
            if (worksapce == null)
                throw new ArgumentNullException(nameof(worksapce));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            foreach (HtmlClientProxyGeneratorMapping proxyGeneratorMapping in _contextMappingsProvider.GetHtmlClientProxyGeneratorMappings(worksapce, solution, projects))
            {
                string generatedContextName = proxyGeneratorMapping.DestinationFileName;

                StringBuilder generatedJs = new StringBuilder();
                StringBuilder generatedTs = new StringBuilder();

                string generatedJsContextExtension = ".js";
                string generatedTsContextExtension = ".d.ts";

                Project destProject = solution.Projects
                        .Last(p => p.Name == proxyGeneratorMapping.DestinationProject.Name);

                IList<Project> involveableProjects = _solutionProjectsSelector.GetInvolveableProjects(worksapce, solution, solution.Projects.ToList(), proxyGeneratorMapping);

                IList<Dto> dtos = involveableProjects
                    .SelectMany(p => _dtosProvider.GetProjectDtos(p, involveableProjects)).ToList();

                IList<DtoController> controllers = involveableProjects
                    .SelectMany(p => _dtoControllersProvider.GetProjectDtoControllersWithTheirOperations(p)).ToList();

                generatedJs.AppendLine(_dtoGenerator.GenerateJavaScriptDtos(dtos));
                generatedJs.AppendLine(_contextGenerator.GenerateJavaScriptContext(controllers, proxyGeneratorMapping));
                generatedTs.AppendLine(_dtoGenerator.GenerateTypeScriptDtos(dtos, proxyGeneratorMapping.TypingsPath));
                generatedTs.AppendLine(_contextGenerator.GenerateTypeScriptContext(controllers, proxyGeneratorMapping));

                WriteResults(solution, generatedJs.ToString(), generatedContextName, generatedJsContextExtension, destProject);

                WriteResults(solution, generatedTs.ToString(), generatedContextName, generatedTsContextExtension, destProject);
            }
        }

        private static void WriteResults(Solution solution, String generatedCodes, string fileName, string extension, Project destProject)
        {
            string fullPath = $@"{new FileInfo(destProject.FilePath).Directory}\{fileName}{extension}";

            File.WriteAllText(fullPath, generatedCodes, Encoding.UTF8);
        }
    }
}
