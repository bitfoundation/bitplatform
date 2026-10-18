using System.Collections.Generic;

namespace Bit.BlazeDb.Generators;

internal sealed class BlazeDbGenerationResult
{
    public BlazeDbTableModel? Model;
    public List<BlazeDbDiagnosticInfo> Diagnostics = new List<BlazeDbDiagnosticInfo>();
}
