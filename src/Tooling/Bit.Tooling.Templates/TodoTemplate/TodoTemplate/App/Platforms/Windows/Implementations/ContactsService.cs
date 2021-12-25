namespace TodoTemplate.App.Platforms.Windows.Implementations;

public class ContactsService : IContactsService
{
    public async Task<List<ContactInfo>> GetContacts()
    {
        if (await Permissions.CheckStatusAsync<Permissions.ContactsRead>() != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.ContactsRead>();

        var windowsContancts = await Contacts.GetAllAsync();

        return windowsContancts.Select(c => new ContactInfo
        {
            Id = c.Id,
            DisplayName = c.DisplayName
        }).ToList();

        List<ContactInfo> result = new();

        for (int i = 0; i < 100; i++)
        {
            result.Add(new() { DisplayName = $"Test Contact {i}" });
        }

        return result;
    }
}