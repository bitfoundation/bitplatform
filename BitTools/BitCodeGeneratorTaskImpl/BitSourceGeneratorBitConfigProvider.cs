using BitCodeGenerator.Implementations;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace BitCodeGeneratorTaskImpl
{
    public class BitSourceGeneratorBitConfigProvider : BitConfigProviderBase
    {
        private readonly string _bitConfigFilePath;
        private readonly string _beingCompiledProjectName;

        public BitSourceGeneratorBitConfigProvider(string bitConfigFilePath, string beingCompiledProjectName)
        {
            _bitConfigFilePath = bitConfigFilePath;
            _beingCompiledProjectName = beingCompiledProjectName;
        }

        public override string GetBitConfigFilePath()
        {
            return _bitConfigFilePath;
        }

        public override BitConfig GetConfiguration()
        {
            BitConfig bitConfig = base.GetConfiguration();

            bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings = bitConfig
                .BitCodeGeneratorConfigs
                .BitCodeGeneratorMappings
                .Where(config => config.SourceProjects.Any(sp => sp.Name == _beingCompiledProjectName))
                .ToArray();

            return bitConfig;
        }
    }
}
