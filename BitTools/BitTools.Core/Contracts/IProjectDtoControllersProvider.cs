using System.Collections.Generic;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts
{
    public interface IProjectDtoControllersProvider
    {
        Task<IList<DtoController>> GetProjectDtoControllersWithTheirOperations(Project project, IList<Project> allSourceProjects = null);
    }
}
