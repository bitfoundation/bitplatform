using Bit.Websites.Sales.Shared.Dtos.ContactUs;

namespace Bit.Websites.Sales.Server.Models.Emailing;

public class NewContactUsSubmitModel
{
    public ContactUsDto ContactUsInfo { get; set; }

    public Uri? HostUri { get; set; }
}
