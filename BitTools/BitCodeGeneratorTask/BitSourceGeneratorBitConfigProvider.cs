using BitCodeGenerator.Implementations;
using Microsoft.CodeAnalysis;

namespace BitCodeGeneratorTask
{
    public class BitSourceGeneratorBitConfigProvider : DefaultBitConfigProvider
    {
        private readonly string _solutionFilePath;

        public BitSourceGeneratorBitConfigProvider(string solutionFilePath)
        {
            _solutionFilePath = solutionFilePath;
        }

        public override string GetSolutionFilePath(Workspace workspace)
        {
            return _solutionFilePath;
        }
    }
}
