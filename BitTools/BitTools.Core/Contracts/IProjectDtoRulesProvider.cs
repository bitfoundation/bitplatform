using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace BitTools.Core.Contracts
{
    public interface IProjectDtoRulesProvider
    {
        IList<DtoRules> GetProjectAllDtoRules(Project project);
    }
}
