using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm.Tests;

/// <summary>
/// Files sent as soon as they are chosen: judged first, their progress followed, what the uploader returned stored in
/// their place, a failure tried again, a cancel honoured - and a submit made meanwhile waiting for them.
/// </summary>
[TestClass]
public class UploadTests : ComponentTestBase
{
    private sealed class FakeFile(string name, int size, string type = "application/pdf") : IBrowserFile
    {
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size => size;
        public string ContentType => type;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (size > maxAllowedSize) throw new IOException("Too large.");
            return new MemoryStream(new byte[size]);
        }
    }

    private static (BormState State, Dictionary<string, object?> Data) Form(BormFileUploader uploader, Action<BormFieldBuilder>? configure = null)
    {
        var form = BormBuilder.Create()
            .Field("resume", BormFieldType.File, f =>
            {
                f.Label("Résumé").Required().UploadWith(uploader);
                configure?.Invoke(f);
            })
            .Build();
        var data = new Dictionary<string, object?>();
        return (new BormState(form, new BormDictionaryDataAccessor(data)), data);
    }

    // Reads the file in small pieces, as a request body would, and returns an id for it.
    private static async Task<string> Send(BormUploadContext upload)
    {
        await using var stream = upload.OpenReadStream();
        var buffer = new byte[100];
        while (await stream.ReadAsync(buffer, upload.CancellationToken) > 0) await Task.Yield();
        return "id:" + upload.File.Name;
    }

    [TestMethod]
    public async Task A_chosen_file_is_sent_its_progress_followed_and_what_came_back_stored_in_its_place()
    {
        var percents = new List<int>();
        var gate = new TaskCompletionSource();
        var (state, data) = Form(async u =>
        {
            await gate.Task;
            return await Send(u);
        });
        var field = state.Definition.Fields[0];

        var uploading = state.UploadAsync(field, "resume", [new FakeFile("cv.pdf", 1000)]);
        var upload = state.UploadsFor("resume").Single();
        upload.Changed += () => percents.Add(upload.Percent);
        Assert.IsTrue(state.IsUploading);
        Assert.AreEqual(BormUploadStatus.Uploading, upload.Status);
        gate.SetResult();
        await uploading;

        Assert.AreEqual(BormUploadStatus.Done, upload.Status);
        Assert.AreEqual(100, upload.Percent);
        Assert.AreEqual("id:cv.pdf", data["resume"]);
        Assert.IsFalse(state.IsUploading);
        // A notification per percent at most, not one per buffer.
        Assert.IsTrue(percents.Count is > 1 and <= 101, $"{percents.Count} notifications");
        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task Several_files_are_stored_as_the_list_of_what_came_back()
    {
        var (state, data) = Form(Send, f => f.Accept(".pdf", multiple: true));
        var field = state.Definition.Fields[0];

        // On the dispatcher, as in an app: two files finishing together are two pieces of work done one after the
        // other, not two threads writing the field at once.
        await Renderer.Dispatcher.InvokeAsync(async () =>
        {
            await state.UploadAsync(field, "resume", [new FakeFile("a.pdf", 10), new FakeFile("b.pdf", 10)]);
            await state.UploadAsync(field, "resume", [new FakeFile("c.pdf", 10)]);
        });

        CollectionAssert.AreEqual(new[] { "id:a.pdf", "id:b.pdf", "id:c.pdf" }, (System.Collections.ICollection)data["resume"]!);
        // The ids carry no extension, and are not judged as file names: the files were judged before they went.
        Assert.IsTrue(await state.ValidateAsync());

        state.RemoveUpload(field, "resume", state.UploadsFor("resume")[1]);
        CollectionAssert.AreEqual(new[] { "id:a.pdf", "id:c.pdf" }, (System.Collections.ICollection)data["resume"]!);
    }

    [TestMethod]
    public async Task A_field_taking_several_files_keeps_the_ones_it_opened_with_beside_the_new_ones()
    {
        var (state, data) = Form(Send, f => f.Accept(".pdf", multiple: true));
        var field = state.Definition.Fields[0];
        data["resume"] = new List<string> { "id1", "id2" };   // an edit form's saved files

        await Renderer.Dispatcher.InvokeAsync(() => state.UploadAsync(field, "resume", [new FakeFile("c.pdf", 10)]));

        CollectionAssert.AreEqual(new[] { "id1", "id2", "id:c.pdf" }, (System.Collections.ICollection)data["resume"]!);
        CollectionAssert.AreEqual(new[] { "id1", "id2" }, state.StoredUploadsFor("resume").ToArray());

        state.RemoveStoredUpload(field, "resume", "id1");
        CollectionAssert.AreEqual(new[] { "id2", "id:c.pdf" }, (System.Collections.ICollection)data["resume"]!);
        state.RemoveUpload(field, "resume", state.UploadsFor("resume").Single());
        CollectionAssert.AreEqual(new[] { "id2" }, (System.Collections.ICollection)data["resume"]!);
    }

    [TestMethod]
    public void The_files_a_field_opened_with_are_listed_beside_the_new_ones_each_to_be_removed()
    {
        var form = BormBuilder.Create()
            .Field("docs", BormFieldType.File, f => f.Label("Documents").Accept(".pdf", multiple: true).UploadWith(Send))
            .Build();
        var data = new Dictionary<string, object?> { ["docs"] = new List<string> { "id1", "id2" } };
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "c.pdf"));

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "id1", "id2", "id:c.pdf" }, (System.Collections.ICollection)data["docs"]!));
        cut.WaitForAssertion(() => Assert.HasCount(3, cut.FindAll(".bit-borm-file__item")));
        cut.Find(".bit-borm-file__item [aria-label='Remove id1']").Click();
        CollectionAssert.AreEqual(new[] { "id2", "id:c.pdf" }, (System.Collections.ICollection)data["docs"]!);
    }

    [TestMethod]
    public async Task Choosing_another_file_lets_go_of_the_one_it_replaces_even_when_the_new_one_fails()
    {
        var attempts = 0;
        var (state, data) = Form(u => ++attempts == 2 ? throw new HttpRequestException("503") : Send(u));
        var field = state.Definition.Fields[0];

        await state.UploadAsync(field, "resume", [new FakeFile("a.pdf", 10)]);
        Assert.AreEqual("id:a.pdf", data["resume"]);

        await state.UploadAsync(field, "resume", [new FakeFile("b.pdf", 10)]);

        Assert.AreEqual(BormUploadStatus.Failed, state.UploadsFor("resume").Single().Status);
        // The field shows only b.pdf, failed; a.pdf is not quietly sent with the form either.
        Assert.IsNull(data["resume"]);
        Assert.IsFalse(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task A_file_the_rules_refuse_is_never_sent()
    {
        var sent = 0;
        var (state, data) = Form(u => { sent++; return Send(u); }, f => f.MaxFileSize(100).Accept(".pdf"));

        await state.UploadAsync(state.Definition.Fields[0], "resume", [new FakeFile("huge.pdf", 5000)]);
        await state.UploadAsync(state.Definition.Fields[0], "resume", [new FakeFile("photo.png", 10, "image/png")]);

        Assert.AreEqual(0, sent);
        Assert.IsNull(data.GetValueOrDefault("resume"));
        StringAssert.Contains(state.MessagesFor("resume").Single().Message, ".pdf");
    }

    [TestMethod]
    public async Task A_failed_upload_says_so_beside_the_file_and_can_be_tried_again()
    {
        var attempts = 0;
        var (state, data) = Form(u => ++attempts == 1 ? throw new HttpRequestException("503") : Send(u));
        var field = state.Definition.Fields[0];

        await state.UploadAsync(field, "resume", [new FakeFile("cv.pdf", 10)]);
        var upload = state.UploadsFor("resume").Single();
        Assert.AreEqual(BormUploadStatus.Failed, upload.Status);
        Assert.AreEqual("cv.pdf could not be uploaded.", upload.Error);
        Assert.IsInstanceOfType<HttpRequestException>(upload.Exception);
        Assert.IsNull(data.GetValueOrDefault("resume"));

        await state.RetryUploadAsync(field, "resume", upload);
        Assert.AreEqual(BormUploadStatus.Done, upload.Status);
        Assert.AreEqual("id:cv.pdf", data["resume"]);
    }

    [TestMethod]
    public async Task Cancelling_stops_the_upload_through_its_token()
    {
        var (state, data) = Form(async u =>
        {
            await Task.Delay(Timeout.Infinite, u.CancellationToken);
            return "never";
        });
        var field = state.Definition.Fields[0];

        var uploading = state.UploadAsync(field, "resume", [new FakeFile("cv.pdf", 10)]);
        var upload = state.UploadsFor("resume").Single();
        state.RemoveUpload(field, "resume", upload);
        await uploading;

        Assert.AreEqual(BormUploadStatus.Cancelled, upload.Status);
        Assert.IsEmpty(state.UploadsFor("resume"));
        Assert.IsNull(data.GetValueOrDefault("resume"));
    }

    [TestMethod]
    public async Task A_submit_made_while_a_file_is_on_its_way_waits_for_it()
    {
        var gate = new TaskCompletionSource();
        var (state, data) = Form(async u =>
        {
            await gate.Task;
            return await Send(u);
        });

        _ = state.UploadAsync(state.Definition.Fields[0], "resume", [new FakeFile("cv.pdf", 10)]);
        object? sent = null;
        var submit = state.SubmitAsync((_, _) => { sent = data.GetValueOrDefault("resume"); return Task.CompletedTask; });
        Assert.IsFalse(submit.IsCompleted);

        gate.SetResult();

        Assert.IsTrue(await submit);
        Assert.AreEqual("id:cv.pdf", sent);
    }

    [TestMethod]
    public void A_file_chosen_in_the_form_is_listed_with_its_outcome_and_the_review_names_it()
    {
        var form = BormBuilder.Create()
            .Field("resume", BormFieldType.File, f => f.Label("Résumé").UploadWith(Send))
            .Review("summary")
            .Build();
        var data = new Dictionary<string, object?>();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "cover.pdf"));

        cut.WaitForAssertion(() => Assert.AreEqual("id:cover.pdf", data["resume"]));
        cut.WaitForAssertion(() => StringAssert.Contains(cut.Find(".bit-borm-file__item").TextContent, "cover.pdf"));
        Assert.AreEqual("Remove cover.pdf", cut.Find(".bit-borm-file__item .bit-borm-btn--danger").GetAttribute("aria-label"));
        StringAssert.Contains(cut.Find(".bit-borm-file [aria-live]").TextContent, "cover.pdf uploaded");
        StringAssert.Contains(cut.Find(".bit-borm-review").TextContent, "cover.pdf");
    }
}
