using Bit.Tooling.Core.Contracts;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeGenerator.Implementations
{
    public class DefaultProjectEnumTypesProvider : IProjectEnumTypesProvider
    {
        private readonly IProjectDtosProvider _dtosProvider;
        private readonly IProjectDtoControllersProvider _projectDtoControllersProvider;

        public DefaultProjectEnumTypesProvider(IProjectDtoControllersProvider projectDtoControllersProvider, IProjectDtosProvider dtosProvider)
        {
            if (projectDtoControllersProvider == null)
                throw new ArgumentNullException(nameof(projectDtoControllersProvider));

            if (dtosProvider == null)
                throw new ArgumentNullException(nameof(dtosProvider));

            _projectDtoControllersProvider = projectDtoControllersProvider;
            _dtosProvider = dtosProvider;
        }

        public async Task<IList<EnumType>> GetProjectEnumTypes(Project project, IList<Project>? allSourceProjects = null)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (allSourceProjects == null)
                allSourceProjects = new List<Project> { project };

            List<DtoController> dtoControllers = (await _projectDtoControllersProvider
                .GetProjectDtoControllersWithTheirOperations(project).ConfigureAwait(false)).ToList();

            IList<Dto> dtos = await _dtosProvider.GetProjectDtos(project, allSourceProjects).ConfigureAwait(false);

            HashSet<EnumType> enums = new HashSet<EnumType>();

            foreach (var enumType in dtoControllers.SelectMany(dtoController => dtoController.Operations.SelectMany(operation => operation.Parameters.Select(p => p.Type).Union(new[] { operation.ReturnType })))
                .Union(dtos.SelectMany(d => d.Properties.Select(p => p.Type)))
                .Select(x => x.GetUnderlyingTypeSymbol())
                .Select(x => x.IsCollectionType() || x.IsQueryableType() ? x.GetElementType() : x)
                .Where(t => t.IsEnum()))
            {
                if (enums.Any(e => SymbolEqualityComparer.Default.Equals(e.EnumTypeSymbol, enumType)))
                    continue;

                enums.Add(new EnumType
                {
                    EnumTypeSymbol = enumType,
                    Members = enumType.GetMembers().OfType<IFieldSymbol>().Select((m, i) => new EnumMember
                    {
                        Name = m.Name,
                        Symbol = m,
                        Index = i,
                        Value = Convert.ToInt32(m.ConstantValue, CultureInfo.InvariantCulture)
                    }).ToList()
                });
            }

            return enums
                .ToList();
        }
    }
}
