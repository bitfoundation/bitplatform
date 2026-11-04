namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public partial class HotelSoftwareFrontDesk
{
    [AutoInject] private IStringLocalizer<HotelSoftwareStrings> H = default!;

    private readonly List<Room> rooms =
    [
        new("101", nameof(HotelSoftwareStrings.DeskKindDouble), RoomStatus.Occupied, "Okafor"),
        new("102", nameof(HotelSoftwareStrings.DeskKindTwin), RoomStatus.Departing, "Lindgren"),
        new("103", nameof(HotelSoftwareStrings.DeskKindDouble), RoomStatus.Arriving, "Moreau"),
        new("104", nameof(HotelSoftwareStrings.DeskKindSuite), RoomStatus.Occupied, "Haddad"),
        new("105", nameof(HotelSoftwareStrings.DeskKindSingle), RoomStatus.Dirty, null),
        new("106", nameof(HotelSoftwareStrings.DeskKindDouble), RoomStatus.Departing, "Novak"),
        new("201", nameof(HotelSoftwareStrings.DeskKindDouble), RoomStatus.Arriving, "Tanaka"),
        new("202", nameof(HotelSoftwareStrings.DeskKindTwin), RoomStatus.Occupied, "Silva"),
        new("203", nameof(HotelSoftwareStrings.DeskKindFamily), RoomStatus.Arriving, "Brennan"),
        new("204", nameof(HotelSoftwareStrings.DeskKindDouble), RoomStatus.OutOfOrder, null),
        new("205", nameof(HotelSoftwareStrings.DeskKindSingle), RoomStatus.Occupied, "Kowalski"),
        new("206", nameof(HotelSoftwareStrings.DeskKindDouble), RoomStatus.Arriving, "Rossi")
    ];

    private readonly List<Stay> arrivals =
    [
        new("Moreau", "103", nameof(HotelSoftwareStrings.DeskArrival1), true),
        new("Tanaka", "201", nameof(HotelSoftwareStrings.DeskArrival2), true),
        new("Brennan", "203", nameof(HotelSoftwareStrings.DeskArrival3), true),
        new("Rossi", "206", nameof(HotelSoftwareStrings.DeskArrival4), true)
    ];

    private readonly List<Stay> departures =
    [
        new("Lindgren", "102", nameof(HotelSoftwareStrings.DeskDeparture1), false),
        new("Novak", "106", nameof(HotelSoftwareStrings.DeskDeparture2), false)
    ];

    private string? status;
    private string? changedRoom;
    private string today = string.Empty;

    private int InHouse => rooms.Count(r => r.Status is RoomStatus.Occupied or RoomStatus.Departing);

    private int Arriving => rooms.Count(r => r.Status is RoomStatus.Arriving);

    private int Departing => rooms.Count(r => r.Status is RoomStatus.Departing);

    private int ToClean => rooms.Count(r => r.Status is RoomStatus.Dirty);

    private int Sellable => rooms.Count(r => r.Status is not RoomStatus.OutOfOrder);

    private int SoldTonight => rooms.Count(r => r.Status is RoomStatus.Occupied or RoomStatus.Arriving);

    protected override void OnInitialized()
    {
        today = DateTime.UtcNow.ToString("dddd d MMMM", CurrentCulture);

        base.OnInitialized();
    }

    private Room Find(string number) => rooms.First(r => r.Number == number);

    private void CheckIn(Stay stay)
    {
        var room = Find(stay.Room);

        room.Status = RoomStatus.Occupied;
        stay.IsDone = true;
        changedRoom = room.Number;
        status = H[nameof(HotelSoftwareStrings.DeskMessageIn), stay.Guest, room.Number];
    }

    private void CheckOut(Stay stay)
    {
        var room = Find(stay.Room);

        room.Status = RoomStatus.Dirty;
        room.Guest = null;
        stay.IsDone = true;
        changedRoom = room.Number;
        status = H[nameof(HotelSoftwareStrings.DeskMessageOut), stay.Guest, room.Number];
    }

    private void MarkClean(Room room)
    {
        room.Status = RoomStatus.Clean;
        changedRoom = room.Number;
        status = H[nameof(HotelSoftwareStrings.DeskMessageClean), room.Number];
    }

    private string StatusName(RoomStatus value) => H[$"DeskStatus{value}"];

    private static string StatusClass(RoomStatus value) => value switch
    {
        RoomStatus.Clean => "is-clean",
        RoomStatus.Dirty => "is-dirty",
        RoomStatus.Occupied => "is-occupied",
        RoomStatus.Arriving => "is-arriving",
        RoomStatus.Departing => "is-departing",
        _ => "is-ooo"
    };

    private enum RoomStatus
    {
        Clean,
        Dirty,
        Occupied,
        Arriving,
        Departing,
        OutOfOrder
    }

    private sealed class Room(string number, string kind, RoomStatus status, string? guest)
    {
        public string Number { get; } = number;

        public string Kind { get; } = kind;

        public RoomStatus Status { get; set; } = status;

        public string? Guest { get; set; } = guest;
    }

    private sealed class Stay(string guest, string room, string detail, bool isArrival)
    {
        public string Guest { get; } = guest;

        public string Room { get; } = room;

        public string Detail { get; } = detail;

        public bool IsArrival { get; } = isArrival;

        public bool IsDone { get; set; }
    }
}
