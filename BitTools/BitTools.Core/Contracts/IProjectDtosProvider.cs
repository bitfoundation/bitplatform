using System.Collections.Generic;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts
{
    public interface IProjectDtosProvider
    {
        Task<IList<Dto>> GetProjectDtos(Project project, IList<Project> allSourceProjects = null);
    }
}
