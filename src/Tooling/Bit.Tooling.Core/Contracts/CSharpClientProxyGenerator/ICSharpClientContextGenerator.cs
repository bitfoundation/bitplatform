using BitTools.Core.Model;
using System.Collections.Generic;

namespace BitTools.Core.Contracts.CSharpClientProxyGenerator
{
    public interface ICSharpClientContextGenerator
    {
        string GenerateCSharpContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping);
    }
}
