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

    public class DefaultBitConfigProvider : IBitConfigProvider
    {
        public virtual string Version { get; set; } = "V1";

        public virtual BitConfig GetConfiguration(Workspace workspace)
        {
            string solutionFilePath = GetSolutionFilePath(workspace);

            DirectoryInfo directoryInfo = Directory.GetParent(solutionFilePath);

            if (directoryInfo == null)
                throw new InvalidOperationException($"Could not find directory of {solutionFilePath}");

            string bitConfigFileName = $"BitConfig{Version}.json";

            FileInfo bitConfigFileInfo =
                directoryInfo.GetFiles(bitConfigFileName)
                    .ExtendedSingleOrDefault($"Looking for {bitConfigFileName}");

            if (bitConfigFileInfo == null)
                throw new BitConfigNotFoundException($"No {bitConfigFileName} found in {directoryInfo.FullName}");

            BitConfig bitConfig = JsonConvert
                .DeserializeObject<BitConfig>(
                    File.ReadAllText(bitConfigFileInfo.FullName));

            foreach (BitCodeGeneratorMapping mapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
            {
                if (string.IsNullOrEmpty(mapping.DestinationFileName))
                    throw new InvalidOperationException($"{nameof(BitCodeGeneratorMapping.DestinationFileName)} is not provided");

                if (string.IsNullOrEmpty(mapping.TypingsPath))
                    throw new InvalidOperationException($"{nameof(BitCodeGeneratorMapping.TypingsPath)} is not provided");

                if (string.IsNullOrEmpty(mapping.DestinationProject?.Name))
                    throw new InvalidOperationException($"{nameof(BitCodeGeneratorMapping.DestinationProject)} is not provided");

                if (mapping.SourceProjects == null || !mapping.SourceProjects.Any() || !mapping.SourceProjects.All(sp => !string.IsNullOrEmpty(sp?.Name)))
                    throw new InvalidOperationException($"No {nameof(BitCodeGeneratorMapping.SourceProjects)} is not provided");

                if (string.IsNullOrEmpty(mapping.Route))
                    throw new InvalidOperationException($"{nameof(BitCodeGeneratorMapping.Route)} is not provided");
            }

            return bitConfig;
        }

        public virtual string GetSolutionFilePath(Workspace workspace)
        {
            return workspace.CurrentSolution.FilePath;
        }
    }
}
