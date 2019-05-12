using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Model.Dtos;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.OData.ODataControllers
{
    public class JobsInfoController : DtoController<JobInfoDto>
    {
        public virtual IBackgroundJobWorker BackgroundJobWorker { get; set; }

        [Get]
        public virtual async Task<SingleResult<JobInfoDto>> Get(string key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (BackgroundJobWorker == null)
                throw new InvalidOperationException("No background job worker is configured");

            JobInfo jobInfo = await BackgroundJobWorker.GetJobInfoAsync(key, cancellationToken);

            return SingleResult(new JobInfoDto
            {
                Id = jobInfo.Id,
                CreatedAt = jobInfo.CreatedAt,
                State = jobInfo.State
            });
        }
    }
}
