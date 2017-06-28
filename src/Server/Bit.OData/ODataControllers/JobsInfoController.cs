using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Model.Dtos;

namespace Bit.OData.ODataControllers
{
    public class JobsInfoController : DtoController<JobInfoDto>
    {
        private readonly IBackgroundJobWorker _backgroundJobWorker;

        public JobsInfoController(IBackgroundJobWorker backgroundJobWorker)
        {
            if (backgroundJobWorker == null)
                throw new ArgumentNullException(nameof(backgroundJobWorker));

            _backgroundJobWorker = backgroundJobWorker;
        }

        protected JobsInfoController()
        {

        }

        [Get]
        public virtual async Task<JobInfoDto> Get(string key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            JobInfo jobInfo = await _backgroundJobWorker.GetJobInfoAsync(key, cancellationToken);

            return new JobInfoDto
            {
                Id = jobInfo.Id,
                CreatedAt = jobInfo.CreatedAt,
                State = jobInfo.State
            };
        }
    }
}
