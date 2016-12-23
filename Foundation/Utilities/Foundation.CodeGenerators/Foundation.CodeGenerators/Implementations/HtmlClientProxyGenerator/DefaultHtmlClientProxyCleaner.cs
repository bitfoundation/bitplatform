using Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientProxyCleaner : IDefaultHtmlClientProxyCleaner
    {
        private readonly IHtmlClientProxyGeneratorMappingsProvider _contextMappingsProvider;

        public DefaultHtmlClientProxyCleaner(IHtmlClientProxyGeneratorMappingsProvider contextMappingsProvider)
        {
            if (contextMappingsProvider == null)
                throw new ArgumentNullException(nameof(contextMappingsProvider));

            _contextMappingsProvider = contextMappingsProvider;
        }

        public virtual void DeleteCodes(Workspace workspace, Solution solution,
            IList<Project> projects)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            foreach (HtmlClientProxyGeneratorMapping proxyGeneratorMapping in _contextMappingsProvider.GetHtmlClientProxyGeneratorMappings(workspace, solution, projects))
            {
                string contextName = proxyGeneratorMapping.DestinationFileName;

                string jsContextExtension = ".js";
                string tsContextExtension = ".d.ts";

                Project destProject = solution.Projects
                        .Last(p => p.Name == proxyGeneratorMapping.DestinationProject.Name);

                DeleteCodes(contextName, jsContextExtension, destProject);
                DeleteCodes(contextName, tsContextExtension, destProject);
            }
        }

        private static void DeleteCodes(string fileName, string extension, Project destProject)
        {
            string fullPath = $@"{new FileInfo(destProject.FilePath).Directory}\{fileName}{extension}";

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
