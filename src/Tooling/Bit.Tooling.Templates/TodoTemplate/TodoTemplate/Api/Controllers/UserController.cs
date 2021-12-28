using Microsoft.AspNetCore.Mvc;
using TodoTemplate.Api.Repository.Contracts;
using TodoTemplate.Shared.Models.Account;

namespace TodoTemplate.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        public IUserRepository UserRepository { get; set; }

        public UserController(IUserRepository userRepository)
        {
            UserRepository = userRepository;
        }

        [HttpGet]
        public IEnumerable<User> GetAll()
        {
            return UserRepository.GetAll();
        }
    }
}

