using Bit.Tooling.CodeGenerator.Implementations;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace BitCodeGeneratorTaskImpl
{
    public class BitSourceGeneratorBitConfigProvider : BitConfigProviderBase
    {
        private readonly string _bitConfigFilePath;
        private readonly string _beingCompiledProjectName;

        public BitSourceGeneratorBitConfigProvider(string bitConfigFilePath, string beingCompiledProjectName)
        {
            if (bitConfigFilePath == null)
                throw new ArgumentNullException(nameof(bitConfigFilePath));

            if (beingCompiledProjectName == null)
                throw new ArgumentNullException(nameof(beingCompiledProjectName));

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
