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
        public override void Execute(SourceGeneratorContext context)
        {
#if DEBUG
            Debugger.Launch();
#endif

            DirectoryInfo projDir = new DirectoryInfo(Path.GetDirectoryName(context.Project.FilePath));

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
            Console.WriteLine($"{text} {DateTimeOffset.Now} \n");
        }

        private void LogException(string text, Exception ex)
        {
            ConsoleColor color = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{text} {DateTimeOffset.Now} \n {ex} \n");
            }
            finally
            {
                Console.ForegroundColor = color;
            }
        }
    }
}
