namespace Bit.BlazeDb.Samples.Core.Data;

public static class SampleData
{
    public static readonly string[] Categories = ["Work", "Home", "Errands"];

    /// <summary>
    /// The rows a new database starts with. Registered as migration 1 (see <see cref="SampleDatabase"/>), so they are
    /// added exactly once per database - in the transaction that moves its UserVersion to 1 - and deleting them
    /// all does not bring them back the next time it opens.
    /// </summary>
    public static void Seed(BlazeDbDatabase db)
    {
        var now = DateTime.UtcNow;

        db.GetTable(TodoItem.Table).InsertMany(
        [
            Todo("Review the pull request", "Work", done: false, now.AddHours(-30)),
            Todo("Write the release notes", "Work", done: false, now.AddHours(-26)),
            Todo("Plan the sprint", "Work", done: true, now.AddHours(-24)),
            Todo("Reply to the support ticket", "Work", done: false, now.AddHours(-5)),
            Todo("Water the plants", "Home", done: true, now.AddHours(-20)),
            Todo("Fix the kitchen tap", "Home", done: false, now.AddHours(-12)),
            Todo("Book the holiday", "Home", done: false, now.AddHours(-3)),
            Todo("Buy groceries", "Errands", done: false, now.AddHours(-8)),
            Todo("Post the parcel", "Errands", done: true, now.AddHours(-7)),
            Todo("Pick up the dry cleaning", "Errands", done: false, now.AddHours(-1)),
        ]);
    }

    private static TodoItem Todo(string title, string category, bool done, DateTime createdAt) =>
        new() { Id = Guid.NewGuid(), Title = title, Category = category, Done = done, CreatedAt = createdAt };
}
