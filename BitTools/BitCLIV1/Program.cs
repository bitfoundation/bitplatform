using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.HtmlClientProxyGenerator;
using BitTools.Core.Contracts;
using Fclp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Diagnostics;
using System.IO;
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
        public static void Main(string[] args)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                Console.WriteLine("Starting...");
                AsyncMain(args).GetAwaiter().GetResult();
                Console.WriteLine("Code generation completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (ex is AggregateException aggExp)
                {
                    foreach (Exception innerException in aggExp.InnerExceptions)
                    {
                        Console.WriteLine(innerException.ToString());
                    }
                }

                Console.WriteLine("Code generation completed with errors");

                throw;
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"Finished... at {stopwatch.Elapsed.TotalSeconds.ToString("#.#")} seconds");
            }
        }

        private static async Task AsyncMain(string[] args)
        {
            FluentCommandLineParser<BitCLIV1Args> commandLineParser = new FluentCommandLineParser<BitCLIV1Args>();

            commandLineParser.Setup(arg => arg.Action)
                .As('a', "action")
                .Required()
                .WithDescription($"Action to perform. {nameof(BitCLIV1Action.Clean)} || {nameof(BitCLIV1Action.Generate)} || {nameof(BitCLIV1Action.Validate)}. Required");

            commandLineParser.Setup(arg => arg.Path)
                .As('p', "path")
                .Required()
                .WithDescription("Path to solution file. Required.");

            commandLineParser.SetupHelp("?", "help")
                .Callback(helpText => Console.WriteLine(helpText));

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

                Console.WriteLine($"Solution Path: {typedArgs.Path}");
                Console.WriteLine($"Action: {typedArgs.Action}");

                try
                {
                    using (Process dotnetBuildProcess = new Process())
                    {
                        dotnetBuildProcess.StartInfo.UseShellExecute = false;
                        dotnetBuildProcess.StartInfo.RedirectStandardOutput = true;
                        dotnetBuildProcess.StartInfo.FileName = "dotnet";
                        dotnetBuildProcess.StartInfo.Arguments = $"build {typedArgs.Path}";
                        dotnetBuildProcess.StartInfo.CreateNoWindow = true;
                        dotnetBuildProcess.StartInfo.WorkingDirectory = Directory.GetParent(typedArgs.Path).FullName;
                        dotnetBuildProcess.Start();
                        string output = await dotnetBuildProcess.StandardOutput.ReadToEndAsync();
                        Console.WriteLine(output);
                        dotnetBuildProcess.WaitForExit();
                    }

                    Console.WriteLine("DotNetBuild completed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DotNetBuild Error => {ex.ToString()}");
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
                            DefaultHtmlClientProxyGenerator generator = new DefaultHtmlClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(), new DefaultBitConfigProvider(), dtosProvider, new DefaultHtmlClientProxyDtoGenerator(), new DefaultHtmlClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));
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
            Console.WriteLine($"{e.Diagnostic.Kind} => {e.Diagnostic.Message}");
        }
    }
}
