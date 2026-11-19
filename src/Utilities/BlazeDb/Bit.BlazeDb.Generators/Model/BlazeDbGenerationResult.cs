using System.Collections.Generic;

namespace Bit.BlazeDb;

internal sealed class BlazeDbGenerationResult
{
    public BlazeDbTableModel? Model;
    public List<BlazeDbDiagnosticInfo> Diagnostics = new List<BlazeDbDiagnosticInfo>();
}
