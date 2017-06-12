using Fclp;
using System;

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
            FluentCommandLineParser<BitCLIV1Args> commandLineParser = new FluentCommandLineParser<BitCLIV1Args>();

            commandLineParser.Setup(arg => arg.Action)
                .As('a', "action")
                .Required()
                .WithDescription($"Action to perform. {nameof(BitCLIV1Action.Clean)} || {nameof(BitCLIV1Action.Generate)} || {nameof(BitCLIV1Action.Validate)}. Required");

            commandLineParser.Setup(arg => arg.Path)
                .As('p', "path")
                .SetDefault("//")
                .WithDescription("Path to folder which contains BitConfigV1.json file. Not Required. Default is current folder.");

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
            }
        }
    }
}
