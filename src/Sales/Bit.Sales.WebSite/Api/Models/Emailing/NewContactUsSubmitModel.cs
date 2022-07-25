using Bit.Sales.WebSite.Shared.Dtos.ContactUs;

namespace Bit.Sales.WebSite.Api.Models.Emailing;

public class NewContactUsSubmitModel
{
    public ContactUsDto ContactUsInfo { get; set; }

    public Uri? HostUri { get; set; }
}
