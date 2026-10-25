using Bit.Borm;

namespace Bit.Borm.Tests;

[TestClass]
public class ConditionTests
{
    private static IBormDataReader Data(params (string, object?)[] values)
    {
        var d = new BormDictionaryDataAccessor();
        foreach (var (k, v) in values) d.SetValue(k, v);
        return d;
    }

    [TestMethod]
    [DataRow(BormConditionOperator.Equals, "Business", true)]
    [DataRow(BormConditionOperator.NotEquals, "Business", false)]
    [DataRow(BormConditionOperator.In, new[] { "Business", "Personal" }, true)]
    public void Evaluates_field_conditions(BormConditionOperator op, object value, bool expected)
    {
        var cond = new BormFieldCondition("AccountType", op, value);
        Assert.AreEqual(expected, cond.Evaluate(Data(("AccountType", "Business"))));
    }

    [TestMethod]
    public void Numeric_comparison_handles_mixed_types()
    {
        var cond = new BormFieldCondition("Age", BormConditionOperator.GreaterThanOrEqual, 18);
        Assert.IsTrue(cond.Evaluate(Data(("Age", 21))));
        Assert.IsFalse(cond.Evaluate(Data(("Age", 16))));
    }

    [TestMethod]
    public void Condition_group_combines_with_and_or()
    {
        var data = Data(("AccountType", "Business"), ("Age", 30));
        var all = BormConditionGroup.All(
            new BormFieldCondition("AccountType", BormConditionOperator.Equals, "Business"),
            new BormFieldCondition("Age", BormConditionOperator.GreaterThan, 18));
        var any = BormConditionGroup.Any(
            new BormFieldCondition("AccountType", BormConditionOperator.Equals, "Nope"),
            new BormFieldCondition("Age", BormConditionOperator.GreaterThan, 18));
        Assert.IsTrue(all.Evaluate(data));
        Assert.IsTrue(any.Evaluate(data));
    }
}

[TestClass]
public class BuiltInRuleTests
{
    private static BormValidationContext Ctx(object? value)
        => new("f", value, new BormDictionaryDataAccessor());

    [TestMethod]
    public async Task Required_fails_on_empty()
    {
        Assert.IsFalse((await new BormRequiredRule().ValidateAsync(Ctx(""))).IsValid);
        Assert.IsTrue((await new BormRequiredRule().ValidateAsync(Ctx("x"))).IsValid);
    }

    [TestMethod]
    public async Task Range_enforces_bounds()
    {
        var rule = new BormRangeRule(1, 10);
        Assert.IsFalse((await rule.ValidateAsync(Ctx(0))).IsValid);
        Assert.IsTrue((await rule.ValidateAsync(Ctx(5))).IsValid);
        Assert.IsFalse((await rule.ValidateAsync(Ctx(11))).IsValid);
    }

    [TestMethod]
    public async Task Email_rule_validates_format()
    {
        var rule = new BormEmailRule();
        Assert.IsTrue((await rule.ValidateAsync(Ctx("a@b.com"))).IsValid);
        Assert.IsFalse((await rule.ValidateAsync(Ctx("nope"))).IsValid);
    }
}

[TestClass]
public class FormValidatorTests
{
    [TestMethod]
    public async Task Validates_nested_objects_and_arrays()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var model = new RegistrationModel { FirstName = "", Email = "bad", Age = 5 };
        model.Items.Add(new LineItem { Product = "", Quantity = 0 });

        var messages = await new BormValidator().ValidateAsync(form, new BormModelDataAccessor(model));

        Assert.Contains(m => m.FieldPath == "FirstName", messages);
        Assert.Contains(m => m.FieldPath == "Email", messages);
        Assert.Contains(m => m.FieldPath == "Age", messages);
        Assert.Contains(m => m.FieldPath == "Items[0].Product", messages);
        Assert.Contains(m => m.FieldPath == "Items[0].Quantity", messages);
    }

    [TestMethod]
    public async Task Hidden_fields_are_not_validated()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.CompanyName, f => f
                .Required("Company required")
                .VisibleWhen("AccountType", BormConditionOperator.Equals, "Business"))
            .Build();

        var model = new RegistrationModel { AccountType = AccountType.Personal };
        var messages = await new BormValidator().ValidateAsync(form, new BormModelDataAccessor(model));

        Assert.DoesNotContain(m => m.FieldPath == "CompanyName", messages);
    }
}
