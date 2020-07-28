using Bit.Tooling.Core.Contracts;
using Bit.Tooling.Core.Model;
using Bit.Tooling.CodeGenerator.Implementations;
using Bit.Tooling.CodeGenerator.Implementations.CSharpClientProxyGenerator;
using Bit.Tooling.CodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using Microsoft.Build.Construction;
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
            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterInstance(MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(vs => vs.Version).FirstOrDefault() ?? throw new InvalidOperationException("Visual studio could not be found. Please install visual studio build tools."));

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

            InitPropjects();

            await GenerateCodes();
        }

        static void InitPropjects()
        {
            if (string.IsNullOrEmpty(_bitConfigFilePath))
                throw new BitConfigNotFoundException("Could not find bit config");

            foreach (string solutionPath in SolutionPaths)
            {
                AllProjectsPaths = AllProjectsPaths.Union(SolutionFile.Parse(solutionPath).ProjectsInOrder.Select(p => Path.GetFullPath(p.AbsolutePath))).ToArray();
            }
        }

        public static string[] AllProjectsPaths { get; set; } = Array.Empty<string>();
        public static string[] SolutionPaths { get; set; } = Array.Empty<string>();
        public static string ProjectPath { get; set; } = default!;
        public static string BeingCompiledProjectName { get; set; } = default!;

        private static string? _bitConfigFilePath;

        static async Task GenerateCodes()
        {
            BitSourceGeneratorBitConfigProvider bitConfigProvider = new BitSourceGeneratorBitConfigProvider(_bitConfigFilePath!, BeingCompiledProjectName);

            BitConfig bitConfig = bitConfigProvider.GetConfiguration();

            using (MSBuildWorkspace workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                { "TargetFramework", "net5.0" },
                { "_TargetFrameworkVersionValue", "5.0" }
            }))
            {
                workspace.SkipUnrecognizedProjects = workspace.LoadMetadataForReferencedProjects = true;

                workspace.WorkspaceFailed += MSBuildWorkspace_WorkspaceFailed;

                foreach (BitCodeGeneratorMapping mapping in bitConfigProvider.GetConfiguration().BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
                {
                    foreach (Bit.Tooling.Core.Model.ProjectInfo proj in mapping.SourceProjects)
                    {
                        if (workspace.CurrentSolution.Projects.Any(p => proj.IsThisProject(p)))
                            continue; /*It's already loaded*/

                        string sourceProjetctPath = proj.Name == BeingCompiledProjectName ? ProjectPath : (AllProjectsPaths ?? throw new InvalidOperationException($"There is no solution project and we're unable to find {proj.Name}")).ExtendedSingle($"Trying to find source project {proj.Name}", projPath => Path.GetFileNameWithoutExtension(projPath) == proj.Name);

                        await workspace.OpenProjectAsync(sourceProjetctPath);
                    }

                    if (!workspace.CurrentSolution.Projects.Any(p => mapping.DestinationProject.IsThisProject(p)))
                    {
                        string DestProjetctPath = mapping.DestinationProject.Name == BeingCompiledProjectName ? ProjectPath : (AllProjectsPaths ?? throw new InvalidOperationException($"There is no solution project and we're unable to find {mapping.DestinationProject.Name}")).ExtendedSingle($"Trying to find destination project {mapping.DestinationProject.Name}", projPath => Path.GetFileNameWithoutExtension(projPath) == mapping.DestinationProject.Name);
                        await workspace.OpenProjectAsync(DestProjetctPath);
                    }
                }

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);
                IBitCodeGeneratorOrderedProjectsProvider bitCodeGeneratorOrderedProjectsProvider = new DefaultBitCodeGeneratorOrderedProjectsProvider();
                IProjectEnumTypesProvider projectEnumTypesProvider = new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider);

                DefaultTypeScriptClientProxyGenerator tsGenerator = new DefaultTypeScriptClientProxyGenerator(bitCodeGeneratorOrderedProjectsProvider,
                    bitConfigProvider, dtosProvider
                    , new DefaultTypeScriptClientProxyDtoGenerator(), new DefaultTypeScriptClientContextGenerator(), controllersProvider, projectEnumTypesProvider);

                await tsGenerator.GenerateCodes(workspace);

                DefaultCSharpClientProxyGenerator csGenerator = new DefaultCSharpClientProxyGenerator(bitCodeGeneratorOrderedProjectsProvider,
                    bitConfigProvider, controllersProvider, new DefaultCSharpClientContextGenerator());

                await csGenerator.GenerateCodes(workspace);
            }
        }

        static void MSBuildWorkspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                Console.WriteLine(e.Diagnostic.Message);
        }
    }
}
