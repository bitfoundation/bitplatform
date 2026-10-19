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
    public List<BlazeDbPropModel> Props = new List<BlazeDbPropModel>();
    public List<BlazeDbCompoundIndexModel> CompoundIndexes = new List<BlazeDbCompoundIndexModel>();
}
