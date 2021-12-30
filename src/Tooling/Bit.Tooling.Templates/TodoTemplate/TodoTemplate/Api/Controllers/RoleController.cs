using System.Threading;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Api.Data.Repositories.Contracts;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRepository<Role> _roleRepository;

        private readonly IMapper _mapper;

        public RoleController(IRepository<Role> roleRepository, IMapper mapper)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<List<RoleDto>> Get(CancellationToken cancellationToken)
        {
            var roles = await _roleRepository.TableNoTracking.ToListAsync(cancellationToken);

            var mappedRoles = _mapper.Map<List<Role>, List<RoleDto>>(roles);

            return mappedRoles;
        }

        [HttpGet("{id:int}")]
        public async Task<RoleDto> Get(int id, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(cancellationToken,id);

            var mappedRole = _mapper.Map<Role, RoleDto>(role);

            return mappedRole;
        }

        [HttpPost]
        public virtual async Task<RoleDto> Create(RoleDto dto, CancellationToken cancellationToken)
        {
            var roleToAdd = _mapper.Map<RoleDto, Role>(dto);

            await _roleRepository.AddAsync(roleToAdd, cancellationToken);

            var role =  await _roleRepository.GetByIdAsync(cancellationToken, roleToAdd.Id);

            var mappedRole = _mapper.Map<Role, RoleDto>(role);

            return mappedRole;
        }

        [HttpPut]
        public virtual async Task<RoleDto> Update(RoleDto dto, CancellationToken cancellationToken)
        {
            var roleToUpdate = await _roleRepository.GetByIdAsync(cancellationToken, dto.Id);

            var updatedRole = _mapper.Map(dto, roleToUpdate);

            await _roleRepository.UpdateAsync(updatedRole, cancellationToken);

            var role = await _roleRepository.GetByIdAsync(cancellationToken, updatedRole.Id);

            var mappedRole = _mapper.Map<Role, RoleDto>(role);

            return mappedRole;
        }

        [HttpDelete("{id:int}")]
        public virtual async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var roleToDelete = await _roleRepository.GetByIdAsync(cancellationToken, id);

            await _roleRepository.DeleteAsync(roleToDelete, cancellationToken);

            return Ok();
        }
    }
}
