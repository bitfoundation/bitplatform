namespace TodoTemplate.App.Models
{
    public class SignUpModel
    {
        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "The email address format is incorrect.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        [MinLength(6, ErrorMessage = "The password must have at least 6 characters.")]
        public string? Password { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the privacy.")]
        public bool IsAcceptPrivacy { get; set; }
    }
}
