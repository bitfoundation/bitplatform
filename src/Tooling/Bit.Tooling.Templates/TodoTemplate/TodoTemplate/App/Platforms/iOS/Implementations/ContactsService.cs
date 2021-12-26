namespace TodoTemplate.App.Platforms.iOS.Implementations;

public class ContactsService : IContactsService
{
    public async Task<List<ContactInfo>> GetContacts()
    {
        if (await Permissions.CheckStatusAsync<Permissions.ContactsRead>() != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.ContactsRead>();

        List<ContactInfo> result = new(); 
        // get contacts from iOS
        // here you've access to all iOS features

        for (int i = 0; i < 100; i++)
        {
            result.Add(new() { DisplayName = $"Test Contact {i}" });
        }

        return result;
    }
}