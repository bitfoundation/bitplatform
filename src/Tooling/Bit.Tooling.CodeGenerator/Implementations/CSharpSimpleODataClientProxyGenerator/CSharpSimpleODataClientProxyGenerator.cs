using Bit.Tooling.Core.Contracts;
using Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeGenerator.Implementations.CSharpSimpleODataClientProxyGenerator
{
    public class CSharpSimpleODataClientProxyGenerator : IDefaultCSharpClientProxyGenerator
    {
        private readonly IBitCodeGeneratorOrderedProjectsProvider _bitCodeGeneratorOrderedProjectsProvider;
        private readonly IBitConfigProvider _bitConfigProvider;
        private readonly IProjectDtoControllersProvider _dtoControllersProvider;
        private readonly ICSharpClientContextGenerator _contextGenerator;
        private readonly ICSharpClientMetadataGenerator _metadataGenerator;
        private readonly IProjectDtosProvider _dtosProvider;
        private IProjectEnumTypesProvider _enumsProvider;

        public CSharpSimpleODataClientProxyGenerator(IBitCodeGeneratorOrderedProjectsProvider bitCodeGeneratorOrderedProjectsProvider,
            IBitConfigProvider bitConfigProvider,
            IProjectDtoControllersProvider dtoControllersProvider,
            ICSharpClientContextGenerator contextGenerator,
            ICSharpClientMetadataGenerator metadataGenerator,
            IProjectDtosProvider dtosProvider,
            IProjectEnumTypesProvider enumsProvider)
        {
            if (bitCodeGeneratorOrderedProjectsProvider == null)
                throw new ArgumentNullException(nameof(bitCodeGeneratorOrderedProjectsProvider));

            if (bitConfigProvider == null)
                throw new ArgumentNullException(nameof(bitConfigProvider));

            if (dtoControllersProvider == null)
                throw new ArgumentNullException(nameof(dtoControllersProvider));

            if (contextGenerator == null)
                throw new ArgumentNullException(nameof(contextGenerator));

            if (metadataGenerator == null)
                throw new ArgumentNullException(nameof(metadataGenerator));

            if (dtosProvider == null)
                throw new ArgumentNullException(nameof(dtosProvider));

            if (enumsProvider == null)
                throw new ArgumentNullException(nameof(enumsProvider));

            _bitCodeGeneratorOrderedProjectsProvider = bitCodeGeneratorOrderedProjectsProvider;
            _bitConfigProvider = bitConfigProvider;
            _dtoControllersProvider = dtoControllersProvider;
            _contextGenerator = contextGenerator;
            _metadataGenerator = metadataGenerator;
            _dtosProvider = dtosProvider;
            _enumsProvider = enumsProvider;
        }

        public virtual async Task GenerateCodes(Workspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            BitConfig bitConfig = _bitConfigProvider.GetConfiguration();

            foreach (BitCodeGeneratorMapping proxyGeneratorMapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings.Where(m => m.GenerationType == GenerationType.CSharpSimpleODataClient))
            {
                string generatedContextName = proxyGeneratorMapping.DestinationFileName;

                StringBuilder generatedCs = new StringBuilder();

                string generatedCSContextExtension = ".cs";

                Project destProject = workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp)
                    .ExtendedSingle($"Trying to find project with name: {proxyGeneratorMapping.DestinationProject.Name}", p => proxyGeneratorMapping.DestinationProject == p);

                IList<Project> involveableProjects = _bitCodeGeneratorOrderedProjectsProvider.GetInvolveableProjects(workspace, workspace.CurrentSolution.Projects.Where(p => p.Language == LanguageNames.CSharp).ToList(), proxyGeneratorMapping);

                List<DtoController> dtoControllers = new List<DtoController>();

                foreach (Project p in involveableProjects)
                {
                    dtoControllers.AddRange(await _dtoControllersProvider.GetProjectDtoControllersWithTheirOperations(p));
                }

                List<Dto> dtos = new List<Dto>();

                foreach (Project p in involveableProjects)
                {
                    dtos.AddRange(await _dtosProvider.GetProjectDtos(p, involveableProjects));
                }

                List<EnumType> enumTypes = new List<EnumType>();

                foreach (Project p in involveableProjects)
                {
                    enumTypes.AddRange(await _enumsProvider.GetProjectEnumTypes(p, involveableProjects));
                }

                generatedCs.AppendLine(_contextGenerator.GenerateCSharpContext(dtoControllers, proxyGeneratorMapping));

                generatedCs.AppendLine(_metadataGenerator.GenerateMetadata(dtos, enumTypes, dtoControllers, proxyGeneratorMapping));

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
