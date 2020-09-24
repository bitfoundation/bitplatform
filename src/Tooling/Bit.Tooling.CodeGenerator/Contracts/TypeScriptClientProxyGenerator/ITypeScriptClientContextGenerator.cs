using Bit.Tooling.Core.Model;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Contracts.TypeScriptClientProxyGenerator
{
    public interface ITypeScriptClientContextGenerator
    {
        string GenerateTypeScriptContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping);

        string GenerateJavaScriptContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping);
    }
}
