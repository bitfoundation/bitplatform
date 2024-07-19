namespace Boilerplate.Client.Core.Components;

public class UserClaims(IEnumerable<Claim> claims)
{
    public string? NameId { get; set; } = claims.SingleOrDefault(c => c.Type == "name_id")?.Value;
    public string? UniqueName { get; set; } = claims.SingleOrDefault(c => c.Type == "unique_name")?.Value;
    public string? Email { get; set; } = claims.SingleOrDefault(c => c.Type == "email")?.Value;
    public string? SecurityStamp { get; set; } = claims.SingleOrDefault(c => c.Type == "AspNet.Identity.SecurityStamp")?.Value;
    public string? SessionId { get; set; } = claims.SingleOrDefault(c => c.Type == "session-id")?.Value;
    public string? Amr { get; set; } = claims.SingleOrDefault(c => c.Type == "amr")?.Value;
    public string? Nbf { get; set; } = claims.SingleOrDefault(c => c.Type == "nbf")?.Value;
    public string? Exp { get; set; } = claims.SingleOrDefault(c => c.Type == "exp")?.Value;
    public string? Iat { get; set; } = claims.SingleOrDefault(c => c.Type == "iat")?.Value;
    public string? Iss { get; set; } = claims.SingleOrDefault(c => c.Type == "iss")?.Value;
    public string? Aud { get; set; } = claims.SingleOrDefault(c => c.Type == "aud")?.Value;
}
