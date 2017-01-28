using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

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

        public virtual BitConfig GetConfiguration(Workspace workspace, Solution solution, IList<Project> projects)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            DirectoryInfo directoryInfo = new FileInfo(solution.FilePath).Directory;

            if (directoryInfo == null)
                throw new InvalidOperationException($"Could not find directory of {solution.FilePath}");

            string bitConfigFileName = $"BitConfig{Version}.json";

            FileInfo bitConfigFileInfo =
                directoryInfo?.GetFiles(bitConfigFileName)
                    .SingleOrDefault();

            if (bitConfigFileInfo == null)
                throw new BitConfigNotFoundException($"No {bitConfigFileName} found in {directoryInfo.FullName}");

            BitConfig bitConfig = JsonConvert
                .DeserializeObject<BitConfig>(
                    File.ReadAllText(bitConfigFileInfo.FullName));

            foreach (BitCodeGeneratorMapping mapping in bitConfig.BitCodeGeneratorConfigs.BitCodeGeneratorMappings)
            {
                if (string.IsNullOrEmpty(mapping.DestinationFileName))
                    throw new InvalidOperationException("DestinationFileName is not provided");

                if (string.IsNullOrEmpty(mapping.TypingsPath))
                    throw new InvalidOperationException("TypingsPath is not provided");

                if (string.IsNullOrEmpty(mapping?.DestinationProject?.Name))
                    throw new InvalidOperationException("DestinationProject is not provided");

                if (mapping.SourceProjects == null || !mapping.SourceProjects.Any() || !mapping.SourceProjects.All(sp => !string.IsNullOrEmpty(sp?.Name)))
                    throw new InvalidOperationException("No | Bad Source Projects is not provided");

                if (string.IsNullOrEmpty(mapping.Namespace))
                    throw new InvalidOperationException("Namespace is not provided");

                if (string.IsNullOrEmpty(mapping.EdmName))
                    throw new InvalidOperationException("EdmName is not provided");
            }

            return bitConfig;
        }
    }
}
