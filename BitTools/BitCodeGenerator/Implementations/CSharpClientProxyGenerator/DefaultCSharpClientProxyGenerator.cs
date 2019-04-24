using BitTools.Core.Contracts;
using BitTools.Core.Contracts.CSharpClientProxyGenerator;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitCodeGenerator.Implementations.CSharpClientProxyGenerator
{
    public class DefaultCSharpClientProxyGenerator : IDefaultCSharpClientProxyGenerator
    {
        private readonly IBitCodeGeneratorOrderedProjectsProvider _bitCodeGeneratorOrderedProjectsProvider;
        private readonly IBitConfigProvider _bitConfigProvider;
        private readonly IProjectDtoControllersProvider _dtoControllersProvider;
        private readonly ICSharpClientContextGenerator _contextGenerator;

        public DefaultCSharpClientProxyGenerator(IBitCodeGeneratorOrderedProjectsProvider bitCodeGeneratorOrderedProjectsProvider, IBitConfigProvider bitConfigProvider, IProjectDtoControllersProvider dtoControllersProvider, ICSharpClientContextGenerator contextGenerator)
        {
            if (bitCodeGeneratorOrderedProjectsProvider == null)
                throw new ArgumentNullException(nameof(bitCodeGeneratorOrderedProjectsProvider));

            if (bitConfigProvider == null)
                throw new ArgumentNullException(nameof(bitConfigProvider));

            if (dtoControllersProvider == null)
                throw new ArgumentNullException(nameof(dtoControllersProvider));

            if (contextGenerator == null)
                throw new ArgumentNullException(nameof(contextGenerator));

            _bitCodeGeneratorOrderedProjectsProvider = bitCodeGeneratorOrderedProjectsProvider;
            _bitConfigProvider = bitConfigProvider;
            _dtoControllersProvider = dtoControllersProvider;
            _contextGenerator = contextGenerator;
        }

        public virtual async Task GenerateCodes(Workspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            BitConfig bitConfig = _bitConfigProvider.GetConfiguration();

            foreach (BitCodeGeneratorMapping proxyGeneratorMapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings.Where(m => m.GenerationType == GenerationType.CSharp))
            {
                string generatedContextName = proxyGeneratorMapping.DestinationFileName;

                StringBuilder generatedCs = new StringBuilder();

                string generatedCSContextExtension = ".cs";

                Project destProject = workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                    .ExtendedSingle($"Trying to find project with name: {proxyGeneratorMapping.DestinationProject.Name}", p => proxyGeneratorMapping.DestinationProject.IsThisProject(p));

                IList<Project> involveableProjects = _bitCodeGeneratorOrderedProjectsProvider.GetInvolveableProjects(workspace, workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp).ToList(), proxyGeneratorMapping);

                List<DtoController> dtoControllers = new List<DtoController>();

                foreach (Project p in involveableProjects)
                {
                    dtoControllers.AddRange(await _dtoControllersProvider.GetProjectDtoControllersWithTheirOperations(p));
                }

                generatedCs.AppendLine(_contextGenerator.GenerateCSharpContext(dtoControllers, proxyGeneratorMapping));

                WriteFiles(generatedCs.ToString(), generatedContextName, generatedCSContextExtension, destProject);
            }
        }

        private static void WriteFiles(string generatedCodes, string fileName, string extension, Project destProject)
        {
            string fullPath = Path.Combine(Directory.GetParent(destProject.FilePath).FullName, $"{fileName}{extension}");

            File.WriteAllText(fullPath, generatedCodes, Encoding.UTF8);
        }
    }
}
