using System.Collections.Generic;

namespace Bit.BlazeDb.Generators;

internal sealed class BlazeDbTableModel
{
    public string? Namespace;
    public string TypeName = "";
    public string TypeKeyword = "class";
    public string TableName = "";
    public string HintName = "";
    public BlazeDbPropModel? Key;

    /// <summary>
    /// The properties a deserialized row is constructed from, in the constructor's parameter order -
    /// empty when the type has a parameterless constructor and every stored property is set afterwards.
    /// </summary>
    public List<BlazeDbPropModel> ConstructorArguments = new List<BlazeDbPropModel>();
    public List<BlazeDbPropModel> Props = new List<BlazeDbPropModel>();
    public List<BlazeDbCompoundIndexModel> CompoundIndexes = new List<BlazeDbCompoundIndexModel>();
}
