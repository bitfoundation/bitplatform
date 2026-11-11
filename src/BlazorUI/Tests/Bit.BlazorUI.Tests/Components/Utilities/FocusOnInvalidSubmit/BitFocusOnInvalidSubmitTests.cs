using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.FocusOnInvalidSubmit;

/// <summary>
/// Covers what BitFocusOnInvalidSubmit does on the .NET side: it renders nothing but a hidden marker, and asks the
/// browser to focus the first invalid field of its form (handing it that marker) after every validation of the whole
/// form that fails - a submit, or one the app asks for - and after nothing else. Which element the browser picks is
/// covered by its E2E tests.
/// </summary>
[TestClass]
public class BitFocusOnInvalidSubmitTests : BunitTestContext
{
    private const string FocusFirstInvalid = "BitBlazorUI.Utils.focusFirstInvalid";

    [TestInitialize]
    public void SetupJsInteropMode()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void BitFocusOnInvalidSubmitShouldRenderOnlyAHiddenMarker()
    {
        var component = RenderForm(new FormModel());

        var marker = component.FindComponent<BitFocusOnInvalidSubmit>();

        // Nothing to see and nothing to lay out: a grid or a flex row of fields is left as it is.
        Assert.AreEqual(1, marker.Nodes.Length);
        Assert.HasCount(1, component.FindAll("form > span[hidden]"));
        Assert.AreEqual(string.Empty, component.Find("form > span[hidden]").TextContent);
    }

    [TestMethod]
    public void BitFocusOnInvalidSubmitShouldAskForTheFocusAfterAnInvalidSubmit()
    {
        var invalidCount = 0;
        var component = RenderForm(new FormModel(), onInvalid: () => invalidCount++);

        component.Find("form").Submit();

        component.WaitForAssertion(() => Assert.AreEqual(1, FocusCalls()));
        Assert.AreEqual(1, invalidCount);

        // The marker is what the browser finds the form by.
        var marker = component.Find("form > span[hidden]");
        var argument = (ElementReference)Context.JSInterop.Invocations.Single(i => i.Identifier == FocusFirstInvalid).Arguments[0]!;

        Assert.AreEqual(marker.GetAttribute("blazor:elementReference"), argument.Id);

        // Every failed submit asks again: the field the user left the focus on may not be the first invalid one.
        component.Find("form").Submit();

        component.WaitForAssertion(() => Assert.AreEqual(2, FocusCalls()));
    }

    [TestMethod]
    public void BitFocusOnInvalidSubmitShouldLeaveTheFocusAloneAfterAValidSubmit()
    {
        var validCount = 0;
        var component = RenderForm(new FormModel { Name = "Ada" }, onValid: () => validCount++);
        var marker = component.FindComponent<BitFocusOnInvalidSubmit>();

        component.Find("form").Submit();

        // It looks once the validation is over - the render it asks for is the sign it has - and finds nothing wrong.
        component.WaitForAssertion(() => Assert.AreEqual(2, marker.RenderCount));
        Assert.AreEqual(1, validCount);
        Assert.AreEqual(0, FocusCalls());
    }

    [TestMethod]
    public void BitFocusOnInvalidSubmitShouldLeaveTheFocusAloneWhileAFieldIsEdited()
    {
        var model = new FormModel();
        var component = RenderForm(model);
        var marker = component.FindComponent<BitFocusOnInvalidSubmit>();

        // A field is validated on its own as it changes, which leaves it invalid here - and leaves the focus where the
        // user is typing.
        component.Find("input").Change("Ada");
        component.Find("input").Change(string.Empty);

        Assert.AreEqual("true", component.Find("input").GetAttribute("aria-invalid"));
        Assert.AreEqual(1, marker.RenderCount);
        Assert.AreEqual(0, FocusCalls());
    }

    [TestMethod]
    public void BitFocusOnInvalidSubmitShouldWaitForTheValidatorsThatComeAfterIt()
    {
        // Placed ahead of the validator, it hears of the validation before the validator has run.
        var component = RenderForm(new FormModel(), helperFirst: true);

        component.Find("form").Submit();

        component.WaitForAssertion(() => Assert.AreEqual(1, FocusCalls()));
    }

    [TestMethod]
    public async Task BitFocusOnInvalidSubmitShouldAskForTheFocusAfterAValidationTheAppRequests()
    {
        var model = new FormModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitFocusOnInvalidSubmit>(0);
                builder.CloseComponent();
            }));
        });

        await component.InvokeAsync(() => editContext.Validate());

        component.WaitForAssertion(() => Assert.AreEqual(1, FocusCalls()));
    }

