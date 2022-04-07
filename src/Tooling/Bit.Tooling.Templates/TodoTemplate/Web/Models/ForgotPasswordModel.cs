namespace TodoTemplate.App.Models
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "The email address format is incorrect.")]
        public string? Email { get; set; }
    }
}
