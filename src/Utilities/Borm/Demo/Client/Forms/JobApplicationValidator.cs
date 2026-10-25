using FluentValidation;

namespace Bit.Borm.Demo.Client.Forms;

/// <summary>
/// A standard FluentValidation validator. Borm runs it against the bound model and maps the
/// failures onto the matching fields by property name.
/// </summary>
public class JobApplicationValidator : AbstractValidator<JobApplication>
{
    public JobApplicationValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(40);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.YearsExperience).InclusiveBetween(0, 60);

        RuleFor(x => x.DesiredSalary)
            .GreaterThan(0)
            .LessThanOrEqualTo(500_000).WithMessage("Let's keep it realistic (≤ 500,000).");

        // Cross-field rule: senior candidates must explain their motivation.
        RuleFor(x => x.Motivation)
            .NotEmpty()
            .MinimumLength(20)
            .When(x => x.YearsExperience >= 5)
            .WithMessage("Candidates with 5+ years should write at least 20 characters.");
    }
}
