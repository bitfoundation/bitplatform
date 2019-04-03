using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace BitCodeGenerator.Implementations
{
    public class BitConfigNotFoundException : Exception
    {
        public BitConfigNotFoundException(string message)
            : base(message)
        {

        }
    }

    public abstract class BitConfigProviderBase : IBitConfigProvider
    {
        public abstract string GetBitConfigFilePath();

        public virtual BitConfig GetConfiguration()
        {
            BitConfig bitConfig = JsonConvert
                .DeserializeObject<BitConfig>(
                    File.ReadAllText(GetBitConfigFilePath()));

            foreach (BitCodeGeneratorMapping mapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
            {
                if (string.IsNullOrEmpty(mapping.DestinationFileName))
                    throw new InvalidOperationException($"{nameof(BitCodeGeneratorMapping.DestinationFileName)} is not provided");

                if (string.IsNullOrEmpty(mapping.DestinationProject?.Name))
                    throw new InvalidOperationException($"{nameof(BitCodeGeneratorMapping.DestinationProject)} is not provided");

                if (mapping.SourceProjects == null || !mapping.SourceProjects.Any() || !mapping.SourceProjects.All(sp => !string.IsNullOrEmpty(sp?.Name)))
                    throw new InvalidOperationException($"No {nameof(BitCodeGeneratorMapping.SourceProjects)} is not provided");

                if (string.IsNullOrEmpty(mapping.Route))
                    throw new InvalidOperationException($"{nameof(BitCodeGeneratorMapping.Route)} is not provided");
            }

            return bitConfig;
        }
    }
}
