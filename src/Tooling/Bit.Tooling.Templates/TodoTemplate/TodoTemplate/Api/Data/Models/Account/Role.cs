using Microsoft.AspNetCore.Identity;

namespace TodoTemplate.Api.Data.Models.Account
{
    public class Role : IdentityRole<int>
    {
        public override int Id { get; set; }
        public override string Name { get; set; }
        public override string NormalizedName { get; set; }
        public override string ConcurrencyStamp { get; set; }
    }
}
