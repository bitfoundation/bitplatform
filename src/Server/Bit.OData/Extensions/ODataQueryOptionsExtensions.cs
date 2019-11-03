using Microsoft.OData.UriParser;
using System;
using System.Linq;

namespace Microsoft.AspNet.OData.Query
{
    public static class ODataQueryOptionsExtensions
    {
        public static string[] GetExpandedProperties(this ODataQueryOptions oDataQueryOptions)
        {
            return oDataQueryOptions.SelectExpand?.SelectExpandClause
                ?.SelectedItems?.OfType<ExpandedReferenceSelectItem>()?.SelectMany(i => i.PathToNavigationProperty)?.Select(i => i.Identifier)?.ToArray() ?? Array.Empty<string>();
        }
    }
}
