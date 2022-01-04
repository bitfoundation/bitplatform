using System.Threading;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<RoleDto> Get(int id, CancellationToken cancellationToken)
        {
            return await Get(cancellationToken).FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
        }

        [HttpPost]
        public async Task<RoleDto> Create(RoleDto dto, CancellationToken cancellationToken)
        {
            var roleToAdd = _mapper.Map<Role>(dto);

            await _context.AddAsync(roleToAdd, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return await Get(roleToAdd.Id, cancellationToken);
        }

        [HttpPut]
        public async Task<RoleDto> Update(RoleDto dto, CancellationToken cancellationToken)
        {
            var roleToUpdate = await _context.Roles.FirstOrDefaultAsync(role => role.Id == dto.Id, cancellationToken);

            var updatedRole = _mapper.Map(dto, roleToUpdate);

            _context.Update(updatedRole);

            await _context.SaveChangesAsync(cancellationToken);

            return await Get(updatedRole.Id, cancellationToken);
        }

        [HttpDelete("{id:int}")]
        public async void Delete(int id,CancellationToken cancellationToken)
        {
            var roleToDelete = await _context.Roles.FirstOrDefaultAsync(role => role.Id == id, cancellationToken);

            _context.Remove(roleToDelete);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
