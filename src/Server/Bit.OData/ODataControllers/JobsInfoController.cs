using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Model.Dtos;

namespace Bit.OData.ODataControllers
{
    public class JobsInfoController : DtoController<JobInfoDto>
    {
        public virtual IBackgroundJobWorker BackgroundJobWorker { get; set; }

        [Get]
        public virtual async Task<JobInfoDto> Get(string key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            JobInfo jobInfo = await BackgroundJobWorker.GetJobInfoAsync(key, cancellationToken);

            return new JobInfoDto
            {
                Id = jobInfo.Id,
                CreatedAt = jobInfo.CreatedAt,
                State = jobInfo.State
            };
        }
    }
}
