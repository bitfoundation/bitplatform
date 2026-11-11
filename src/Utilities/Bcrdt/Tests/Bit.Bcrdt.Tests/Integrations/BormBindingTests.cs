using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Bit.Bcrdt.Borm;
using Bit.Borm;

namespace Bit.Bcrdt.Tests.Integrations;

public enum BormTicketPriority { Low, Normal, High }

public sealed class BormTicketAddress
{
    public string City { get; set; } = "";
    public string Street { get; set; } = "";
}

public sealed class BormTicketLine
{
    public string Product { get; set; } = "";
    public int Quantity { get; set; }
    [DataType(DataType.MultilineText)] public string Notes { get; set; } = "";
}

public sealed class BormTicket
{
    public string Title { get; set; } = "";
    [DataType(DataType.MultilineText)] public string Description { get; set; } = "";
    public int Estimate { get; set; }
    public BormTicketPriority Priority { get; set; }
    public DateOnly? Due { get; set; }
    public bool Urgent { get; set; }
    [DataType(DataType.Password)] public string Secret { get; set; } = "";
    public BormTicketAddress Address { get; set; } = new();
    public List<BormTicketLine> Lines { get; set; } = [];
}

/// <summary>
/// <see cref="BcrdtBormBinding"/>: several people filling in one Borm form, each change written as the
/// field it is, rows kept by identity, and nothing echoed back.
/// </summary>
[TestClass]
public class BormBindingTests
{
    private static readonly BormDefinition Definition = BormSchemaGenerator.Generate<BormTicket>();

    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 25; seed++) yield return [seed];
        }
    }

    private sealed class Replica : IDisposable
    {
        public Replica(BcrdtDocument doc, BormTicket? model = null, BcrdtBormOptions? options = null)
        {
            Doc = doc;
            Model = model ?? new BormTicket();
            State = new BormState(Definition, new BormModelDataAccessor(Model));
            Binding = new BcrdtBormBinding(State, doc.GetMap("ticket"), options);
        }

        public BcrdtDocument Doc { get; }
        public BormTicket Model { get; }
        public BormState State { get; }
        public BcrdtBormBinding Binding { get; }
        public BormFieldDefinition Lines => State.Definition.FindField("Lines")!;

        public void Dispose()
        {
            Binding.Dispose();
            State.Dispose();
        }
    }

    private static BcrdtDocument Doc(ulong clientId) => new(new BcrdtDocumentOptions { ClientId = clientId });

    private static void Sync(params BcrdtDocument[] docs)
    {
        foreach (BcrdtDocument source in docs)
        {
            foreach (BcrdtDocument target in docs)
            {
                if (!ReferenceEquals(source, target)) target.ApplyUpdate(source.EncodeStateAsUpdate(target.GetStateVector()), origin: "sync");
            }
        }
    }

    private static string Json(BormTicket ticket) => JsonSerializer.Serialize(ticket);

    [TestMethod]
    public void The_first_form_seeds_the_document_and_the_next_opens_with_what_it_holds()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        var seeded = new BormTicket
        {
            Title = "Printer on fire",
            Description = "It started\nwith smoke.",
            Estimate = 3,
            Priority = BormTicketPriority.High,
            Due = new DateOnly(2026, 11, 30),
            Urgent = true,
            Secret = "hunter2",
            Address = new BormTicketAddress { City = "Oslo", Street = "Main 1" },
            Lines = [new BormTicketLine { Product = "Toner", Quantity = 2, Notes = "black" }, new BormTicketLine { Product = "Paper", Quantity = 5 }],
        };
        using var first = new Replica(a, seeded);
        Sync(a, b);
        using var second = new Replica(b, new BormTicket { Title = "something else", Lines = [new BormTicketLine { Product = "stale" }] });

        BcrdtMap stored = a.GetMap("ticket");
        Assert.IsInstanceOfType<BcrdtText>(stored.Get("Description"), "a text area merges");
        Assert.AreEqual("High", stored.Get("Priority"));
        Assert.AreEqual("2026-11-30", stored.Get("Due"));
        Assert.IsFalse(stored.ContainsKey("Secret"), "a password is never shared");

        Assert.AreEqual("Printer on fire", second.Model.Title);
        Assert.AreEqual("It started\nwith smoke.", second.Model.Description);
        Assert.AreEqual(BormTicketPriority.High, second.Model.Priority);
        Assert.AreEqual(new DateOnly(2026, 11, 30), second.Model.Due);
        Assert.AreEqual("Oslo", second.Model.Address.City);
        CollectionAssert.AreEqual(new[] { "Toner", "Paper" }, second.Model.Lines.Select(l => l.Product).ToArray());
        Assert.AreEqual("black", second.Model.Lines[0].Notes);
        Assert.AreEqual("", second.Model.Secret);

        seeded.Secret = "";
        Assert.AreEqual(Json(seeded), Json(second.Model));
        Assert.IsFalse(second.State.TouchedFields.Count > 0, "what arrives is not something the user touched");
    }

    [TestMethod]
    public void A_change_writes_only_the_field_that_changed()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a);
        Sync(a, b);
        using var second = new Replica(b);

        List<IReadOnlyCollection<string>> changedKeys = [];
        using IDisposable observer = b.GetMap("ticket").Observe((change, _) => changedKeys.Add([.. change.Keys.Keys]));

        first.State.SetValue("Estimate", 8);
        Sync(a, b);

        Assert.AreEqual(1, changedKeys.Count);
        CollectionAssert.AreEqual(new[] { "Estimate" }, changedKeys[0].ToArray());
        Assert.AreEqual(8, second.Model.Estimate);
    }

    [TestMethod]
    public void Concurrent_edits_of_different_fields_both_survive()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Title = "Draft" });
        Sync(a, b);
        using var second = new Replica(b);

        first.State.SetValue("Title", "Final title");
        first.State.SetValue("Address.City", "Bergen");
        second.State.SetValue("Estimate", 13);
        second.State.SetValue("Priority", BormTicketPriority.Low);
        Sync(a, b);

        foreach (Replica replica in new[] { first, second })
        {
            Assert.AreEqual("Final title", replica.Model.Title);
            Assert.AreEqual("Bergen", replica.Model.Address.City);
            Assert.AreEqual(13, replica.Model.Estimate);
            Assert.AreEqual(BormTicketPriority.Low, replica.Model.Priority);
        }
    }

    [TestMethod]
    public void Concurrent_edits_of_one_field_converge_on_one_value()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a);
        Sync(a, b);
        using var second = new Replica(b);

        first.State.SetValue("Title", "from a");
        second.State.SetValue("Title", "from b");
        Sync(a, b);

        Assert.AreEqual(first.Model.Title, second.Model.Title);
        Assert.IsTrue(first.Model.Title is "from a" or "from b");
    }

    [TestMethod]
    public void Concurrent_typing_in_one_text_field_merges()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Description = "Hello world" });
        Sync(a, b);
        using var second = new Replica(b);

        first.State.SetValue("Description", "Hello big world");
        second.State.SetValue("Description", "Hello world!");
        Sync(a, b);

        Assert.AreEqual("Hello big world!", first.Model.Description);
        Assert.AreEqual("Hello big world!", second.Model.Description);
        Assert.AreEqual("Hello big world!", a.GetMap("ticket").GetText("Description")!.ToString());
    }

    [TestMethod]
    public void Text_fields_merge_as_the_options_say()
    {
        using BcrdtDocument a = Doc(1);
        using var replica = new Replica(a, new BormTicket { Title = "t", Description = "d" },
            new BcrdtBormOptions { MergeText = field => field.Name == "Title" });

        Assert.IsInstanceOfType<BcrdtText>(a.GetMap("ticket").Get("Title"));
        Assert.AreEqual("d", a.GetMap("ticket").Get("Description"));
        Assert.AreSame(a.GetMap("ticket").Get("Title"), replica.Binding.TextOf("Title"));
        Assert.IsNull(replica.Binding.TextOf("Description"));
    }

    [TestMethod]
    public void A_remote_change_to_the_field_being_edited_waits_until_editing_ends()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Title = "Old", Description = "one two" });
        Sync(a, b);
        using var second = new Replica(b);

        first.Binding.BeginEditing("Title");
        Assert.AreEqual("Title", first.Binding.EditingPath);
        second.State.SetValue("Title", "Theirs");
        second.State.SetValue("Estimate", 5);
        Sync(a, b);

        Assert.AreEqual("Old", first.Model.Title, "the box being typed in is not overwritten");
        Assert.AreEqual(5, first.Model.Estimate, "every other field follows at once");

        first.Binding.EndEditing();
        Assert.AreEqual("Theirs", first.Model.Title);
        Assert.IsNull(first.Binding.EditingPath);

        // A held-back text still merges with what the user commits: their edit is read against what
        // they were shown, not against the newer text.
        first.Binding.BeginEditing("Description");
        second.State.SetValue("Description", "zero one two");
        Sync(a, b);
        Assert.AreEqual("one two", first.Model.Description);

        first.State.SetValue("Description", "one two three");
        Assert.AreEqual("one two three", first.Model.Description, "still the user's own box");
        first.Binding.EndEditing();
        Sync(a, b);

        Assert.AreEqual("zero one two three", first.Model.Description);
        Assert.AreEqual("zero one two three", second.Model.Description);
    }

    [TestMethod]
    public void What_the_user_commits_in_the_field_they_edit_wins_over_the_change_held_back()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a);
        Sync(a, b);
        using var second = new Replica(b);

        first.Binding.BeginEditing("Title");
        second.State.SetValue("Title", "Theirs");
        Sync(a, b);
        first.State.SetValue("Title", "Mine");
        first.Binding.EndEditing();
        Sync(a, b);

        Assert.AreEqual("Mine", first.Model.Title);
        Assert.AreEqual("Mine", second.Model.Title);
    }

    [TestMethod]
    public void Repeater_rows_are_inserted_removed_and_reordered_by_identity()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Lines = [new() { Product = "A" }, new() { Product = "B" }, new() { Product = "C" }] });
        Sync(a, b);
        using var second = new Replica(b);

        int at = first.State.InsertArrayItem(first.Lines, "Lines", 1);
        first.State.SetValue($"Lines[{at}].Product", "X");
        Sync(a, b);
        CollectionAssert.AreEqual(new[] { "A", "X", "B", "C" }, second.Model.Lines.Select(l => l.Product).ToArray());

        second.State.MoveArrayItem("Lines", 3, 0);
        Sync(a, b);
        CollectionAssert.AreEqual(new[] { "C", "A", "X", "B" }, first.Model.Lines.Select(l => l.Product).ToArray());

        BormTicketLine moved = first.Model.Lines[0];
        first.State.RemoveArrayItem("Lines", 2);
        Sync(a, b);
        CollectionAssert.AreEqual(new[] { "C", "A", "B" }, second.Model.Lines.Select(l => l.Product).ToArray());
        Assert.AreSame(moved, first.Model.Lines[0], "a row keeps its object across moves");

        BcrdtMap holder = a.GetMap("ticket").GetMap("Lines")!;
        Assert.AreEqual(3, holder.GetMap("rows")!.Count, "a move writes the row's order key, not a copy of the row");
        Assert.IsNull(holder.Get("order"));
    }

    [TestMethod]
    public void Moving_a_row_writes_only_its_own_order_key()
    {
        using BcrdtDocument a = Doc(1);
        using var first = new Replica(a, new BormTicket { Lines = [new() { Product = "A" }, new() { Product = "B" }, new() { Product = "C" }, new() { Product = "D" }] });

        BcrdtMap rows = a.GetMap("ticket").GetMap("Lines")!.GetMap("rows")!;
        Dictionary<string, object?> before = rows.Keys.ToDictionary(id => id, id => rows.GetMap(id)!.Get("$order"));
        Assert.IsTrue(before.Values.All(order => BcrdtOrderKey.IsValid(order as string)));

        first.State.MoveArrayItem("Lines", 3, 0);

        string[] changed = [.. rows.Keys.Where(id => !Equals(before[id], rows.GetMap(id)!.Get("$order")))];
        Assert.HasCount(1, changed, "the rows that kept their relative order keep their keys");
        CollectionAssert.AreEqual(new[] { "D", "A", "B", "C" }, first.Model.Lines.Select(l => l.Product).ToArray());
    }

    [TestMethod]
    public void An_edit_lands_on_its_row_while_a_row_is_inserted_above_it()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Lines = [new() { Product = "A" }, new() { Product = "B" }] });
        Sync(a, b);
        using var second = new Replica(b);

        first.State.InsertArrayItem(first.Lines, "Lines", 0);
        first.State.SetValue("Lines[0].Product", "New");
        second.State.SetValue("Lines[1].Quantity", 7);   // row B, as the second user sees it
        Sync(a, b);

        foreach (Replica replica in new[] { first, second })
        {
            CollectionAssert.AreEqual(new[] { "New", "A", "B" }, replica.Model.Lines.Select(l => l.Product).ToArray());
            Assert.AreEqual(7, replica.Model.Lines[2].Quantity);
            Assert.AreEqual(0, replica.Model.Lines[1].Quantity);
        }
    }

    [TestMethod]
    public void A_row_moved_while_another_user_edits_it_keeps_the_edit()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Lines = [new() { Product = "A" }, new() { Product = "B" }, new() { Product = "C" }] });
        Sync(a, b);
        using var second = new Replica(b);

        first.State.MoveArrayItem("Lines", 0, 2);
        second.State.SetValue("Lines[0].Notes", "keep me");
        second.State.SetValue("Lines[0].Quantity", 4);
        Sync(a, b);

        foreach (Replica replica in new[] { first, second })
        {
            CollectionAssert.AreEqual(new[] { "B", "C", "A" }, replica.Model.Lines.Select(l => l.Product).ToArray());
            Assert.AreEqual("keep me", replica.Model.Lines[2].Notes);
            Assert.AreEqual(4, replica.Model.Lines[2].Quantity);
        }
    }

    [TestMethod]
    public void Concurrent_row_inserts_both_survive_and_a_removed_row_stays_removed()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Lines = [new() { Product = "A" }, new() { Product = "B" }] });
        Sync(a, b);
        using var second = new Replica(b);

        int fromA = first.State.AddArrayItem(first.Lines, "Lines");
        first.State.SetValue($"Lines[{fromA}].Product", "from a");
        int fromB = second.State.AddArrayItem(second.Lines, "Lines");
        second.State.SetValue($"Lines[{fromB}].Product", "from b");
        second.State.RemoveArrayItem("Lines", 0);
        first.State.SetValue("Lines[1].Product", "B edited");
        Sync(a, b);

        Assert.AreEqual(Json(first.Model), Json(second.Model));
        string[] products = [.. first.Model.Lines.Select(l => l.Product)];
        CollectionAssert.DoesNotContain(products, "A");
        CollectionAssert.Contains(products, "B edited");
        CollectionAssert.Contains(products, "from a");
        CollectionAssert.Contains(products, "from b");
        Assert.AreEqual(3, products.Length);
    }

    [TestMethod]
    public void Concurrent_moves_of_one_row_leave_one_row()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Lines = [new() { Product = "A" }, new() { Product = "B" }, new() { Product = "C" }] });
        Sync(a, b);
        using var second = new Replica(b);

        first.State.MoveArrayItem("Lines", 0, 2);
        second.State.MoveArrayItem("Lines", 0, 1);
        Sync(a, b);

        Assert.AreEqual(Json(first.Model), Json(second.Model));
        Assert.AreEqual(3, first.Model.Lines.Count);
        CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, first.Model.Lines.Select(l => l.Product).ToArray());

        // A move is a write of the row's order key: one of the two won, and there is nothing to tidy.
        Assert.AreEqual(3, a.GetMap("ticket").GetMap("Lines")!.GetMap("rows")!.Count);
        first.State.SetValue("Lines[0].Quantity", 1);
        first.State.MoveArrayItem("Lines", 0, 1);
        Sync(a, b);
        Assert.AreEqual(Json(first.Model), Json(second.Model));
    }

    [TestMethod]
    public void Nothing_is_echoed_back()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Description = "text", Lines = [new() { Product = "A" }] });
        Sync(a, b);
        using var second = new Replica(b);

        int localUpdatesA = 0, localUpdatesB = 0;
        a.Updated += update => { if (!Equals(update.Origin, "sync")) localUpdatesA++; };
        b.Updated += update => { if (!Equals(update.Origin, "sync")) localUpdatesB++; };
        int fieldChangesB = 0;
        second.State.FieldChanged += _ => fieldChangesB++;

        first.State.SetValue("Title", "one");
        first.State.SetValue("Description", "texts");
        first.State.AddArrayItem(first.Lines, "Lines");
        first.State.SetValue("Lines[0].Notes", "n");
        Assert.AreEqual(4, localUpdatesA, "one transaction per change");

        Sync(a, b);
        Sync(a, b);
        Assert.AreEqual(0, localUpdatesB, "applying a change writes nothing back");
        Assert.AreEqual(4, localUpdatesA);
        Assert.AreEqual(Json(first.Model), Json(second.Model));
        Assert.IsTrue(fieldChangesB > 0);

        // Writing what the document already holds is no change.
        first.State.SetValue("Title", "one");
        first.Binding.Publish();
        Assert.AreEqual(4, localUpdatesA);
    }

    [TestMethod]
    public void Publish_writes_what_Borm_did_not_report()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Title = "Start", Lines = [new() { Product = "A" }] });
        Sync(a, b);
        using var second = new Replica(b);

        first.State.SetValue("Title", "Changed");
        first.State.AddArrayItem(first.Lines, "Lines");
        first.State.Reset();                     // raises no FieldChanged
        Sync(a, b);
        Assert.AreEqual("Changed", second.Model.Title);

        first.Binding.Publish();
        Sync(a, b);
        Assert.AreEqual("Start", second.Model.Title);
        Assert.AreEqual(Json(first.Model), Json(second.Model));
    }

    [TestMethod]
    public void Undo_tracking_the_binding_takes_back_only_the_local_users_change()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        using var first = new Replica(a, new BormTicket { Title = "Start", Description = "abc" });
        Sync(a, b);
        using var second = new Replica(b);

        using var undo = new BcrdtUndoManager(a.GetMap("ticket"), new BcrdtUndoManagerOptions { TrackedOrigins = [first.Binding.Origin] });
        first.State.SetValue("Title", "Mine");
        first.State.SetValue("Description", "abcd");
        second.State.SetValue("Estimate", 3);
        Sync(a, b);

        undo.Undo();
        Sync(a, b);

        foreach (Replica replica in new[] { first, second })
        {
            Assert.AreEqual("Start", replica.Model.Title);
            Assert.AreEqual("abc", replica.Model.Description);
            Assert.AreEqual(3, replica.Model.Estimate);
        }
    }

    private static BormDefinition GroupsForm()
    {
        BormDefinition form = BormBuilder.Create()
            .Field("Title", BormFieldType.Text)
            .Field("TitleLength", BormFieldType.Integer, f => f.Computed(context => (context.Value("Title") as string)?.Length ?? 0, "Title"))
            .ArrayOf("Groups", BormFieldType.Object, a => a.Items(min: 0))
            .Build();
        form.FindByPath("Groups")!.ItemTemplate = new BormFieldDefinition("item", BormFieldType.Object)
        {
            Children =
            {
                new BormFieldDefinition("Name", BormFieldType.Text),
                new BormFieldDefinition("Members", BormFieldType.Array)
                {
                    ItemTemplate = new BormFieldDefinition("item", BormFieldType.Object)
                    {
                        Children = { new BormFieldDefinition("Email", BormFieldType.Email) },
                    },
                },
            },
        };
        return form;
    }

    [TestMethod]
    public void A_dictionary_form_with_a_repeater_inside_a_row_converges_and_computes_for_itself()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BormDefinition form = GroupsForm();
        using var stateA = new BormState(form, new BormDictionaryDataAccessor());
        using var bindingA = new BcrdtBormBinding(stateA, a.GetMap("groups"));
        BormFieldDefinition groups = form.FindByPath("Groups")!;
        BormFieldDefinition members = form.FindByPath("Groups[0].Members")!;

        stateA.SetValue("Title", "Teams");
        stateA.AddArrayItem(groups, "Groups");
        stateA.SetValue("Groups[0].Name", "Core");
        stateA.AddArrayItem(members, "Groups[0].Members");
        stateA.SetValue("Groups[0].Members[0].Email", "ada@example.com");
        Sync(a, b);

        using var stateB = new BormState(form, new BormDictionaryDataAccessor());
        using var bindingB = new BcrdtBormBinding(stateB, b.GetMap("groups"));
        Assert.AreEqual("ada@example.com", stateB.GetValue("Groups[0].Members[0].Email"));
        Assert.AreEqual(5L, Convert.ToInt64(stateB.GetValue("TitleLength")));
        Assert.IsFalse(a.GetMap("groups").ContainsKey("TitleLength"), "a computed value is computed, not shared");

        // Concurrently: a member added on one side, the group renamed and a second group put in front of
        // it on the other.
        stateA.AddArrayItem(members, "Groups[0].Members");
        stateA.SetValue("Groups[0].Members[1].Email", "grace@example.com");
        stateB.InsertArrayItem(groups, "Groups", 0);
        stateB.SetValue("Groups[0].Name", "New");
        stateB.SetValue("Groups[1].Name", "Core team");
        Sync(a, b);

        foreach (BormState state in new[] { stateA, stateB })
        {
            Assert.AreEqual("New", state.GetValue("Groups[0].Name"));
            Assert.AreEqual("Core team", state.GetValue("Groups[1].Name"));
            Assert.AreEqual(2, state.ArrayCount("Groups[1].Members"));
            Assert.AreEqual("grace@example.com", state.GetValue("Groups[1].Members[1].Email"));
        }
    }

    [TestMethod]
    public void Collaborators_see_which_field_each_other_is_editing()
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = 1 });
        BcrdtSimulatedPeer[] peers = network.AddPeers(2);
        using var presenceA = new BcrdtPresenceService(peers[0].Awareness, peers[0].Doc);
        using var presenceB = new BcrdtPresenceService(peers[1].Awareness, peers[1].Doc);
        presenceA.SetUser(new BcrdtCollaboratorInfo("ada", "Ada", "#e5484d"));
        presenceB.SetUser(new BcrdtCollaboratorInfo("bob", "Bob", "#0090ff"));

        using var first = new Replica(peers[0].Doc, new BormTicket { Lines = [new() { Product = "A" }, new() { Product = "B" }] },
            new BcrdtBormOptions { Presence = presenceA });
        network.Flush();
        using var second = new Replica(peers[1].Doc, options: new BcrdtBormOptions { Presence = presenceB });
        network.Flush();

        int changes = 0;
        second.Binding.Changed += () => changes++;

        first.Binding.BeginEditing("Lines[1].Notes");
        network.Flush();

        BcrdtBormEditor editor = second.Binding.Editors.Single();
        Assert.AreEqual("Ada", editor.Collaborator.User.Name);
        Assert.AreEqual("Lines[1].Notes", editor.Path);
        Assert.AreEqual("Ada", second.Binding.EditorsOf("Lines[1].Notes").Single().User.Name);
        Assert.IsTrue(changes > 0);

        // The editor is named by row, so the path follows the row on every replica.
        second.State.InsertArrayItem(second.Lines, "Lines", 0);
        network.Flush();
        Assert.AreEqual("Lines[2].Notes", second.Binding.Editors.Single().Path);
        Assert.AreEqual("Lines[2].Notes", first.Binding.EditingPath);

        first.Binding.EndEditing();
        network.Flush();
        Assert.AreEqual(0, second.Binding.Editors.Count);
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public void Random_form_edits_converge_on_a_lossy_network(int seed)
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions
        {
            Seed = seed,
            DropProbability = 0.2,
            DuplicateProbability = 0.2,
            MaxLatencySteps = 4,
        });

        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        var random = new Random(seed);
        List<Replica> replicas = [new Replica(peers[0].Doc, new BormTicket { Description = "start", Lines = [new() { Product = "first" }] })];
        network.Flush();
        network.SynchronizeAll();
        replicas.Add(new Replica(peers[1].Doc));
        replicas.Add(new Replica(peers[2].Doc));

        List<string> history = [];
        try
        {
            for (int round = 0; round < 40; round++)
            {
                for (int i = 0; i < replicas.Count; i++)
                {
                    history.Add($"{peers[i].Name}: {RandomEdit(replicas[i], random)}");
                }
                network.Step();
            }

            foreach (Replica replica in replicas) replica.Binding.EndEditing();
            network.Flush();
            network.SynchronizeAll();

            BcrdtConvergenceHarness.AssertConverged(network);

            // A form opened afresh on the converged document reads what every form shows.
            using BcrdtDocument fresh = Doc(99);
            fresh.ApplyUpdate(peers[0].Doc.EncodeStateAsUpdate());
            using var reader = new Replica(fresh);
            foreach (Replica replica in replicas) Assert.AreEqual(Json(reader.Model), Json(replica.Model));
        }
        catch (Exception ex)
        {
            throw new AssertFailedException(
                $"Seed {seed} diverged. Last operations:\n{string.Join("\n", history.TakeLast(20))}", ex);
        }
        finally
        {
            foreach (Replica replica in replicas) replica.Dispose();
        }
    }

    private static string RandomEdit(Replica replica, Random random)
    {
        BormState state = replica.State;
        int rows = state.ArrayCount("Lines");
        string[] words = ["alpha", "beta", "gamma", "delta", ""];

        switch (random.Next(11))
        {
            case 0:
                string title = words[random.Next(words.Length)];
                state.SetValue("Title", title);
                return $"Title = {title}";
            case 1:
                int estimate = random.Next(100);
                state.SetValue("Estimate", estimate);
                return $"Estimate = {estimate}";
            case 2:
                var priority = (BormTicketPriority)random.Next(3);
                state.SetValue("Priority", priority);
                return $"Priority = {priority}";
            case 3:
                string typed = Type(replica.Model.Description, random);
                state.SetValue("Description", typed);
                return $"Description = {typed}";
            case 4:
                int at = state.InsertArrayItem(replica.Lines, "Lines", random.Next(rows + 1));
                state.SetValue($"Lines[{at}].Product", "p" + random.Next(1000));
                return $"insert row at {at}";
            case 5 when rows > 0:
                int removed = random.Next(rows);
                state.RemoveArrayItem("Lines", removed);
                return $"remove row {removed}";
            case 6 when rows > 1:
                int from = random.Next(rows), to = random.Next(rows);
                state.MoveArrayItem("Lines", from, to);
                return $"move row {from} -> {to}";
            case 7 when rows > 0:
                int row = random.Next(rows);
                string notes = Type(replica.Model.Lines[row].Notes, random);
                state.SetValue($"Lines[{row}].Notes", notes);
                return $"Lines[{row}].Notes = {notes}";
            case 8 when rows > 0:
                int edited = random.Next(rows);
                int quantity = random.Next(10);
                state.SetValue($"Lines[{edited}].Quantity", quantity);
                return $"Lines[{edited}].Quantity = {quantity}";
            case 9:
                string[] paths = ["Title", "Description", "Estimate", rows > 0 ? $"Lines[{random.Next(rows)}].Notes" : "Urgent"];
                string path = paths[random.Next(paths.Length)];
                replica.Binding.BeginEditing(path);
                return $"begin editing {path}";
            default:
                replica.Binding.EndEditing();
                return "end editing";
        }
    }

    private static string Type(string text, Random random)
    {
        int position = random.Next(text.Length + 1);
        if (text.Length > 0 && random.Next(3) == 0)
        {
            int length = Math.Min(text.Length - Math.Min(position, text.Length - 1), 1 + random.Next(3));
            position = Math.Min(position, text.Length - 1);
            return text.Remove(position, length);
        }
        return text.Insert(position, ((char)('a' + random.Next(26))).ToString());
    }
}
