using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>Forms as their EditContexts know them: fields, validations and their outcome, what validates and what shows messages.</summary>
[TestClass]
public class FormsTests : BunitContext
{
    private void UseBevTools(BevValueAccess? access = null) => Services.AddBitBevToolsServices(o =>
    {
        o.Enabled = true;
        o.ValueAccess = access;
    });

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private BevFormInfo Form()
    {
        Session.Components.Refresh(force: true);
        Session.Forms.Discover();
        return Assert.ContainsSingle(Session.Forms.Forms());
    }

    [TestMethod]
    public void A_form_is_found_with_its_owner_model_validator_displays_and_rules()
    {
        UseBevTools();
        Render<SignUpForm>(ps => ps.Add(p => p.WithValidator, true).Add(p => p.WithSummary, true));

        var form = Form();
        Assert.AreEqual("SignUpForm", form.OwnerName);
        Assert.AreEqual(nameof(SignUpModel), form.ModelType);
        CollectionAssert.AreEqual(new[] { "DataAnnotationsValidator" }, form.Validators.ToArray());
        CollectionAssert.AreEqual(new[] { "ValidationSummary" }, form.MessageDisplays.ToArray());
        CollectionAssert.AreEquivalent(new[] { "Email: Required, EmailAddress", "Code: StringLength" }, form.ValidationAttributes.ToArray());
        Assert.IsNull(form.LastValidationPassed);
    }

