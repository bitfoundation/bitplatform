using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator;
using BitTools.Core.Contracts;
using Fclp;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BitCLIV1
{
    [Flags]
    public enum BitCLIV1Action
    {
        Generate = 1,
        Validate = 2,
        Clean = 4
    }

    public class BitCLIV1Args
    {
        public BitCLIV1Action Action { get; set; }

        public string Path { get; set; }
    }

    public class Program
    {
        private static void WriteInfo(string message)
        {
            WriteLine(message, ConsoleColor.Blue, isError: false);
        }

        private static void WriteWarning(string message)
        {
            WriteLine(message, ConsoleColor.Yellow, isError: false);
        }

        private static void WriteWellMessage(string message)
        {
            WriteLine(message, ConsoleColor.Green, isError: false);
        }

        private static void WriteMessage(string message)
        {
            WriteLine(message, ConsoleColor.White, isError: false);
        }

        private static void WriteError(string message)
        {
            WriteLine(message, ConsoleColor.Red, isError: true);
        }

        private static void WriteLine(string message, ConsoleColor color, bool isError)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (isError == false)
                Console.WriteLine(message);
            else
                Console.Error.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void Main(string[] args)
        {
            if (ProjectCollection.GlobalProjectCollection.GetToolset("15.0") == null)
            {
                throw new Exception("MSBuild 15 not found");
            }

            ProjectCollection.GlobalProjectCollection.DefaultToolsVersion = "15.0";

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                WriteMessage("Starting...");
                AsyncMain(args).GetAwaiter().GetResult();
                WriteWellMessage("Code generation completed successfully");
            }
            catch (Exception ex)
            {
                WriteError(ex.ToString());

                if (ex is ReflectionTypeLoadException refExp)
                {
                    foreach (Exception innerException in refExp.LoaderExceptions)
                    {
                        WriteError(innerException.ToString());
                    }
                }

                if (ex is AggregateException aggExp)
                {
                    foreach (Exception innerException in aggExp.InnerExceptions)
                    {
                        WriteError(innerException.ToString());
                    }
                }

                WriteError("Code generation completed with errors");

                Environment.Exit(-1);
            }
            finally
            {
                stopwatch.Stop();
                WriteMessage($"Finished... at {stopwatch.Elapsed.TotalSeconds.ToString("#.#")} seconds");
                Environment.Exit(0);
            }
        }

        private static async Task AsyncMain(string[] args)
        {
            FluentCommandLineParser<BitCLIV1Args> commandLineParser = new FluentCommandLineParser<BitCLIV1Args>();

            commandLineParser.Setup(arg => arg.Action)
                .As('a', "action")
                .SetDefault(BitCLIV1Action.Generate)
                .WithDescription($"Action to perform. {nameof(BitCLIV1Action.Clean)} || {nameof(BitCLIV1Action.Generate)} || {nameof(BitCLIV1Action.Validate)}. Required");

            commandLineParser.Setup(arg => arg.Path)
                .As('p', "path")
                .Required()
                .WithDescription("Path to solution file. Required.");

            commandLineParser.SetupHelp("?", "help")
                .Callback(helpText => WriteInfo(helpText));

            ICommandLineParserResult result = commandLineParser.Parse(args);

            if (result.HasErrors == true)
            {
                throw new Exception(result.ErrorText);
            }
            else
            {
                BitCLIV1Args typedArgs = commandLineParser.Object;

                typedArgs.Path = Path.Combine(Environment.CurrentDirectory, typedArgs.Path);

                if (!File.Exists(typedArgs.Path))
                    throw new FileNotFoundException($"Solution could not be found at {typedArgs.Path}");

                WriteInfo($"Solution Path: {typedArgs.Path}");
                WriteInfo($"Action: {typedArgs.Action}");

                try
                {
                    WriteMessage("DotNetBuild started...");

                    using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                    using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                    {
                        using (Process dotnetBuildProcess = new Process())
                        {
                            dotnetBuildProcess.StartInfo.UseShellExecute = false;
                            dotnetBuildProcess.StartInfo.RedirectStandardOutput = dotnetBuildProcess.StartInfo.RedirectStandardError = true;
                            dotnetBuildProcess.StartInfo.FileName = "dotnet";
                            dotnetBuildProcess.StartInfo.Arguments = $"build {typedArgs.Path}";
                            dotnetBuildProcess.StartInfo.CreateNoWindow = true;
                            dotnetBuildProcess.StartInfo.WorkingDirectory = Directory.GetParent(typedArgs.Path).FullName;
                            dotnetBuildProcess.OutputDataReceived += (sender, e) =>
                            {
                                if (e.Data != null)
                                    WriteMessage(e.Data);
                                else
                                    outputWaitHandle.Set();
                            };
                            dotnetBuildProcess.ErrorDataReceived += (sender, e) =>
                            {
                                if (e.Data != null)
                                    WriteError(e.Data);
                                else
                                    errorWaitHandle.Set();
                            };
                            dotnetBuildProcess.Start();
                            dotnetBuildProcess.BeginOutputReadLine();
                            dotnetBuildProcess.BeginErrorReadLine();
                            dotnetBuildProcess.WaitForExit();
                            outputWaitHandle.WaitOne();
                            errorWaitHandle.WaitOne();
                        }
                    }

                    WriteMessage("DotNetBuild completed");
                }
                catch (Exception ex)
                {
                    WriteError($"DotNetBuild Error => {ex.ToString()}");
                }

                using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
                {
                    workspace.LoadMetadataForReferencedProjects = workspace.SkipUnrecognizedProjects = true;

                    workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

                    await workspace.OpenSolutionAsync(typedArgs.Path);

                    switch (typedArgs.Action)
                    {
                        case BitCLIV1Action.Generate:
                            IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                            IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);
                            DefaultTypeScriptClientProxyGenerator generator = new DefaultTypeScriptClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(), new DefaultBitConfigProvider(), dtosProvider, new DefaultTypeScriptClientProxyDtoGenerator(), new DefaultTypeScriptClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));
                            await generator.GenerateCodes(workspace);
                            break;
                        case BitCLIV1Action.Validate:
                            throw new NotImplementedException("Validate");
                        case BitCLIV1Action.Clean:
                            throw new NotImplementedException("Clean");
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }

        private static void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            string message = $"{e.Diagnostic.Kind} => {e.Diagnostic.Message}";
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                WriteError(message);
            else
                WriteWarning(message);
        }
    }
}
