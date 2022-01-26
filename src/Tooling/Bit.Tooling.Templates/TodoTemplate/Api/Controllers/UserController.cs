using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        private readonly IMapper _mapper;

        public UserController(UserManager<User> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public IQueryable<UserDto> Get(CancellationToken cancellationToken)
        {
            return _userManager.Users.ProjectTo<UserDto>(_mapper.ConfigurationProvider, cancellationToken);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> Get(int id, CancellationToken cancellationToken)
        {
            var user = await Get(cancellationToken).FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
            if (user is null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult<UserDto>> Create(UserDto dto, CancellationToken cancellationToken)
        {
            var userToAdd = _mapper.Map<User>(dto);

            await _userManager.CreateAsync(userToAdd);

            return await Get(userToAdd.Id, cancellationToken);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult<ResponseTokenDto>> Token(RequestTokenDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Email);

            if (user is null) return NotFound();

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordValid) throw new BadHttpRequestException("Wrong username or password");

            var claims = await _userManager.GetClaimsAsync(user);

            var securityToken = new JwtSecurityTokenHandler()
                .CreateJwtSecurityToken(new SecurityTokenDescriptor
                {
                    Expires = DateTime.Now.AddDays(10),
                    Subject = new ClaimsIdentity(claims)
                });

            return Ok(new ResponseTokenDto { Token = new JwtSecurityTokenHandler().WriteToken(securityToken) });
        }
    }
}
