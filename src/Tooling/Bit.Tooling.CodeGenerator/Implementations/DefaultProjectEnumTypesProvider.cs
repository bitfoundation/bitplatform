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

            IList<EnumType> enumTypes = new List<EnumType>();

            List<DtoController> dtoControllers = (await _projectDtoControllersProvider
                .GetProjectDtoControllersWithTheirOperations(project)).ToList();

            IList<Dto> dtos = await _dtosProvider.GetProjectDtos(project, allSourceProjects);

            List<Compilation> sourceProjectsCompilations = new List<Compilation> { };

            foreach (var p in allSourceProjects)
            {
                sourceProjectsCompilations.Add(await p.GetCompilationAsync());
            }

            dtos.SelectMany(d => d.Properties)
                .Where(p => p.Type.IsEnum())
                .Select(p => p.Type.GetUnderlyingTypeSymbol())
                .Union(dtoControllers.SelectMany(dtoController => dtoController.Operations.SelectMany(operation => operation.Parameters.Select(p => p.Type).Union(new[] { operation.ReturnType }))).Where(t => t.IsEnum()).Select(t => t.GetUnderlyingComplexType()))
                .Where(enumType => sourceProjectsCompilations.Any(c => c.Assembly.TypeNames.Any(tName => tName == enumType.Name)))
                .Distinct()
                .ToList()
                .ForEach(enumType =>
                {
                    enumTypes.Add(new EnumType
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
                });

            return enumTypes;
        }
    }
}
