using System;
using System.Threading.Tasks;
using Foundation.Core.Contracts;
using Foundation.Core.Models;

namespace Foundation.Test.Api.Implementations
{
    public class TestLogStore : ILogStore
    {
        private readonly IContentFormatter _contentFormatter;

        protected TestLogStore()
        {
        }

        public TestLogStore(IContentFormatter contentFormatter)
        {
            if (contentFormatter == null)
                throw new ArgumentNullException(nameof(contentFormatter));

            _contentFormatter = contentFormatter;
        }

        public virtual Task SaveLogAsync(LogEntry logEntry)
        {
            string logAsJson = _contentFormatter.Serialize(logEntry);

            return Task.CompletedTask;
        }

        public virtual void SaveLog(LogEntry logEntry)
        {
            string logAsJson = _contentFormatter.Serialize(logEntry);
        }
    }
}
