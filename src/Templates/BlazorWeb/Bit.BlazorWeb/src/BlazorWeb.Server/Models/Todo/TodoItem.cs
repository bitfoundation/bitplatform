﻿using BlazorWeb.Server.Models.Identity;

namespace BlazorWeb.Server.Models.Todo;

public class TodoItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool IsDone { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    public int UserId { get; set; }
}
