using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoTemplate.Shared.Enums;

namespace TodoTemplate.Shared.Dtos.Account
{
    public class UserDto
    {
        public int Id { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }

        public string? FullName { get; set; }

        public Gender? Gender { get; set; }

        public DateTimeOffset? BirthDate { get; set; }
    }
}
