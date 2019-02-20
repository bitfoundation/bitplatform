using Bit.Core.Models;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations.Metadata;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Owin.Metadata
{
    public class BitMetadataBuilder : DefaultProjectMetadataBuilder
    {
        public static readonly string UnknownError = nameof(UnknownError);

        public static readonly string ResourceNotFoundException = nameof(ResourceNotFoundException);

        public static readonly string DomainLogicException = nameof(DomainLogicException);

        public static readonly string KnownError = nameof(KnownError);

        public static readonly string JobNotFound = nameof(JobNotFound);

        public static readonly string BadRequestException = nameof(BadRequestException);

        public override Task<IEnumerable<ObjectMetadata>> BuildMetadata()
        {
            AddProjectMetadata(new ProjectMetadata
            {
                ProjectName = "Bit",
                Messages = new List<EnvironmentCulture>
                {
                    new EnvironmentCulture
                    {
                        Name = "FaIr",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = UnknownError,Title = "خطای نامشخص" },
                            new EnvironmentCultureValue { Name = ResourceNotFoundException,Title = "موجودیت مورد نظر یافت نشد"},
                            new EnvironmentCultureValue { Name = DomainLogicException,Title = "خطای منطق" },
                            new EnvironmentCultureValue { Name = KnownError,Title = "خطای مشخص" },
                            new EnvironmentCultureValue { Name = JobNotFound,Title = "کار مورد نظر پیدا نشد" },
                            new EnvironmentCultureValue { Name = BadRequestException,Title = "درخواست نامعتبر" }
                        }
                    },
                    new EnvironmentCulture
                    {
                        Name = "EnUs",
                        Values = new List<EnvironmentCultureValue>
                        {
                            new EnvironmentCultureValue { Name = UnknownError,Title = "Unknown error" },
                            new EnvironmentCultureValue { Name = ResourceNotFoundException,Title = "Resource not found"},
                            new EnvironmentCultureValue { Name = DomainLogicException,Title = "Domain logic error" },
                            new EnvironmentCultureValue { Name = KnownError,Title = "Known error" },
                            new EnvironmentCultureValue { Name = JobNotFound,Title = "Job not found" },
                            new EnvironmentCultureValue { Name = BadRequestException,Title = "Invalid request" }
                        }
                    }
                }
            });

            return base.BuildMetadata();
        }
    }
}
