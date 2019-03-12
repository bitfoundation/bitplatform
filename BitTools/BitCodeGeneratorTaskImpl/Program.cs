using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Fclp;
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
    public class BitCodeGeneratorTaskImplArgs
    {
        public string SolutionPath { get; set; }

        public string ProjectPath { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Bit Code Generator implementation started...");

            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();

            FluentCommandLineParser<BitCodeGeneratorTaskImplArgs> commandLineParser = new FluentCommandLineParser<BitCodeGeneratorTaskImplArgs>();

            commandLineParser.Setup(arg => arg.ProjectPath)
                .As('p', "projectPath")
                .Required();

            commandLineParser.Setup(arg => arg.SolutionPath)
                .As('s', "solutionPath")
                .Required();

            ICommandLineParserResult result = commandLineParser.Parse(args);

            if (result.HasErrors == true)
            {
                throw new Exception(result.ErrorText);
            }
            else
            {
                SolutionPath = commandLineParser.Object.SolutionPath;
                ProjectPath = commandLineParser.Object.ProjectPath;

                if (SolutionPath == "*Undefined*") // dotnet commands using portable msbuild of dotnet sdk-cli
                {
                    DirectoryInfo projDir = new DirectoryInfo(Path.GetDirectoryName(ProjectPath));

                    while (projDir.Parent != null)
                    {
                        string filePath = Path.Combine(projDir.FullName, "BitConfigV1.json");

                        if (File.Exists(filePath))
                        {
                            SolutionPath = Directory.EnumerateFiles(projDir.FullName, "*.sln").FirstOrDefault();
                            break;
                        }

                        projDir = projDir.Parent;
                    }
                }

                MSBuildWorkspace workspace = MSBuildWorkspace.Create();

                workspace.SkipUnrecognizedProjects = workspace.LoadMetadataForReferencedProjects = true;

                workspace.WorkspaceFailed += MSBuildWorkspace_WorkspaceFailed;

                workspace.OpenProjectAsync(ProjectPath).GetAwaiter().GetResult();

                CallGenerateCodes(workspace, workspace.CurrentSolution.Projects.Single()).GetAwaiter().GetResult();
            }
        }

        public static string SolutionPath { get; set; }
        public static string ProjectPath { get; set; }

        static async Task CallGenerateCodes(MSBuildWorkspace workspace, Project beingCompiledProject)
        {
            try
            {
                IReadOnlyList<ProjectInSolution> allProjects = SolutionFile.Parse(SolutionPath).ProjectsInOrder;

                BitSourceGeneratorBitConfigProvider bitConfigProvider = new BitSourceGeneratorBitConfigProvider(SolutionPath);

                foreach (BitCodeGeneratorMapping mapping in bitConfigProvider.GetConfiguration(workspace).BitCodeGeneratorConfigs.BitCodeGeneratorMappings.Where(config => config.SourceProjects.Any(sp => sp.Name == beingCompiledProject.Name)))
                {
                    foreach (BitTools.Core.Model.ProjectInfo proj in mapping.SourceProjects)
                    {
                        if (workspace.CurrentSolution.Projects.Any(p => p.Name == proj.Name))
                            continue; /*It's already loaded*/

                        await workspace.OpenProjectAsync(allProjects.ExtendedSingle($"Trying to find source project {proj.Name}", p => p.ProjectName == proj.Name).AbsolutePath);
                    }

                    if (!workspace.CurrentSolution.Projects.Any(p => p.Name == mapping.DestinationProject.Name))
                        await workspace.OpenProjectAsync(allProjects.ExtendedSingle($"Trying to find destination project {mapping.DestinationProject.Name}", p => p.ProjectName == mapping.DestinationProject.Name).AbsolutePath);
                }

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultTypeScriptClientProxyGenerator generator = new DefaultTypeScriptClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    bitConfigProvider, dtosProvider
                    , new DefaultTypeScriptClientProxyDtoGenerator(), new DefaultTypeScriptClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                await generator.GenerateCodes(workspace);
            }
            finally
            {
                workspace.WorkspaceFailed -= MSBuildWorkspace_WorkspaceFailed;
            }
        }

        static void MSBuildWorkspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                Console.WriteLine(e.Diagnostic.Message);
        }
    }
}
