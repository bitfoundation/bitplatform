using System.ComponentModel.DataAnnotations;

namespace TodoTemplate.Shared.Dtos.Account
{
    public class RequestTokenDto
    {
        [Required]
        public string? UserName { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}
