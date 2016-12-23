using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation.CodeGenerators.Contracts;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace Foundation.CodeGenerators.Implementations
{
    public class DefaultFoundationVSPackageConfigurationProvider : IFoundationVSPackageConfigurationProvider
    {
        public virtual FoundationVSPackageConfiguration GetConfiguration(Workspace workspace, Solution solution, IList<Project> projects)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            DirectoryInfo directoryInfo = new FileInfo(solution.FilePath).Directory;

            if(directoryInfo == null)
                throw new InvalidOperationException($"Could not find directory of {solution.FilePath}");

            FileInfo foundationVsPackageConfigurationFileInfo =
                directoryInfo?.GetFiles("FoundationVSPackageConfiguration.json")
                    .SingleOrDefault();

            if (foundationVsPackageConfigurationFileInfo == null)
                throw new InvalidOperationException($"No FoundationVSPackageConfiguration.json found in {directoryInfo.FullName}");

            FoundationVSPackageConfiguration foundationVsPackageConfiguration = JsonConvert
                .DeserializeObject<FoundationVSPackageConfiguration>(
                    File.ReadAllText(foundationVsPackageConfigurationFileInfo.FullName));

            foreach (HtmlClientProxyGeneratorMapping mapping in foundationVsPackageConfiguration.HtmlClientProxyGeneratorConfiguration.HtmlClientProxyGeneratorMappings)
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

            return foundationVsPackageConfiguration;
        }
    }
}
