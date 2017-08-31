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
        private readonly IBitConfigProvider _bitConfigProvider;

        public DefaultHtmlClientProxyCleaner(IBitConfigProvider bitConfigProvider)
        {
            if (bitConfigProvider == null)
                throw new ArgumentNullException(nameof(bitConfigProvider));

            _bitConfigProvider = bitConfigProvider;
        }

        public virtual async Task DeleteCodes(Workspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            BitConfig bitConfig = _bitConfigProvider.GetConfiguration(workspace.CurrentSolution.FilePath);

            foreach (BitCodeGeneratorMapping proxyGeneratorMapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
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
