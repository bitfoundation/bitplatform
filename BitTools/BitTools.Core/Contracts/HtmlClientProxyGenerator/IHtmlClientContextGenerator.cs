using System.Collections.Generic;
using BitTools.Core.Model;

namespace BitTools.Core.Contracts.HtmlClientProxyGenerator
{
    public interface IHtmlClientContextGenerator
    {
        string GenerateTypeScriptContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping);

        string GenerateJavaScriptContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping);
    }
}
