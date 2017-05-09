using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData;
using Foundation.Api.Contracts;
using Foundation.Model.DomainModels;

namespace Foundation.Api.ApiControllers
{
    public class JobsInfoController : DtoController<JobInfo>
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
        public virtual async Task<JobInfo> Get(string key, CancellationToken cancellationToken)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return await _backgroundJobWorker.GetJobInfoAsync(key, cancellationToken);
        }
    }
}
