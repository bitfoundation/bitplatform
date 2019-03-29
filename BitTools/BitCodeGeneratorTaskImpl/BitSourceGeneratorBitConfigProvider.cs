using BitCodeGenerator.Implementations;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace BitCodeGeneratorTaskImpl
{
    public class BitSourceGeneratorBitConfigProvider : DefaultBitConfigProvider
    {
        private readonly string _solutionFilePath;
        private readonly string _beingCompiledProjectName;

        public BitSourceGeneratorBitConfigProvider(string solutionFilePath, string beingCompiledProjectName)
        {
            _solutionFilePath = solutionFilePath;
            _beingCompiledProjectName = beingCompiledProjectName;
        }

        public override string GetSolutionFilePath(Workspace workspace = null)
        {
            return _solutionFilePath;
        }

        public override BitConfig GetConfiguration(Workspace workspace = null)
        {
            BitConfig bitConfig = base.GetConfiguration(workspace);

            bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings = bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings.Where(config => config.SourceProjects.Any(sp => sp.Name == _beingCompiledProjectName)).ToArray();

            return bitConfig;
        }
    }
}
