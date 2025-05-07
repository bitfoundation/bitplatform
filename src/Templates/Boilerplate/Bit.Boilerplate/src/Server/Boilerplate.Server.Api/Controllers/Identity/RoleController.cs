//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
using Ganss.Xss;
using Humanizer;

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
public partial class RoleController : AppControllerBase, IRoleController
{
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private HtmlSanitizer htmlSanitizer = default!;


    [HttpGet]
    public async Task<List<RoleDto>> GetAllRoles(CancellationToken cancellationToken)
    {
        return await DbContext.Roles.Where(r => r.Name != AppBuiltInRoles.BasicUser && r.Name != AppBuiltInRoles.SuperAdmin).Project().ToListAsync(cancellationToken);
    }

    [HttpGet]
    public async Task<List<UserDto>> GetAllUsers(CancellationToken cancellationToken)
    {
        return await DbContext.Users.Project().ToListAsync(cancellationToken);
    }

    [HttpGet]
    public async Task<List<RoleClaimDto>> GetClaims(Guid roleId, CancellationToken cancellationToken)
    {
        return await DbContext.RoleClaims.Project().Where(rc => rc.RoleId == roleId).ToListAsync(cancellationToken);
    }

    [HttpGet]
    public async Task<List<UserDto>> GetUsers(RoleDto roleDto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleDto.Name))
            throw new BadRequestException();

        var userId = User.GetUserId();
        HttpContext.RequestAborted = cancellationToken;
        var users = await userManager.GetUsersInRoleAsync(roleDto.Name);

        return [.. users.Select(u => u.Map())];
    }

    [HttpPost]
    public async Task<RoleDto> Create(RoleDto roleDto, CancellationToken cancellationToken)
    {
        roleDto.Name = htmlSanitizer.Sanitize(roleDto.Name ?? string.Empty);

        var entityToAdd = roleDto.Map();

        await DbContext.Roles.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToAdd.Map();
    }

    [HttpPost]
    public async Task<RoleDto> Update(RoleDto roleDto, CancellationToken cancellationToken)
    {
        roleDto.Name = htmlSanitizer.Sanitize(roleDto.Name ?? string.Empty);

        var entityToUpdate = await DbContext.Roles.FindAsync([roleDto.Id], cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        roleDto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpPost]
    public async Task SaveMaxPrivilegedSessions(CancellationToken cancellationToken)
    {

    }
}
