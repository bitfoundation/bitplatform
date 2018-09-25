using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using BitTools.Core.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
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
        private MSBuildWorkspace _mSBuildWorkspace;

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

            Task.Run(() => CallGenerateCodes((MSBuildWorkspace)context.Project.Solution.Workspace, solutionFullName));
        }

        private async Task CallGenerateCodes(MSBuildWorkspace workspace, string solutionFullName)
        {
            Stopwatch sw = null;

            try
            {
                sw = Stopwatch.StartNew();

                _mSBuildWorkspace = MSBuildWorkspace.Create(workspace.Properties, workspace.Services.HostServices);
                _mSBuildWorkspace.WorkspaceFailed += MSBuildWorkspace_WorkspaceFailed;
                _mSBuildWorkspace.SkipUnrecognizedProjects = true;
                await _mSBuildWorkspace.OpenSolutionAsync(solutionFullName);

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultTypeScriptClientProxyGenerator generator = new DefaultTypeScriptClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    new BitSourceGeneratorBitConfigProvider(solutionFullName), dtosProvider
                    , new DefaultTypeScriptClientProxyDtoGenerator(), new DefaultTypeScriptClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                await generator.GenerateCodes(_mSBuildWorkspace);

                Log($"Code Generation Completed in {sw.ElapsedMilliseconds} ms using {_mSBuildWorkspace.GetType().Name}.");
            }
            catch (Exception ex)
            {
                LogException("Code Generation failed.", ex);
                throw;
            }
            finally
            {
                sw?.Stop();
                if (_mSBuildWorkspace != null)
                    _mSBuildWorkspace.WorkspaceFailed -= MSBuildWorkspace_WorkspaceFailed;
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
