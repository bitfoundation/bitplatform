namespace Bit.BlazorUI.Demo.Client.Web.Services;
public partial class WebExceptionHandler : ExceptionHandlerBase
{
    public WebExceptionHandler(IStringLocalizer<AppStrings> localizer)
        : base(localizer)
    {
    }

    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        base.Handle(exception, parameters);
    }
}
