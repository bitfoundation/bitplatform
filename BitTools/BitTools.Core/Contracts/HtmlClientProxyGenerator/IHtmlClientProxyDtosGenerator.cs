using System.Collections.Generic;
using BitTools.Core.Model;

namespace BitTools.Core.Contracts.HtmlClientProxyGenerator
{
    public interface IHtmlClientProxyDtosGenerator
    {
        string GenerateTypeScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes, string typingsPath);

        string GenerateJavaScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes);
    }
}