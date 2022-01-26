using Microsoft.AspNetCore.Authorization;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]

public class RoleController : ControllerBase
{
    private readonly TodoTemplateDbContext _dbContext;

    private readonly IMapper _mapper;

    public RoleController(TodoTemplateDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    [HttpGet]
    public IQueryable<RoleDto> Get(CancellationToken cancellationToken)
    {
        return _dbContext.Roles.ProjectTo<RoleDto>(_mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> Get(int id, CancellationToken cancellationToken)
    {
        var role = await Get(cancellationToken).FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
        if (role is null)
            return NotFound();
        return Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(RoleDto dto, CancellationToken cancellationToken)
    {
        var roleToAdd = _mapper.Map<Role>(dto);

        await _dbContext.AddAsync(roleToAdd, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await Get(roleToAdd.Id, cancellationToken);
    }

    [HttpPut]
    public async Task<ActionResult<RoleDto>> Update(RoleDto dto, CancellationToken cancellationToken)
    {
        var roleToUpdate = await _dbContext.Roles.FirstOrDefaultAsync(role => role.Id == dto.Id, cancellationToken);

        if (roleToUpdate is null)
            return NotFound();

        var updatedRole = _mapper.Map(dto, roleToUpdate);

        _dbContext.Update(updatedRole);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await Get(updatedRole.Id, cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async void Delete(int id, CancellationToken cancellationToken)
    {
        _dbContext.Remove(new Role { Id = id });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
