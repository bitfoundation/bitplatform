using FluentValidation;

namespace Bit.Borm.Tests;

/// <summary>
/// A form has one external-validator slot, and every integration writes to it. Wiring up
/// FluentValidation <em>and</em> the model's own <c>IValidatableObject</c> is an entirely reasonable
/// thing to want — they cover different rules — and it used to mean whichever line came second
/// silently threw the first away, leaving a form with half its validation and nothing to say why.
/// </summary>
[TestClass]
public class CombinedExternalValidatorTests
{
    private sealed class BookingFluentValidator : AbstractValidator<BookingRequest>
    {
        public BookingFluentValidator()
            => RuleFor(x => x.Reference).NotEmpty().WithMessage("A reference is required.");
    }

    private static BookingRequest Bad() => new()
    {
        Reference = "",                          // the FluentValidation rule
        Start = new DateOnly(2026, 5, 10),
        End = new DateOnly(2026, 5, 1)           // the IValidatableObject rule
    };

    private static BormState State(BookingRequest model)
        => new(BormSchemaGenerator.Generate<BookingRequest>(), new BormModelDataAccessor(model));

    [TestMethod]
    public async Task Both_layers_run_whichever_order_they_are_wired_in()
    {
        var first = State(Bad()).UseDataAnnotations().UseFluentValidation(new BookingFluentValidator());
        Assert.IsFalse(await first.ValidateAsync());
        Assert.Contains(m => m.Message.Contains("end date", StringComparison.Ordinal), first.MessagesFor("End"));
        Assert.Contains(m => m.Message.Contains("reference is required", StringComparison.OrdinalIgnoreCase), first.MessagesFor("Reference"));

        var second = State(Bad()).UseFluentValidation(new BookingFluentValidator()).UseDataAnnotations();
        Assert.IsFalse(await second.ValidateAsync());
        Assert.Contains(m => m.Message.Contains("end date", StringComparison.Ordinal), second.MessagesFor("End"));
        Assert.Contains(m => m.Message.Contains("reference is required", StringComparison.OrdinalIgnoreCase), second.MessagesFor("Reference"));
    }

    [TestMethod]
    public async Task The_same_complaint_from_both_layers_is_read_once()
    {
        // Two layers can legitimately report the same problem on the same field in the same words.
        var state = State(Bad());
        state.ExternalValidator = state.ExternalValidator
            .CombineWith((_, _, _) => new ValueTask<IReadOnlyList<BormValidationMessage>>(
                (IReadOnlyList<BormValidationMessage>)[new BormValidationMessage("Reference", "Say it once.")]))
            .CombineWith((_, _, _) => new ValueTask<IReadOnlyList<BormValidationMessage>>(
                (IReadOnlyList<BormValidationMessage>)[new BormValidationMessage("Reference", "Say it once.")]));

        await state.ValidateAsync();

        Assert.ContainsSingle(m => m.Message == "Say it once.", state.MessagesFor("Reference"));
    }

    [TestMethod]
    public void Combining_with_nothing_hands_the_validator_straight_back()
    {
        BormExternalValidator only = (_, _, _) =>
            new ValueTask<IReadOnlyList<BormValidationMessage>>(Array.Empty<BormValidationMessage>());

        Assert.AreSame(only, ((BormExternalValidator?)null).CombineWith(only));
    }
}
