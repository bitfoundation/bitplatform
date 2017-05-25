using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts.Metadata;

namespace Bit.Owin.Implementations.Metadata
{
    public class DefaultAppMetadataProvider : IAppMetadataProvider
    {
        private readonly IEnumerable<IMetadataBuilder> _metadataBuilders;

        private AppMetadata _appMetadata;
        private readonly IAppEnvironmentProvider _appEnvironmentProvider;

        protected DefaultAppMetadataProvider()
        {

        }

        public DefaultAppMetadataProvider(IEnumerable<IMetadataBuilder> metadataBuilders, IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (metadataBuilders == null)
                throw new ArgumentNullException(nameof(metadataBuilders));

            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _metadataBuilders = metadataBuilders;
            _appEnvironmentProvider = appEnvironmentProvider;
        }

        public virtual async Task<AppMetadata> GetAppMetadata()
        {
            if (_appMetadata == null)
            {
                List<ObjectMetadata> allMetadata = new List<ObjectMetadata>();

                foreach (IMetadataBuilder metadataBuilder in _metadataBuilders)
                {
                    allMetadata.AddRange(await metadataBuilder.BuildMetadata());
                }

                AppEnvironment activeAppEnvironment = _appEnvironmentProvider.GetActiveAppEnvironment();

                _appMetadata = new AppMetadata
                {
                    Messages = activeAppEnvironment.Cultures.ToList(),
                    Views = allMetadata.OfType<ViewMetadata>().ToList(),
                    Dtos = allMetadata.OfType<DtoMetadata>().ToList(),
                    Projects = allMetadata.OfType<ProjectMetadata>().ToList()
                };
            }

            return _appMetadata;
        }
    }
}
