
namespace TodoTemplate.Shared.Dtos.Account
{
    public class RoleDto
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? NormalizedName { get; set; }

        public string? ConcurrencyStamp { get; set; }
    }
}
