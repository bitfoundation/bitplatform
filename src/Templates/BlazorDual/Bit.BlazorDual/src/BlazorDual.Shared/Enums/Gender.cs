﻿namespace BlazorDual.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<EmailTemplate>))]
public enum Gender
{
    Male,
    Female,
    Custom
}
