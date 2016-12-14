using System.Collections.Generic;
using Foundation.CodeGenerators.Model;

namespace Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator
{
    public interface IHtmlClientProxyDtosGenerator
    {
        string GenerateTypeScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes, string typingsPath);

        string GenerateJavaScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes);
    }
}