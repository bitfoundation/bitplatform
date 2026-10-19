using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm.Tests;

public class UploadModel
{
    public string Name { get; set; } = "";
    public IBrowserFile? Resume { get; set; }
    public List<IBrowserFile> Attachments { get; set; } = [];
    public byte[]? Thumbnail { get; set; }
    public Stream? Archive { get; set; }
}

/// <summary>
/// A file property has to resolve to a file field. Falling through to the bottom of the type resolver
/// produced a text box, which is unusable — and silently so, since the schema still rendered.
/// </summary>
[TestClass]
public class FileTypeResolutionTests
{
    [TestMethod]
    public void A_browser_file_property_generates_a_file_field()
    {
        var form = BormSchemaGenerator.Generate<UploadModel>();

        var resume = form.FindField("Resume")!;
        Assert.AreEqual(BormFieldType.File, resume.Type);
        Assert.IsFalse(resume.Multiple);
    }

    [TestMethod]
    public void A_collection_of_files_is_one_multi_file_field_not_a_repeater()
    {
        var attachments = BormSchemaGenerator.Generate<UploadModel>().FindField("Attachments")!;

        Assert.AreEqual(BormFieldType.File, attachments.Type);
        Assert.IsTrue(attachments.Multiple);
        Assert.IsNull(attachments.ItemTemplate);
    }

    [TestMethod]
    public void Byte_arrays_and_streams_are_left_out_rather_than_given_a_picker_that_refuses_every_file()
    {
        // A file input produces an IBrowserFile, which cannot be written into a byte[] or a Stream, so
        // the file field these used to generate rejected every selection as "not a valid value".
        var form = BormSchemaGenerator.Generate<UploadModel>();

        Assert.IsNull(form.FindField("Thumbnail"));
        Assert.IsNull(form.FindField("Archive"));
        Assert.AreNotEqual(BormFieldType.File, BormFieldTypeResolver.Resolve(typeof(byte[])));
        Assert.AreNotEqual(BormFieldType.File, BormFieldTypeResolver.Resolve(typeof(Stream)));
    }

    [TestMethod]
    public void An_ordinary_collection_is_still_a_repeater()
    {
        var items = BormSchemaGenerator.Generate<RegistrationModel>().FindField("Items")!;

        Assert.AreEqual(BormFieldType.Array, items.Type);
        Assert.IsNotNull(items.ItemTemplate);
    }
}

/// <summary>
/// Prefilling a form from a saved record is one change, not thirty. Notifying per field re-renders the
/// whole form each time and briefly exposes a half-applied state to anything watching.
/// </summary>
[TestClass]
public class BatchUpdateTests
{
    private static (BormState State, int Notifications) Count(Action<BormState> act)
    {
        var form = BormBuilder.Create().Text("a").Text("b").Text("c").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        var notifications = 0;
        state.StateChanged += () => notifications++;
        act(state);
        return (state, notifications);
    }

    [TestMethod]
    public void Writing_one_at_a_time_notifies_once_per_field()
    {
        var (_, notifications) = Count(s =>
        {
            s.SetValue("a", "1");
            s.SetValue("b", "2");
            s.SetValue("c", "3");
        });

        Assert.AreEqual(3, notifications);
    }

    [TestMethod]
    public void SetValues_notifies_once_for_the_whole_batch()
    {
        var (state, notifications) = Count(s => s.SetValues(new Dictionary<string, object?>
        {
            ["a"] = "1", ["b"] = "2", ["c"] = "3"
        }));

        Assert.AreEqual(1, notifications);
        Assert.AreEqual("1", state.GetValue("a"));
        Assert.AreEqual("3", state.GetValue("c"));
    }

    [TestMethod]
    public void A_prefill_does_not_mark_fields_touched()
    {
        // The user has not visited these fields, so they must not open covered in errors.
        var (state, _) = Count(s => s.SetValues(new Dictionary<string, object?> { ["a"] = "1" }));

        Assert.IsFalse(state.IsTouched("a"));
        Assert.IsTrue(state.IsDirty("a"));
    }

    [TestMethod]
    public void Nested_batches_still_notify_exactly_once()
    {
        var (_, notifications) = Count(s => s.Batch(() =>
        {
            s.SetValue("a", "1");
            s.Batch(() => s.SetValue("b", "2"));
            s.SetValue("c", "3");
        }));

        Assert.AreEqual(1, notifications);
    }

    [TestMethod]
    public void A_batch_that_throws_still_releases_the_notifications()
    {
        var form = BormBuilder.Create().Text("a").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var notifications = 0;
        state.StateChanged += () => notifications++;

        Assert.ThrowsExactly<InvalidOperationException>(() => state.Batch(() =>
        {
            state.SetValue("a", "1");
            throw new InvalidOperationException("boom");
        }));

        // The pending notification is released, and the form is not left permanently silent.
        Assert.AreEqual(1, notifications);
        state.SetValue("a", "2");
        Assert.AreEqual(2, notifications);
    }
}
