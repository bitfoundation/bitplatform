using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoTemplate.Api.Contracts;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        private readonly IJwtService _jwtService;

        private readonly IMapper _mapper;

        public UserController(UserManager<User> userManager, IJwtService jwtService, IMapper mapper)
        {
            _userManager = userManager;
            _jwtService = jwtService;
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

            if (user is null) return NotFound();

            return Ok(user);
        }

        [HttpPost("[action]"), AllowAnonymous]
        public async Task<ActionResult<UserDto>> Create(UserDto dto, CancellationToken cancellationToken)
        {
            var userToAdd = _mapper.Map<User>(dto);

            var identityResult = await _userManager.CreateAsync(userToAdd, dto.Password);

            if (!identityResult.Succeeded)
            {
                var identityError = identityResult.Errors.First();
                throw new BadHttpRequestException(identityError.Code + identityError.Description);
            }

            return Ok(await Get(userToAdd.Id, cancellationToken));
        }

        [HttpPost("[action]"), AllowAnonymous]
        public async Task<ActionResult<ResponseTokenDto>> Token(RequestTokenDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);

            if (user is null) return NotFound();

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordValid) throw new BadHttpRequestException("Wrong username or password");

            return Ok(await _jwtService.GenerateToken(dto));
        }
    }
}
