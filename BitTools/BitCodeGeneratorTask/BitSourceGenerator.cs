using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using BitTools.Core.Contracts;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            CallGenerateCodes(context.Project.Solution.Workspace, solutionFullName);
        }

        private async void CallGenerateCodes(Workspace workspace, string solutionFullName)
        {
            Stopwatch sw = null;

            try
            {
                sw = Stopwatch.StartNew();

                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);

                DefaultTypeScriptClientProxyGenerator generator = new DefaultTypeScriptClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(),
                    new BitSourceGeneratorBitConfigProvider(solutionFullName), dtosProvider
                    , new DefaultTypeScriptClientProxyDtoGenerator(), new DefaultTypeScriptClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));

                await generator.GenerateCodes(workspace);

                Log($"Code Generation Completed in {sw.ElapsedMilliseconds} ms using {workspace.GetType().Name}");
            }
            catch (Exception ex)
            {
                LogException("Code Generation failed.", ex);
                throw;
            }
            finally
            {
                sw?.Stop();
            }
        }

        private void Log(string text)
        {
            _logger.Warn($">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName} <<<<< \n");
        }

        private void LogException(string text, Exception ex)
        {
            _logger.Error($">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName}<<<<< \n {ex} \n", ex);
        }
    }
}
