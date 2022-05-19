namespace TodoTemplate.Api
{
    public class AppSettings
    {
        public IdentitySettings IdentitySettings { get; set; }

        public JwtSettings JwtSettings { get; set; }

        public EmailSettings EmailSettings { get; set; }

        public HealCheckSettings HealCheckSettings { get; set; }
        
        public string UserProfileImagePath { get; set; }

        public string WebServerAddress { get; set; }
    }

    public class HealCheckSettings
    {
        public bool EnableHealthChecks { get; set; }
    }

    public class IdentitySettings
    {
        public bool PasswordRequireDigit { get; set; }
        public int PasswordRequiredLength { get; set; }
        public bool PasswordRequireNonAlphanumeric { get; set; }
        public bool PasswordRequireUppercase { get; set; }
        public bool PasswordRequireLowercase { get; set; }
        public bool RequireUniqueEmail { get; set; }
        public TimeSpan ConfirmationEmailResendDelay { get; set; }
        public TimeSpan ResetPasswordEmailResendDelay { get; set; }
    }

    public class JwtSettings
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int NotBeforeMinutes { get; set; }
        public int ExpirationMinutes { get; set; }
    }

    public class EmailSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string DefaulFromEmail { get; set; }
        public string DefaultFromName { get; set; }
        public bool HasCredential => (string.IsNullOrEmpty(UserName) is false) && (string.IsNullOrEmpty(Password) is false);
    }
}
