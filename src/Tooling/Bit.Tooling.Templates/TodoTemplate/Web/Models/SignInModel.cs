namespace TodoTemplate.App.Models
{
    public class SignInModel
    {
        [Required(ErrorMessage = "Please enter your email.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        public string? Password { get; set; }
    }
}
