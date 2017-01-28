using System.Collections.Generic;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts
{
    public interface IProjectDtoControllersProvider
    {
        IList<DtoController> GetProjectDtoControllersWithTheirOperations(Project project);
    }
}
