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

        public virtual IList<BitCodeGeneratorMapping> GetBitCodeGeneratorMappings(Solution solution, IList<Project> projects)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            HashSet<BitCodeGeneratorMapping> affectedBitCodeGeneratorMappings = new HashSet<BitCodeGeneratorMapping>();

            BitConfig bitConfig = _configurationProvider.GetConfiguration(solution, projects);

            foreach (Project vsProject in projects)
            {
                bitConfig.BitCodeGeneratorConfigs
                    .BitCodeGeneratorMappings
                    .Where(cm => cm.SourceProjects.Any(sp => sp.Name == vsProject.Name))
                    .ToList()
                    .ForEach((sm) => affectedBitCodeGeneratorMappings.Add(sm));
            }

            return affectedBitCodeGeneratorMappings.ToList();
        }
    }
}
