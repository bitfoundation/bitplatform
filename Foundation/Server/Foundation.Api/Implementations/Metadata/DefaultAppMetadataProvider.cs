using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Api.Contracts.Metadata;
using Foundation.Core.Contracts;
using Foundation.Core.Models;

namespace Foundation.Api.Implementations.Metadata
{
    public class DefaultAppMetadataProvider : IAppMetadataProvider
    {
        private readonly IEnumerable<IMetadataBuilder> _metadataBuilders;

        private AppMetadata _appMetadata;
        private IAppEnvironmentProvider _appEnvironmentProvider;

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

        public virtual AppMetadata GetAppMetadata()
        {
            if (_appMetadata == null)
            {
                List<ObjectMetadata> allMetadata = _metadataBuilders
                    .SelectMany(mb => mb.BuildMetadata())
                    .ToList();

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
