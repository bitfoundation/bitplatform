using System.Threading;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using TodoTemplate.Api.Data.Context;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly TodoTemplateDbContext _context;

        private readonly IMapper _mapper;

        public RoleController(TodoTemplateDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public IQueryable<RoleDto> Get(CancellationToken cancellationToken)
        {
            return _context.Roles.ProjectTo<RoleDto>(_mapper.ConfigurationProvider, cancellationToken);
        }

        [HttpGet("{id:int}")]
        public RoleDto Get(int id, CancellationToken cancellationToken)
        {
            return Get(cancellationToken).FirstOrDefault(role => role.Id == id);
        }

        [HttpPost]
        public RoleDto Create(RoleDto dto, CancellationToken cancellationToken)
        {
            var roleToAdd = _mapper.Map<Role>(dto);

            _context.Add(roleToAdd);

            _context.SaveChanges();

            return Get(roleToAdd.Id, cancellationToken);
        }

        [HttpPut]
        public RoleDto Update(RoleDto dto, CancellationToken cancellationToken)
        {
            var roleToUpdate = _context.Roles.FirstOrDefault(role => role.Id == dto.Id);

            var updatedRole = _mapper.Map(dto, roleToUpdate);

            _context.Update(updatedRole);

            _context.SaveChanges();

            return Get(updatedRole.Id, cancellationToken);
        }

        [HttpDelete("{id:int}")]
        public void Delete(int id)
        {
            var roleToDelete = _context.Roles.FirstOrDefault(role => role.Id == id);

            _context.Remove(roleToDelete);

            _context.SaveChanges();
        }
    }
}
