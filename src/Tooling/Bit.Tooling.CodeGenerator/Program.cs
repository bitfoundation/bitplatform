using Bit.Tooling.CodeGenerator.Implementations;
using Bit.Tooling.CodeGenerator.Implementations.CSharpHttpClientProxyGenerator;
using Bit.Tooling.CodeGenerator.Implementations.CSharpODataMetadataGenerator;
using Bit.Tooling.CodeGenerator.Implementations.CSharpSimpleODataClientProxyGenerator;
using Bit.Tooling.CodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using Bit.Tooling.Core.Contracts;
using Bit.Tooling.Core.Model;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BitCodeGeneratorTaskImpl
{
    public class Program
    {
        public const string BitConfigFileName = "BitConfig.json";

        public static async Task Main(string[] args)
        {
            Init(args);

            BitSourceGeneratorBitConfigProvider bitConfigProvider = new BitSourceGeneratorBitConfigProvider(_bitConfigFilePath!, BeingCompiledProjectName);

            BitConfig bitConfig = bitConfigProvider.GetConfiguration();

            Version? bitConfigVSVersion = string.IsNullOrEmpty(bitConfig.VisualStudioBuildToolsVersion) ? null : new Version(bitConfig.VisualStudioBuildToolsVersion);

            VisualStudioInstance? selectedVSInstance = MSBuildLocator.QueryVisualStudioInstances().Where(vs => bitConfigVSVersion == null || vs.Version == bitConfigVSVersion).OrderByDescending(vs => vs.Version).FirstOrDefault() ?? throw new InvalidOperationException("Visual studio could not be found. Please install visual studio build tools.");

            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterInstance(selectedVSInstance);

            Console.WriteLine($"{selectedVSInstance.Version} was selected from followings: {string.Join(",", MSBuildLocator.QueryVisualStudioInstances().Select(vs => vs.Version))}");

            await GenerateCodes(bitConfig, bitConfigProvider).ConfigureAwait(false);
        }

        static void Init(string[] args)
        {
            ProjectPath = args.ElementAt(1);

            DirectoryInfo projDir = new DirectoryInfo(Path.GetDirectoryName(ProjectPath));

            while (projDir.Parent != null)
            {
                SolutionPaths = SolutionPaths.Union(Directory.EnumerateFiles(projDir.FullName, "*.sln")).ToArray();

                _bitConfigFilePath = Path.Combine(projDir.FullName, BitConfigFileName);

                if (File.Exists(_bitConfigFilePath))
                    break;

                projDir = projDir.Parent;
            }

            BeingCompiledProjectName = Path.GetFileNameWithoutExtension(ProjectPath);

            if (string.IsNullOrEmpty(_bitConfigFilePath))
                throw new BitConfigNotFoundException("Could not find bit config");

            foreach (string solutionPath in SolutionPaths)
            {
                AllProjectsPaths = AllProjectsPaths.Union(new Bit.Tooling.CodeGenerator.Implementations.SolutionInfo(solutionPath).GetProjects().Select(p => Path.GetFullPath(p.AbsolutePath))).ToArray();
            }
        }

        public static string[] AllProjectsPaths { get; set; } = Array.Empty<string>();
        public static string[] SolutionPaths { get; set; } = Array.Empty<string>();
        public static string ProjectPath { get; set; } = default!;
        public static string BeingCompiledProjectName { get; set; } = default!;

        private static string? _bitConfigFilePath;

        static async Task GenerateCodes(BitConfig bitConfig, BitSourceGeneratorBitConfigProvider bitConfigProvider)
        {
            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                { "TargetFramework", bitConfig.TargetFramework ?? "net7.0" }
            }))
            {
                workspace.SkipUnrecognizedProjects = workspace.LoadMetadataForReferencedProjects = true;

                workspace.WorkspaceFailed += MSBuildWorkspace_WorkspaceFailed;

                foreach (BitCodeGeneratorMapping mapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
                {
                    foreach (Bit.Tooling.Core.Model.ProjectInfo proj in mapping.SourceProjects.Union(new[] { mapping.DestinationProject }))
                    {
                        if (workspace.CurrentSolution.Projects.Any(p => proj == p))
                            continue; /*It's already loaded*/

                        string sourceProjetctPath = proj.Name == BeingCompiledProjectName ? ProjectPath : (AllProjectsPaths ?? throw new InvalidOperationException($"There is no solution project and we're unable to find {proj.Name}")).ExtendedSingle($"Trying to find project {proj.Name}", projPath => Path.GetFileNameWithoutExtension(projPath) == proj.Name);

                        await workspace.OpenProjectAsync(sourceProjetctPath).ConfigureAwait(false);
                    }
                }

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);
                IBitCodeGeneratorOrderedProjectsProvider bitCodeGeneratorOrderedProjectsProvider = new DefaultBitCodeGeneratorOrderedProjectsProvider();
                IProjectEnumTypesProvider projectEnumTypesProvider = new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider);

                TypeScriptJayDataClientProxyGenerator typeScriptJayDataGeneratedCode = new TypeScriptJayDataClientProxyGenerator(bitCodeGeneratorOrderedProjectsProvider,
                    bitConfigProvider, dtosProvider
                    , new TypeScriptJayDataClientProxyDtoGenerator(), new TypeScriptJayDataClientContextGenerator(), controllersProvider, projectEnumTypesProvider);

                await typeScriptJayDataGeneratedCode.GenerateCodes(workspace).ConfigureAwait(false);

                CSharpSimpleODataClientProxyGenerator csharpSimpleODataClientGeneratedCode = new CSharpSimpleODataClientProxyGenerator(bitCodeGeneratorOrderedProjectsProvider,
                    bitConfigProvider, controllersProvider, new CSharpSimpleODataClientContextGenerator(), new CSharpSimpleODataClientMetadataGenerator(), dtosProvider, projectEnumTypesProvider);

                await csharpSimpleODataClientGeneratedCode.GenerateCodes(workspace).ConfigureAwait(false);

                CSharpHttpClientProxyGenerator csharpHttpClientProxyGenerator = new CSharpHttpClientProxyGenerator(bitCodeGeneratorOrderedProjectsProvider,
                    bitConfigProvider, controllersProvider, new CSharpHttpClientContextGenerator(), dtosProvider, projectEnumTypesProvider);

                await csharpHttpClientProxyGenerator.GenerateCodes(workspace).ConfigureAwait(false);
            }
        }

        static void MSBuildWorkspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                Console.WriteLine(e.Diagnostic.Message);
        }
    }
}
