using BitTools.Core.Contracts;
using BitTools.Core.Contracts.CSharpClientProxyGenerator;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitCodeGenerator.Implementations.CSharpClientProxyGenerator
{
    public class DefaultCSharpClientProxyGenerator : IDefaultCSharpClientProxyGenerator
    {
        private readonly IBitConfigProvider _bitConfigProvider;

        public DefaultCSharpClientProxyGenerator(IBitConfigProvider bitConfigProvider)
        {
            if (bitConfigProvider == null)
                throw new ArgumentNullException(nameof(bitConfigProvider));

            _bitConfigProvider = bitConfigProvider;
        }

        public virtual async Task GenerateCodes(Workspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            BitConfig bitConfig = _bitConfigProvider.GetConfiguration();

            foreach (BitCodeGeneratorMapping proxyGeneratorMapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings.Where(m => m.GenerationType == GenerationType.CSharp))
            {
                string generatedContextName = proxyGeneratorMapping.DestinationFileName;

                Project destProject = workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                    .ExtendedSingle($"Trying to find project with name: {proxyGeneratorMapping.DestinationProject.Name}", p => p.Name == proxyGeneratorMapping.DestinationProject.Name);
            }
        }

        private static void WriteFiles(string generatedCodes, string fileName, string extension, Project destProject)
        {
            string fullPath = Path.Combine(Directory.GetParent(destProject.FilePath).FullName, $"{fileName}{extension}");

            File.WriteAllText(fullPath, generatedCodes, Encoding.UTF8);
        }
    }
}
