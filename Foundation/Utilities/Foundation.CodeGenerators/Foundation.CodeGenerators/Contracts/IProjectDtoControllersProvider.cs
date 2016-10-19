using System.Collections.Generic;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Contracts
{
    public interface IProjectDtoControllersProvider
    {
        IList<DtoController> GetProjectDtoControllersWithTheirOperations(Project project);
    }
}
