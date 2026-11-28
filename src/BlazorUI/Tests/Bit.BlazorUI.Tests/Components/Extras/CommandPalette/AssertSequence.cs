using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// Element-by-element equality for sequences, so an expected value can be written as a collection
/// expression and compared against whatever enumerable the code under test returns.
/// </summary>
internal static class AssertSequence
{
    public static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        => CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
}
