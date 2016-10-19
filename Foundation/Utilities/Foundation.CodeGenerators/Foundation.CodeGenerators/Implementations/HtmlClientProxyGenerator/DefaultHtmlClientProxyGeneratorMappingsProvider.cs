using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.CodeGenerators.Contracts;
using Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using System.Collections.ObjectModel;

namespace Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientProxyGeneratorMappingsProvider : IHtmlClientProxyGeneratorMappingsProvider
    {
        private readonly IFoundationVSPackageConfigurationProvider _configurationProvider;

        public DefaultHtmlClientProxyGeneratorMappingsProvider(IFoundationVSPackageConfigurationProvider configurationProvider)
        {
            if (configurationProvider == null)
                throw new ArgumentNullException(nameof(configurationProvider));

            _configurationProvider = configurationProvider;
        }

        public virtual IList<HtmlClientProxyGeneratorMapping> GetHtmlClientProxyGeneratorMappings(Workspace worksapce, Solution solution, IList<Project> projects)
        {
            if (worksapce == null)
                throw new ArgumentNullException(nameof(worksapce));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            HashSet<HtmlClientProxyGeneratorMapping> affectedClientProxyGeneratorMappings = new HashSet<HtmlClientProxyGeneratorMapping>();

            FoundationVSPackageConfiguration foundationVsPackageConfiguration = _configurationProvider.GetConfiguration(worksapce, solution, projects);

            foreach (Project vsProject in projects)
            {
                foundationVsPackageConfiguration.HtmlClientProxyGeneratorConfiguration
                    .HtmlClientProxyGeneratorMappings
                    .Where(cm => cm.SourceProjects.Any(sp => sp.Name == vsProject.Name))
                    .ToList()
                    .ForEach((sm) => affectedClientProxyGeneratorMappings.Add(sm));
            }

            return affectedClientProxyGeneratorMappings.ToList();
        }
    }
}
