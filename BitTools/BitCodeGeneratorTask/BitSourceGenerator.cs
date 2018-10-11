using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Uno.SourceGeneration;

namespace BitCodeGeneratorTask
{
    public class BitSourceGenerator : SourceGenerator
    {
        private ISourceGeneratorLogger _logger;

        public override void Execute(SourceGeneratorContext context)
        {
#if DEBUG
            Debugger.Launch();
#endif

            _logger = context.GetLogger();

            DirectoryInfo projDir = new DirectoryInfo(Path.GetDirectoryName(context.Project.FilePath) ?? throw new InvalidOperationException("Context's project's file path is null"));

            string solutionFullName = null;

            while (projDir.Parent != null)
            {
                string filePath = Path.Combine(projDir.FullName, "BitConfigV1.json");

                if (File.Exists(filePath))
                {
                    solutionFullName = Directory.EnumerateFiles(projDir.FullName, "*.sln").FirstOrDefault();
                    break;
                }

                projDir = projDir.Parent;
            }

            CallGenerateCodes((MSBuildWorkspace)context.Project.Solution.Workspace, context.Project, solutionFullName).GetAwaiter().GetResult();
        }

        private async Task CallGenerateCodes(MSBuildWorkspace workspace, Project beingCompiledProject, string solutionFullName)
        {
            Stopwatch sw = null;

            try
            {
                sw = Stopwatch.StartNew();

                workspace.WorkspaceFailed += MSBuildWorkspace_WorkspaceFailed;
                workspace.SkipUnrecognizedProjects = workspace.LoadMetadataForReferencedProjects = true;

                IReadOnlyList<ProjectInSolution> allProjects = SolutionFile.Parse(solutionFullName).ProjectsInOrder;

                BitSourceGeneratorBitConfigProvider bitConfigProvider = new BitSourceGeneratorBitConfigProvider(solutionFullName);

                foreach (BitCodeGeneratorMapping mapping in bitConfigProvider.GetConfiguration(workspace).BitCodeGeneratorConfigs.BitCodeGeneratorMappings.Where(config => config.SourceProjects.Any(sp => sp.Name == beingCompiledProject.Name)))
                {
                    foreach (BitTools.Core.Model.ProjectInfo proj in mapping.SourceProjects)
                    {
                        if (workspace.CurrentSolution.Projects.Any(p => p.Name == proj.Name))
                            continue; /*It's already loaded*/

                        await workspace.OpenProjectAsync(allProjects.Single(p => p.ProjectName == proj.Name).AbsolutePath);
                    }
                }

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultTypeScriptClientProxyGenerator generator = new DefaultTypeScriptClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    bitConfigProvider, dtosProvider
                    , new DefaultTypeScriptClientProxyDtoGenerator(), new DefaultTypeScriptClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                await generator.GenerateCodes(workspace);

                Log($"Code Generation Completed in {sw.ElapsedMilliseconds} ms using {workspace.GetType().Name}.");
            }
            catch (Exception ex)
            {
                LogException("Code Generation failed.", ex);
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
                Log(e.Diagnostic.Message);
        }

        private void Log(string text)
        {
            text = $">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName} <<<<< \n";

            _logger.Warn(text);

            File.WriteAllText(Path.Combine(Path.GetTempPath(), $"Bit-Source-Generator-Log-Log{Guid.NewGuid()}.log"), text);
        }

        private void LogException(string text, Exception ex)
        {
            text = $">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName}<<<<< \n {ex} \n";

            _logger.Error(text, ex);

            File.WriteAllText(Path.Combine(Path.GetTempPath(), $"Bit-Source-Generator-Log-Log{Guid.NewGuid()}.log"), text);
        }
    }
}
