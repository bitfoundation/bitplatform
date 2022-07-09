namespace Bit.Sales.WebSite.Shared.Dtos.ContactUs
{
    public class ContactUsDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Name { get; set; }

        public string? Information { get; set; }
    }
}
