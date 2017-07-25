using Bit.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Core.Implementations
{
    public class DefaultScopeStatusManager : IScopeStatusManager
    {
        private bool _isFailed;

        public virtual bool WasSucceeded()
        {
            return _isFailed == false;
        }

        public virtual void MarkAsFailed()
        {
            _isFailed = true;
        }

        public virtual void MarkAsSucceeded()
        {
            _isFailed = false;
        }
    }
}
