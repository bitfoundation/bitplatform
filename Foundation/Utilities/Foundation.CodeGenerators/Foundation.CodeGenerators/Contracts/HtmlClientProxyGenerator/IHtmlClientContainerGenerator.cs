using System.Collections.Generic;
using Foundation.CodeGenerators.Model;

namespace Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator
{
    public interface IHtmlClientContainerGenerator
    {
        string GenerateTypeScriptContainer(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping);

        string GenerateJavaScriptContainer(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping);
    }
}
