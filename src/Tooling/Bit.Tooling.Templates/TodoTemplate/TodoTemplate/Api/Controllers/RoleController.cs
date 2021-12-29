using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TodoTemplate.Api.Repository.Contracts;
using TodoTemplate.Shared.Models.Account;

namespace TodoTemplate.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;

        public RoleController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(await _roleRepository.GetAllRole());
            }
            catch(Exception exception)
            {
                return BadRequest(exception);
            }

        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            try
            {
                return Ok(await _roleRepository.GetByIdRole(id));
            }
            catch (Exception exception)
            {
                return BadRequest(exception);
            }
        }

        [HttpPost]
        public IActionResult Add([FromBody] Role role)
        {
            try
            {
                _roleRepository.AddRole(role);

                return Ok();
            }
            catch (Exception exception)
            {
                return BadRequest(exception);
            }

        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Role role)
        {
            try
            {
                var toUpdateRole = await _roleRepository.GetByIdRole(role.Id);

                toUpdateRole.Name = role.Name;

                _roleRepository.UpdateRole(toUpdateRole);

                return Ok();
            }
            catch (Exception exception)
            {
                return BadRequest(exception);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                var toDeleteRole = await _roleRepository.GetByIdRole(id);

                _roleRepository.DeleteRole(toDeleteRole);

                return Ok();
            }
            catch (Exception exception)
            {
                return BadRequest(exception);
            }
        }
    }
}
