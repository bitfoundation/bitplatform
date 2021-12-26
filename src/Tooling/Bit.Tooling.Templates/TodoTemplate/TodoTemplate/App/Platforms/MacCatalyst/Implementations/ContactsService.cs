namespace TodoTemplate.App.Platforms.MacCatalyst.Implementations;

public class ContactsService : IContactsService
{
    public async Task<List<ContactInfo>> GetContacts()
    {
        if (await Permissions.CheckStatusAsync<Permissions.ContactsRead>() != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.ContactsRead>();

        List<ContactInfo> result = new();

        for (int i = 0; i < 100; i++)
        {
            result.Add(new() { DisplayName = $"Test Contact {i}" });
        }

        return result;
    }
}