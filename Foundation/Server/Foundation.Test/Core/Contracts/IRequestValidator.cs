using System;

namespace Foundation.Test.Core.Contracts
{
    public interface IRequestValidator
    {
        void ValidateRequestByUri(Uri requestUri);
    }
}
