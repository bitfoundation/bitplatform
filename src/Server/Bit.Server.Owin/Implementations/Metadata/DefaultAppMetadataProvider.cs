using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Models;
using Bit.Owin.Contracts.Metadata;

namespace Bit.Owin.Implementations.Metadata
{
    public class DefaultAppMetadataProvider : IAppMetadataProvider
    {
        private AppMetadata? _appMetadata;

        public virtual IEnumerable<IMetadataBuilder> MetadataBuilders { get; set; } = default!;

        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual async Task<AppMetadata> GetAppMetadata()
        {
            if (_appMetadata == null)
            {
                List<ObjectMetadata> allMetadata = new List<ObjectMetadata>();

                foreach (IMetadataBuilder metadataBuilder in MetadataBuilders)
                {
                    allMetadata.AddRange(await metadataBuilder.BuildMetadata().ConfigureAwait(false));
                }

                _appMetadata = new AppMetadata
                {
                    Messages = AppEnvironment.Cultures.ToList(),
                    Views = allMetadata.OfType<ViewMetadata>().ToList(),
                    Dtos = allMetadata.OfType<DtoMetadata>().ToList(),
                    Projects = allMetadata.OfType<ProjectMetadata>().ToList()
                };
            }

            return _appMetadata;
        }
    }
}
