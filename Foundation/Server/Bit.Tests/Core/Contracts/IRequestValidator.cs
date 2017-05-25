using System;

namespace Bit.Tests.Core.Contracts
{
    public interface IRequestValidator
    {
        void ValidateRequestByUri(Uri requestUri);
    }
}
