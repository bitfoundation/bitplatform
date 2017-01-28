using BitTools.Core.Contracts;
using BitTools.Core.Contracts.HtmlClientProxyGenerator;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitCodeGenerator.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientProxyCleaner : IDefaultHtmlClientProxyCleaner
    {
        private readonly IBitCodeGeneratorMappingsProvider _bitCodeGeneratorMappingsProvider;

        public DefaultHtmlClientProxyCleaner(IBitCodeGeneratorMappingsProvider bitCodeGeneratorMappingsProvider)
        {
            if (bitCodeGeneratorMappingsProvider == null)
                throw new ArgumentNullException(nameof(bitCodeGeneratorMappingsProvider));

            _bitCodeGeneratorMappingsProvider = bitCodeGeneratorMappingsProvider;
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

            foreach (BitCodeGeneratorMapping proxyGeneratorMapping in _bitCodeGeneratorMappingsProvider.GetBitCodeGeneratorMappings(workspace, solution, projects))
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
