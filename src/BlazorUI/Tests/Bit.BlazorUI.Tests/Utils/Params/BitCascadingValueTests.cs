using System;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Params;

[TestClass]
public class BitCascadingValueTests
{
    [TestMethod]
    public void ShouldInferValueTypeFromTheRuntimeTypeOfTheValue()
    {
        var cascadingValue = new BitCascadingValue(5);

        Assert.AreEqual(typeof(int), cascadingValue.ValueType);
        Assert.AreEqual(5, cascadingValue.Value);
        Assert.IsNull(cascadingValue.Name);
        Assert.IsFalse(cascadingValue.IsFixed);
        Assert.IsTrue(cascadingValue.Enabled);
    }

    [TestMethod]
    public void ShouldUseTheExplicitValueTypeWhenProvided()
    {
        var cascadingValue = new BitCascadingValue(5, typeof(int?));

        Assert.AreEqual(typeof(int?), cascadingValue.ValueType);
    }

    [TestMethod]
    public void ShouldThrowWhenBothTheValueAndTheValueTypeAreMissing()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BitCascadingValue(null));
    }

    [TestMethod]
    public void ShouldThrowWhenTheValueIsNotAssignableToTheValueType()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BitCascadingValue("not-a-number", typeof(int)));
    }

    [TestMethod]
    public void ShouldThrowWhenANullValueIsCascadedAsANonNullableValueType()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BitCascadingValue(null, typeof(int)));
    }

    [TestMethod]
    public void ShouldAcceptANullValueForANullableValueType()
    {
        var cascadingValue = new BitCascadingValue(null, typeof(int?));

        Assert.IsNull(cascadingValue.Value);
        Assert.AreEqual(typeof(int?), cascadingValue.ValueType);
    }

    [TestMethod]
    public void ShouldAcceptANullValueForAReferenceType()
    {
        var cascadingValue = new BitCascadingValue(null, typeof(string));

        Assert.IsNull(cascadingValue.Value);
        Assert.AreEqual(typeof(string), cascadingValue.ValueType);
    }

    [TestMethod]
    public void ShouldAcceptABoxedValueOfTheUnderlyingTypeOfANullableValueType()
    {
        var cascadingValue = new BitCascadingValue(5, typeof(int?));

        Assert.AreEqual(5, cascadingValue.Value);
    }

    [TestMethod]
    public void ShouldAcceptAValueOfADerivedTypeForABaseValueType()
    {
        var cascadingValue = new BitCascadingValue(new CascadingDemoServiceDecorator(), typeof(ICascadingDemoService));

        Assert.AreEqual(typeof(ICascadingDemoService), cascadingValue.ValueType);
    }

    [TestMethod]
    public void ShouldValidateTheValueWhenItIsAssignedAfterConstruction()
    {
        var cascadingValue = new BitCascadingValue(5);

        Assert.ThrowsExactly<ArgumentException>(() => cascadingValue.Value = "not-a-number");

        cascadingValue.Value = 7;

        Assert.AreEqual(7, cascadingValue.Value);
    }

    [TestMethod]
    public void ShouldThrowWhenANullValueIsAssignedToANonNullableValueType()
    {
        var cascadingValue = new BitCascadingValue(5);

        Assert.ThrowsExactly<ArgumentException>(() => cascadingValue.Value = null);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow(null)]
    public void ShouldNormalizeAnEmptyNameToNull(string? name)
    {
        var cascadingValue = new BitCascadingValue(5, name);

        Assert.IsNull(cascadingValue.Name);

        cascadingValue.Name = name;

        Assert.IsNull(cascadingValue.Name);
    }

    [TestMethod]
    public void ShouldKeepANonEmptyName()
    {
        var cascadingValue = new BitCascadingValue(5, "Number");

        Assert.AreEqual("Number", cascadingValue.Name);
    }

    [TestMethod]
    public void ShouldCreateATypedValueUsingFrom()
    {
        var cascadingValue = BitCascadingValue.From<int?>(5);

        Assert.AreEqual(typeof(int?), cascadingValue.ValueType);
        Assert.AreEqual(5, cascadingValue.Value);
        Assert.IsNull(cascadingValue.Name);
        Assert.IsFalse(cascadingValue.IsFixed);
    }

    [TestMethod]
    public void ShouldCreateANamedTypedValueUsingFrom()
    {
        var cascadingValue = BitCascadingValue.From<string?>(null, "Greeting");

        Assert.AreEqual(typeof(string), cascadingValue.ValueType);
        Assert.IsNull(cascadingValue.Value);
        Assert.AreEqual("Greeting", cascadingValue.Name);
    }

    [TestMethod]
    public void ShouldCreateAFixedTypedValueUsingFrom()
    {
        var cascadingValue = BitCascadingValue.From(5, true);

        Assert.AreEqual(typeof(int), cascadingValue.ValueType);
        Assert.IsTrue(cascadingValue.IsFixed);
    }

    [TestMethod]
    public void ShouldCreateAFixedTypedValueUsingFixed()
    {
        var cascadingValue = BitCascadingValue.Fixed<ICascadingDemoService>(new CascadingDemoService(), "Service");

        Assert.AreEqual(typeof(ICascadingDemoService), cascadingValue.ValueType);
        Assert.AreEqual("Service", cascadingValue.Name);
        Assert.IsTrue(cascadingValue.IsFixed);
    }

    [TestMethod]
    public void ShouldCascadeNullableValueTypesThroughTheImplicitOperators()
    {
        BitCascadingValue nullableInt = (int?)null;
        BitCascadingValue namedNullableBool = ((bool?)null, "Flag");
        BitCascadingValue nullableString = (string?)null;

        Assert.AreEqual(typeof(int?), nullableInt.ValueType);
        Assert.AreEqual(typeof(bool?), namedNullableBool.ValueType);
        Assert.AreEqual("Flag", namedNullableBool.Name);
        Assert.AreEqual(typeof(string), nullableString.ValueType);
    }

    [TestMethod]
    public void ShouldCascadeNonNullableValuesThroughTheImplicitOperators()
    {
        BitCascadingValue number = 5;
        BitCascadingValue namedText = ("hi", "Greeting");

        Assert.AreEqual(typeof(int), number.ValueType);
        Assert.AreEqual(5, number.Value);
        Assert.AreEqual(typeof(string), namedText.ValueType);
        Assert.AreEqual("Greeting", namedText.Name);
    }

    [TestMethod]
    public void ShouldCascadeReferenceTypesThroughTheImplicitOperators()
    {
        BitCascadingValue uri = new Uri("https://bitplatform.dev");
        BitCascadingValue nullUri = (Uri?)null;
        BitCascadingValue routeData = (RouteData?)null;
        BitCascadingValue dir = BitDir.Rtl;

        Assert.AreEqual(typeof(Uri), uri.ValueType);
        Assert.AreEqual(typeof(Uri), nullUri.ValueType);
        Assert.AreEqual(typeof(RouteData), routeData.ValueType);
        Assert.AreEqual(typeof(BitDir), dir.ValueType);
    }

    [TestMethod]
    public void ShouldRenderAReadableStringRepresentation()
    {
        Assert.AreEqual("Int32 = 5", new BitCascadingValue(5).ToString());
        Assert.AreEqual("Greeting: String = hi", new BitCascadingValue("hi", "Greeting").ToString());
        Assert.AreEqual("String = null", new BitCascadingValue(null, typeof(string)).ToString());
    }
}
