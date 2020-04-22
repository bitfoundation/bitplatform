using Bit.Tooling.Core.Model;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator
{
    public interface ICSharpClientContextGenerator
    {
        string GenerateCSharpContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping);
    }
}
