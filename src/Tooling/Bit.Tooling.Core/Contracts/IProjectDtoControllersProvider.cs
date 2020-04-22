using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Tooling.Core.Contracts
{
    public interface IProjectDtoControllersProvider
    {
        Task<IList<DtoController>> GetProjectDtoControllersWithTheirOperations(Project project, IList<Project>? allSourceProjects = null);
    }
}
