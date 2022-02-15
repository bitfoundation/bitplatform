using TodoTemplate.Api.Models.Account;
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

    [HttpGet, EnableQuery]
    public IQueryable<RoleDto> Get(CancellationToken cancellationToken)
    {
        return _dbContext.Roles.ProjectTo<RoleDto>(_mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<RoleDto> Get(int id, CancellationToken cancellationToken)
    {
        var role = await Get(cancellationToken).FirstOrDefaultAsync(role => role.Id == id, cancellationToken);

        if (role is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.RoleCouldNotBeFound));

        return role;
    }

    [HttpPost]
    public async Task Post(RoleDto dto, CancellationToken cancellationToken)
    {
        var roleToAdd = _mapper.Map<Role>(dto);

        await _dbContext.AddAsync(roleToAdd, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task Put(RoleDto dto, CancellationToken cancellationToken)
    {
        var roleToUpdate = await _dbContext.Roles.FirstOrDefaultAsync(role => role.Id == dto.Id, cancellationToken);

        if (roleToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.RoleCouldNotBeFound));

        var updatedRole = _mapper.Map(dto, roleToUpdate);

        _dbContext.Update(updatedRole);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        _dbContext.Remove(new Role { Id = id });

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.RoleCouldNotBeFound));
    }
}
