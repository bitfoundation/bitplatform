using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts
{
    public interface IProjectDtoRulesProvider
    {
        Task<IList<DtoRules>> GetProjectAllDtoRules(Project project, IList<Project>? allSourceProjects = null);
    }
}
