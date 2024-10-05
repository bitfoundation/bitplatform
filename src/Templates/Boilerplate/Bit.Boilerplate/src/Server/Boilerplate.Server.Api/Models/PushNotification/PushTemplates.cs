namespace Boilerplate.Server.Api.Models.PushNotification;

public partial class PushTemplates
{
    public partial class Generic
    {
        public const string Android = "{ \"message\" : { \"notification\" : { \"title\" : \"Boilerplate\", \"body\" : \"$(alertMessage)\"}, \"data\" : { \"action\" : \"$(alertAction)\" } } }";
        public const string iOS = "{ \"aps\" : {\"alert\" : \"$(alertMessage)\"}, \"action\" : \"$(alertAction)\" }";
    }

    public partial class Silent
    {
        public const string Android = "{ \"message\" : { \"data\" : {\"message\" : \"$(alertMessage)\", \"action\" : \"$(alertAction)\"} } }";
        public const string iOS = "{ \"aps\" : {\"content-available\" : 1, \"apns-priority\": 5, \"sound\" : \"\", \"badge\" : 0}, \"message\" : \"$(alertMessage)\", \"action\" : \"$(alertAction)\" }";
    }
}