    [TestMethod]
    public void A_submit_records_the_validation_and_its_messages_and_typing_counts_changes_without_values()
    {
        UseBevTools();
        var cut = Render<SignUpForm>(ps => ps.Add(p => p.WithValidator, true).Add(p => p.WithSummary, true));
        Form();

        cut.Find("form").Submit();
        var invalid = Form();
        Assert.IsFalse(invalid.LastValidationPassed);
        Assert.AreEqual(1, invalid.Validations);
        var email = invalid.Fields.Single(f => f.Name == "Email");
        CollectionAssert.AreEqual(new[] { "The Email field is required." }, email.Messages.ToArray());
        var validated = Session.Timeline.Snapshot().Last(e => e.Category == "form");
        Assert.AreEqual("Form validated: SignUpModel, 1 message", validated.Title);
        Assert.AreEqual("SignUpForm", validated.ComponentName);

        cut.Find("input").Change("ada@example.com");
        cut.Find("input").Change("ada@example.org");
        cut.Find("form").Submit();
        var valid = Form();
        Assert.IsTrue(valid.LastValidationPassed);
        Assert.AreEqual(2, valid.Fields.Single(f => f.Name == "Email").Changes);
        Assert.IsTrue(valid.Modified);
        Assert.DoesNotContain("ada@example", System.Text.Json.JsonSerializer.Serialize(valid), StringComparison.Ordinal);

        // The export carries the panel's view of the form, values included in none of it.
        var export = BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Indented = false, Sections = new HashSet<string> { "forms" } });
        StringAssert.Contains(export, "\"model\":\"SignUpModel\"");
        StringAssert.Contains(export, "\"lastValidationPassed\":true");
        Assert.DoesNotContain("ada@example", export, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Validation_attributes_nothing_checks_are_reported()
    {
        UseBevTools();
        var cut = Render<SignUpForm>(ps => ps.Add(p => p.WithValidator, false).Add(p => p.WithSummary, true));
        cut.Find("form").Submit();

        Assert.IsEmpty(Form().Validators, "BevTools' own handler is not a validator");
        Assert.IsTrue(Form().LastValidationPassed, "nothing validated it, so it passed");
        Session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "forms.validation", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("SignUpForm: SignUpModel's validation attributes are never checked", finding.Title);
        StringAssert.Contains(finding.Evidence, "Email: Required, EmailAddress");
        StringAssert.Contains(finding.Suggestion, "<DataAnnotationsValidator />");
        StringAssert.Contains(Session.Timeline.Snapshot().Last(e => e.Category == "form").Detail, "nothing validates it");
    }

    [TestMethod]
    public void The_frameworks_form_mapping_is_not_a_validator()
    {
        // Found in a browser: an EditForm with a FormName on a server gets a form-mapping subscription that reports
        // form-post binding errors and never checks the model's attributes.
        UseBevTools();
        Render<SignUpForm>(ps => ps.Add(p => p.WithValidator, false).Add(p => p.WithSummary, true));
        Form();
        var context = (EditContext)typeof(EditForm).GetField("_editContext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(Session.Components.All.Single(r => r.Component is EditForm).Component)!;
        Microsoft.AspNetCore.Components.Forms.Mapping.FakeMappingSubscriptions.Attach(context);

        Assert.IsEmpty(Form().Validators);
        Session.Diagnostics.Evaluate(force: true);
        Assert.Contains(f => f.Diagnostic.Title.EndsWith("validation attributes are never checked", StringComparison.Ordinal), Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void Failed_validation_with_nothing_to_show_it_is_reported()
    {
        UseBevTools();
        var cut = Render<SignUpForm>(ps => ps.Add(p => p.WithValidator, true).Add(p => p.WithSummary, false));
        cut.Find("form").Submit();

        Assert.IsEmpty(Form().MessageDisplays, "the input listens for its CSS class, which is not a display");
        Session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "forms.validation", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("SignUpForm: validation fails and no message is displayed", finding.Title);
        Assert.AreEqual(BevDiagnosticSeverity.Info, finding.Severity);
    }

    [TestMethod]
    public void Without_value_access_messages_are_hidden()
    {
        UseBevTools(BevValueAccess.None);
        var cut = Render<SignUpForm>(ps => ps.Add(p => p.WithValidator, true).Add(p => p.WithSummary, true));
        cut.Find("form").Submit();

        var email = Form().Fields.Single(f => f.Name == "Email");
        CollectionAssert.AreEqual(new[] { BevRedactor.HiddenValue }, email.Messages.ToArray());
    }

    [TestMethod]
    public void A_form_that_went_away_is_forgotten_and_not_reported()
    {
        UseBevTools();
        var cut = Render<FormHost>(ps => ps.Add(p => p.Show, true));
        Assert.AreEqual(nameof(SignUpModel), Form().ModelType);

        // Its validator unsubscribes as it is disposed: the gone form must not read as one nothing validates.
        cut.Render(ps => ps.Add(p => p.Show, false));
        Session.Components.Refresh(force: true);
        Session.Forms.Discover();
        Assert.IsEmpty(Session.Forms.Forms());
        Session.Diagnostics.Evaluate(force: true);
        Assert.DoesNotContain(f => f.Diagnostic.RuleId == "forms.validation", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void BevTools_keeps_no_form_alive()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<BevToolsSession>();
        session.EnsureActivated();
        using var renderer = new PlainRenderer(scope.ServiceProvider);

        var model = ShowAndRemoveForm(renderer, session);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.IsFalse(model.IsAlive, "the model and its EditContext go with the form");
    }

    // Not inlined, so nothing of the form is left on this frame's stack.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference ShowAndRemoveForm(PlainRenderer renderer, BevToolsSession session)
    {
        var root = renderer.RenderRoot<FormHost>(new Dictionary<string, object?> { [nameof(FormHost.Show)] = true });
        session.Components.Refresh(force: true);
        session.Forms.Discover();
        Assert.ContainsSingle(session.Forms.Forms());
        var model = new WeakReference(FormHost.LastModel);
        FormHost.LastModel = null;
        renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(FormHost.Show)] = false });
        renderer.SetRootParameters(root, new Dictionary<string, object?> { [nameof(FormHost.Show)] = false });
        session.Components.Refresh(force: true);
        session.Forms.Discover();
        return model;
    }

    [TestMethod]
    public void A_form_reset_after_submit_is_still_watched()
    {
        UseBevTools();
        var cut = Render<ResettingForm>();
        Form();

        // Each valid submit gives the EditForm a new model, and with it a new EditContext.
        for (var i = 0; i < 3; i++)
        {
            cut.Find("form").Submit();
        }

        Assert.AreEqual(3, Session.Timeline.Snapshot().Count(e => e.Category == "form"));
        Assert.AreEqual(3, cut.Instance.Resets);
        Assert.IsNull(Form().LastValidationPassed, "the form listed is the current one, not yet validated");
    }

    [TestMethod]
    public void A_form_whose_submit_the_code_handles_is_not_called_unchecked()
    {
        UseBevTools();
        Render<SubmitHandlingForm>();

        var form = Form();
        Assert.IsTrue(form.HandlesSubmit);
        Session.Diagnostics.Evaluate(force: true);
        Assert.DoesNotContain(f => f.Diagnostic.RuleId == "forms.validation", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void The_forms_panel_lists_the_form()
    {
        UseBevTools();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
        Session.Ui.IsOpen = true;
        var panel = Render<BevToolsPanel>();
        Render<SignUpForm>(ps => ps.Add(p => p.WithValidator, false).Add(p => p.WithSummary, false));
        panel.InvokeAsync(() => panel.Instance.ShowPanel("forms"));

        panel.WaitForAssertion(() =>
        {
            StringAssert.Contains(panel.Find(".bev-body").TextContent, "SignUpModel");
            StringAssert.Contains(panel.Find(".bev-body").TextContent, "never checked");
        });
    }
}

public class SignUpModel
{
    [Required, EmailAddress]
    public string? Email { get; set; }

    [StringLength(5)]
    public string? Code { get; set; }

    public string? Note { get; set; }
}

public class FormHost : ComponentBase
{
    public static object? LastModel;

    [Parameter] public bool Show { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Show)
        {
            builder.OpenComponent<SignUpForm>(0);
            builder.AddComponentParameter(1, nameof(SignUpForm.WithValidator), true);
            builder.AddComponentReferenceCapture(2, form => LastModel = ((SignUpForm)form).Model);
            builder.CloseComponent();
        }
    }
}

public class ResettingForm : ComponentBase
{
    private SignUpModel _model = new() { Email = "ada@example.com" };

    public int Resets { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<EditForm>(0);
        builder.AddComponentParameter(1, nameof(EditForm.Model), _model);
        builder.AddComponentParameter(2, nameof(EditForm.OnValidSubmit), EventCallback.Factory.Create<EditContext>(this, () =>
        {
            _model = new SignUpModel { Email = "ada@example.com" };
            Resets++;
        }));
        builder.AddComponentParameter(3, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => b =>
        {
            b.OpenComponent<DataAnnotationsValidator>(0);
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }
}

public class SubmitHandlingForm : ComponentBase
{
    private readonly SignUpModel _model = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<EditForm>(0);
        builder.AddComponentParameter(1, nameof(EditForm.Model), _model);
        // Validated by hand, as some forms are: Validator.TryValidateObject and a ValidationMessageStore.
        builder.AddComponentParameter(2, nameof(EditForm.OnSubmit), EventCallback.Factory.Create<EditContext>(this, _ => { }));
        builder.AddComponentParameter(3, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => _ => { }));
        builder.CloseComponent();
    }
}

public class SignUpForm : ComponentBase
{
    private readonly SignUpModel _model = new();

    public SignUpModel Model => _model;

    [Parameter] public bool WithValidator { get; set; }

    [Parameter] public bool WithSummary { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<EditForm>(0);
        builder.AddComponentParameter(1, nameof(EditForm.Model), _model);
        builder.AddComponentParameter(2, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => b =>
        {
            if (WithValidator)
            {
                b.OpenComponent<DataAnnotationsValidator>(0);
                b.CloseComponent();
            }

            b.OpenComponent<InputText>(1);
            b.AddComponentParameter(2, nameof(InputText.Value), _model.Email);
            b.AddComponentParameter(3, nameof(InputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => _model.Email = value));
            b.AddComponentParameter(4, nameof(InputText.ValueExpression), (Expression<Func<string?>>)(() => _model.Email));
            b.CloseComponent();
            if (WithSummary)
            {
                b.OpenComponent<ValidationSummary>(5);
                b.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }
}

