using BitTools.Core.Contracts;
using BitTools.Core.Contracts.HtmlClientProxyGenerator;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        public virtual async Task DeleteCodes(Workspace workspace, IList<string> projectNames)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (projectNames == null)
                throw new ArgumentNullException(nameof(projectNames));

            foreach (BitCodeGeneratorMapping proxyGeneratorMapping in _bitCodeGeneratorMappingsProvider.GetBitCodeGeneratorMappings(workspace, projectNames))
            {
                string contextName = proxyGeneratorMapping.DestinationFileName;

                string jsContextExtension = ".js";
                string tsContextExtension = ".d.ts";

                Project destProject = workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                        .ExtendedSingle($"Trying to find project with name: {proxyGeneratorMapping.DestinationProject.Name}", p => p.Name == proxyGeneratorMapping.DestinationProject.Name);

                DeleteFiles(contextName, jsContextExtension, destProject);
                DeleteFiles(contextName, tsContextExtension, destProject);
            }
        }

        private static void DeleteFiles(string fileName, string extension, Project destProject)
        {
            string fullPath = Path.Combine(Directory.GetParent(destProject.FilePath).FullName, $"{fileName}{extension}");

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
