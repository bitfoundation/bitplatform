﻿namespace Boilerplate.Server.Api.Models.Emailing;

public partial class PrivilegedAccessTokenTemplateModel
{
    public required string DisplayName { get; set; }

    public required string Token { get; set; }
}
