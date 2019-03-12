using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BitCodeGeneratorTask
{
    public class BitSourceGenerator : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            try
            {
#if DEBUG
                System.Diagnostics.Debugger.Launch();
#endif

                LogMessage($"ProjectPath: {ProjectPath}");

                if (SolutionPath == "*Undefined*") // dotnet build
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

                LogMessage($"SolutionPath: {SolutionPath}");

                MSBuildWorkspace workspace = MSBuildWorkspace.Create();

                workspace.SkipUnrecognizedProjects = workspace.LoadMetadataForReferencedProjects = true;

                workspace.WorkspaceFailed += MSBuildWorkspace_WorkspaceFailed;

                workspace.OpenProjectAsync(ProjectPath).GetAwaiter().GetResult();

                CallGenerateCodes(workspace, workspace.CurrentSolution.Projects.Single()).GetAwaiter().GetResult();
            }
            catch (ReflectionTypeLoadException exp)
            {
                foreach (Exception e in exp.LoaderExceptions)
                {
                    LogError(e.Message, e);
                }
            }
            catch (Exception exp)
            {
                LogError(exp.Message, exp);
            }

            return true;
        }

        private async Task CallGenerateCodes(MSBuildWorkspace workspace, Project beingCompiledProject)
        {
            Stopwatch sw = null;

            try
            {
                sw = Stopwatch.StartNew();

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

                LogMessage($"Code Generation Completed in {sw.ElapsedMilliseconds} ms using {workspace.GetType().Name}.");
            }
            catch (Exception ex)
            {
                LogError("Code Generation failed.", ex);
                throw;
            }
            finally
            {
                sw?.Stop();
                workspace.WorkspaceFailed -= MSBuildWorkspace_WorkspaceFailed;
            }
        }

        private void MSBuildWorkspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                LogMessage(e.Diagnostic.Message);
        }

        private void LogMessage(string text)
        {
            text = $">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName} <<<<< \n";

            Log.LogMessage(MessageImportance.High, text);
        }

        private void LogError(string text, Exception ex)
        {
            text = $">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName}<<<<< \n {ex} \n";

            Log.LogError(text);
        }

        [Required]
        public string SolutionPath { get; set; }

        [Required]
        public string ProjectPath { get; set; }
    }
}