#if NET11_0_OR_GREATER
    [TestMethod]
    public async Task BitFocusOnInvalidSubmitShouldWaitForAnAsyncValidatorToFinish()
    {
        var model = new FormModel { Name = "Ada" };
        var editContext = new EditContext(model);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AddAsyncValidator(editContext, gate.Task, "That name is taken.");

        var component = RenderHelper(editContext);
        var marker = component.FindComponent<BitFocusOnInvalidSubmit>();

        var validation = component.InvokeAsync(() => editContext.ValidateAsync(TestContext.CancellationToken));

        // While the validator is still out, there is nothing to look at yet.
        await component.InvokeAsync(() => { });

        Assert.IsTrue(editContext.IsValidationPending());
        Assert.AreEqual(1, marker.RenderCount);
        Assert.AreEqual(0, FocusCalls());

        gate.SetResult();
        Assert.IsFalse(await validation);

        // Its message is in once the validation is over, and that is when the focus is asked for.
        component.WaitForAssertion(() => Assert.AreEqual(1, FocusCalls()));
    }

    [TestMethod]
    public async Task BitFocusOnInvalidSubmitShouldLeaveTheFocusInAFieldEditedWhileAValidationRuns()
    {
        var model = new FormModel { Name = "Ada" };
        var editContext = new EditContext(model);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AddAsyncValidator(editContext, gate.Task, "That name is taken.");

        var component = RenderHelper(editContext);
        var marker = component.FindComponent<BitFocusOnInvalidSubmit>();

        var validation = component.InvokeAsync(() => editContext.ValidateAsync(TestContext.CancellationToken));

        // The user goes back to a field before the validator answers: they have moved on from the submit.
        await component.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(FormModel.Name))));

        gate.SetResult();
        Assert.IsFalse(await validation);
        await component.InvokeAsync(() => { });

        Assert.IsNotEmpty(editContext.GetValidationMessages());
        Assert.AreEqual(1, marker.RenderCount);
        Assert.AreEqual(0, FocusCalls());
    }

#endif

    [TestMethod]
    public async Task BitFocusOnInvalidSubmitShouldFollowANewEditContext()
    {
        var first = new EditContext(new FormModel());
        var second = new EditContext(new FormModel());
        first.EnableDataAnnotationsValidation(Context.Services);
        second.EnableDataAnnotationsValidation(Context.Services);

        RenderFragment content = builder =>
        {
            builder.OpenComponent<BitFocusOnInvalidSubmit>(0);
            builder.CloseComponent();
        };

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, first);
            parameters.Add(p => p.ChildContent, content);
        });

        // A form given a new model hands down a new EditContext.
        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, second);
            parameters.Add(p => p.ChildContent, content);
        });

        var marker = component.FindComponent<BitFocusOnInvalidSubmit>();
        var renders = marker.RenderCount;

        await component.InvokeAsync(() => first.Validate());

        // The old one is no longer listened to: nothing is even looked at.
        Assert.AreEqual(renders, marker.RenderCount);
        Assert.AreEqual(0, FocusCalls());

        await component.InvokeAsync(() => second.Validate());

        component.WaitForAssertion(() => Assert.AreEqual(1, FocusCalls()));
    }

    [TestMethod]
    public async Task BitFocusOnInvalidSubmitShouldStopListeningOnceRemoved()
    {
        var editContext = new EditContext(new FormModel());
        editContext.EnableDataAnnotationsValidation(Context.Services);

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitFocusOnInvalidSubmit>(0);
                builder.CloseComponent();
            }));
        });

        component.Render(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(_ => { }));
        });

        await component.InvokeAsync(() => editContext.Validate());

        Assert.IsEmpty(component.FindComponents<BitFocusOnInvalidSubmit>());
        Assert.AreEqual(0, FocusCalls());
    }

    [TestMethod]
    public void BitFocusOnInvalidSubmitShouldRequireAnEditContext()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RenderComponent<BitFocusOnInvalidSubmit>());

        Assert.Contains("EditContext", exception.Message);
    }

    private IRenderedComponent<EditForm> RenderForm(FormModel model, Action? onValid = null, Action? onInvalid = null, bool helperFirst = false)
    {
        return RenderComponent<EditForm>(parameters =>
        {
            parameters.Add(p => p.Model, model);
            parameters.Add(p => p.OnValidSubmit, () => onValid?.Invoke());
            parameters.Add(p => p.OnInvalidSubmit, () => onInvalid?.Invoke());
            parameters.Add(p => p.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                if (helperFirst)
                {
                    builder.OpenComponent<BitFocusOnInvalidSubmit>(0);
                    builder.CloseComponent();
                }

                builder.OpenComponent<DataAnnotationsValidator>(1);
                builder.CloseComponent();

                if (helperFirst is false)
                {
                    builder.OpenComponent<BitFocusOnInvalidSubmit>(2);
                    builder.CloseComponent();
                }

                builder.OpenComponent<BitTextField>(3);
                builder.AddAttribute(4, nameof(BitTextField.Value), model.Name);
                builder.AddAttribute(5, nameof(BitTextField.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.Name = v));
                builder.AddAttribute(6, nameof(BitTextField.ValueExpression), (Expression<Func<string?>>)(() => model.Name));
                builder.CloseComponent();
            }));
        });
    }

    private IRenderedComponent<CascadingValue<EditContext>> RenderHelper(EditContext editContext)
    {
        return RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitFocusOnInvalidSubmit>(0);
                builder.CloseComponent();
            }));
        });
    }

#if NET11_0_OR_GREATER
    // A validator that answers the request by registering work that only finishes once the gate opens - a server asked
    // whether a name is taken - and reports the message it found then.
    private static void AddAsyncValidator(EditContext editContext, Task gate, string message)
    {
        var store = new ValidationMessageStore(editContext);

        editContext.OnValidationRequested += (_, args) => args.AddAsyncValidator(async _ =>
        {
            await gate;
            store.Add(editContext.Field(nameof(FormModel.Name)), message);
            editContext.NotifyValidationStateChanged();
        });
    }
#endif

    private int FocusCalls() => Context.JSInterop.Invocations.Count(i => i.Identifier == FocusFirstInvalid);

    private sealed class FormModel
    {
        [Required]
        public string? Name { get; set; }
    }

#if NET11_0_OR_GREATER
    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
#endif
}
