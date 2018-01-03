using BitCodeGenerator.Implementations;
using BitCodeGenerator.Implementations.HtmlClientProxyGenerator;
using BitTools.Core.Contracts;
using Fclp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;

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

        public string SolutionPath { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            ASyncMain(args).GetAwaiter().GetResult();
        }

        public static async System.Threading.Tasks.Task ASyncMain(string[] args)
        {
            FluentCommandLineParser<BitCLIV1Args> commandLineParser = new FluentCommandLineParser<BitCLIV1Args>();

            commandLineParser.Setup(arg => arg.Action)
                .As('a', "action")
                .Required()
                .WithDescription($"Action to perform. {nameof(BitCLIV1Action.Clean)} || {nameof(BitCLIV1Action.Generate)} || {nameof(BitCLIV1Action.Validate)}. Required");

            commandLineParser.Setup(arg => arg.SolutionPath)
                .As('p', "path")
                .Required()
                .WithDescription("Path to solution file. Required.");

            commandLineParser.SetupHelp("?", "help")
                .Callback(helpText => Console.WriteLine(helpText));

            ICommandLineParserResult result = commandLineParser.Parse(args);

            if (result.HasErrors == true)
            {
                Console.WriteLine(result.ErrorText);
            }
            else
            {
                BitCLIV1Args typedArgs = commandLineParser.Object;

                typedArgs.SolutionPath = Path.Combine(Environment.CurrentDirectory, typedArgs.SolutionPath);

                using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
                {
                    workspace.LoadMetadataForReferencedProjects = workspace.SkipUnrecognizedProjects = true;

                    workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

                    workspace.OpenSolutionAsync(typedArgs.SolutionPath).GetAwaiter().GetResult();

                    try
                    {
                        switch (typedArgs.Action)
                        {
                            case BitCLIV1Action.Generate:
                                IProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();
                                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(controllersProvider);
                                DefaultHtmlClientProxyGenerator generator = new DefaultHtmlClientProxyGenerator(new DefaultBitCodeGeneratorOrderedProjectsProvider(), new DefaultBitConfigProvider(), dtosProvider, new DefaultHtmlClientProxyDtoGenerator(), new DefaultHtmlClientContextGenerator(), controllersProvider, new DefaultProjectEnumTypesProvider(controllersProvider, dtosProvider));
                                generator.GenerateCodes(workspace).GetAwaiter().GetResult();
                                break;
                            case BitCLIV1Action.Validate:
                                throw new NotImplementedException("Validate");
                            case BitCLIV1Action.Clean:
                                throw new NotImplementedException("Clean");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
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
