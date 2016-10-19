using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Foundation.CodeGenerators.Contracts;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Foundation.CodeGenerators.Implementations
{
    public class DefaultProjectDtosProvider : IProjectDtosProvider
    {
        private readonly IProjectDtoControllersProvider _projectDtoControllersProvider;

        public DefaultProjectDtosProvider(IProjectDtoControllersProvider projectDtoControllersProvider)
        {
            if (projectDtoControllersProvider == null)
                throw new ArgumentNullException(nameof(projectDtoControllersProvider));

            _projectDtoControllersProvider = projectDtoControllersProvider;
        }

        public virtual IList<Dto> GetProjectDtos(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            IList<Dto> dtos = _projectDtoControllersProvider
                .GetProjectDtoControllersWithTheirOperations(project)
                .Select(dtoController => new Dto
                {
                    DtoSymbol = (INamedTypeSymbol)dtoController.ModelSymbol,
                    Properties = dtoController.ModelSymbol.GetMembers().OfType<IPropertySymbol>().ToList()
                })
                .ToList();

            return dtos;
        }
    }
}
