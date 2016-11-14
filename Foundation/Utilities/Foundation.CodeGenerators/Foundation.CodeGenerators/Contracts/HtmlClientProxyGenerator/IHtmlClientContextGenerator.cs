using System.Collections.Generic;
using Foundation.CodeGenerators.Model;

namespace Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator
{
    public interface IHtmlClientContextGenerator
    {
        string GenerateTypeScriptContext(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping);

        string GenerateJavaScriptContext(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping);
    }
}
