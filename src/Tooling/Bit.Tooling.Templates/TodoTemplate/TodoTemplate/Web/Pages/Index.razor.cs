namespace TodoTemplate.App.Pages;

public partial class Index
{
    [Inject] public IContactsService ContactsService { get; set; }
    [Inject] public IToastService ToastService { get; set; }

    public List<ContactInfo> Contacts { get; set; }

    public async Task Reload()
    {
        Contacts = await ContactsService.GetContacts();
        await ToastService.ShowToast("Done!");
    }

    protected async override Task OnInitializedAsync()
    {
#if DEBUG
        await Task.Delay(TimeSpan.FromSeconds(2));
#endif

        Contacts = await ContactsService.GetContacts();

        await base.OnInitializedAsync();
    }
}
