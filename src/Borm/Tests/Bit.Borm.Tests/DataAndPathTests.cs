using Bit.Borm;

namespace Bit.Borm.Tests;

[TestClass]
public class FormPathTests
{
    [TestMethod]
    public void Parses_dotted_and_indexed_paths()
    {
        var segments = BormPath.Parse("items[2].product");
        Assert.AreEqual(3, segments.Count);
        Assert.AreEqual("items", segments[0].Name);
        Assert.IsTrue(segments[1].IsIndex);
        Assert.AreEqual(2, segments[1].Index);
        Assert.AreEqual("product", segments[2].Name);
    }

    [TestMethod]
    public void Combine_builds_paths()
    {
        Assert.AreEqual("a.b", BormPath.Combine("a", "b"));
        Assert.AreEqual("b", BormPath.Combine("", "b"));
        Assert.AreEqual("a[3]", BormPath.Combine("a", 3));
    }
}

[TestClass]
public class DictionaryDataAccessorTests
{
    [TestMethod]
    public void Sets_and_gets_nested_values()
    {
        var data = new BormDictionaryDataAccessor();
        data.SetValue("address.city", "Paris");
        Assert.AreEqual("Paris", data.GetValue("address.city"));
    }

    [TestMethod]
    public void Sets_and_gets_array_values()
    {
        var data = new BormDictionaryDataAccessor();
        data.SetValue("items[0].product", "Widget");
        data.SetValue("items[1].product", "Gadget");
        Assert.AreEqual("Widget", data.GetValue("items[0].product"));
        Assert.AreEqual("Gadget", data.GetValue("items[1].product"));
    }

    [TestMethod]
    public void Missing_paths_return_null()
    {
        var data = new BormDictionaryDataAccessor();
        Assert.IsNull(data.GetValue("nope.missing"));
        Assert.IsNull(data.GetValue("arr[5]"));
    }
}

[TestClass]
public class ModelDataAccessorTests
{
    [TestMethod]
    public void Reads_and_writes_nested_properties()
    {
        var model = new RegistrationModel();
        var data = new BormModelDataAccessor(model);

        data.SetValue("FirstName", "Ada");
        data.SetValue("Address.City", "London");

        Assert.AreEqual("Ada", model.FirstName);
        Assert.AreEqual("London", model.Address.City);
        Assert.AreEqual("Ada", data.GetValue("FirstName"));
        Assert.AreEqual("London", data.GetValue("Address.City"));
    }

    [TestMethod]
    public void Converts_string_to_target_type()
    {
        var model = new RegistrationModel();
        var data = new BormModelDataAccessor(model);
        data.SetValue("Age", "42");
        Assert.AreEqual(42, model.Age);
    }

    [TestMethod]
    public void Resolves_array_element_type()
    {
        var data = new BormModelDataAccessor(new RegistrationModel());
        Assert.AreEqual(typeof(LineItem), data.GetElementType("Items"));
    }
}
