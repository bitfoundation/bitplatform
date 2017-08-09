using System;
using System.Collections.Generic;
using System.Linq;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;

namespace BitCodeGenerator.Implementations
{
    public class DefaultBitCodeGeneratorMappingsProvider : IBitCodeGeneratorMappingsProvider
    {
        private readonly IBitConfigProvider _configurationProvider;

        public DefaultBitCodeGeneratorMappingsProvider(IBitConfigProvider configurationProvider)
        {
            if (configurationProvider == null)
                throw new ArgumentNullException(nameof(configurationProvider));

            _configurationProvider = configurationProvider;
        }

        public virtual IList<BitCodeGeneratorMapping> GetBitCodeGeneratorMappings(Workspace workspace, string solutionFilePath, IList<string> projectNames)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (projectNames == null)
                throw new ArgumentNullException(nameof(projectNames));

            HashSet<BitCodeGeneratorMapping> affectedBitCodeGeneratorMappings = new HashSet<BitCodeGeneratorMapping>();

            BitConfig bitConfig = _configurationProvider.GetConfiguration(solutionFilePath);

            foreach (string projName in projectNames)
            {
                bitConfig.BitCodeGeneratorConfigs
                    .BitCodeGeneratorMappings
                    .Where(cm => cm.SourceProjects.Any(sp => sp.Name == projName))
                    .ToList()
                    .ForEach((sm) => affectedBitCodeGeneratorMappings.Add(sm));
            }

            return affectedBitCodeGeneratorMappings.ToList();
        }
    }
}
