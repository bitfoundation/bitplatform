using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.OData.Query
{
    public static class ODataQueryOptionsExtensions
    {
        public static string[] GetExpandedProperties(this ODataQueryOptions oDataQueryOptions)
        {
            if (oDataQueryOptions == null)
                throw new ArgumentNullException(nameof(oDataQueryOptions));

            static void GetEntirePath(ExpandedNavigationSelectItem expandItem, ref List<ExpandedNavigationSelectItem> expandItems)
            {
                expandItems.Add(expandItem);

                var nestedExpandItems = expandItem.SelectAndExpand?.SelectedItems?.OfType<ExpandedNavigationSelectItem>()?.ToList();

                if (nestedExpandItems != null)
                {
                    foreach (var nestedExpandItem in nestedExpandItems)
                    {
                        GetEntirePath(nestedExpandItem, ref expandItems);
                    }
                }
            }

            var finalResult = new List<string> { };

            foreach (ExpandedNavigationSelectItem expandItem in oDataQueryOptions.SelectExpand?.SelectExpandClause
                ?.SelectedItems?.OfType<ExpandedNavigationSelectItem>() ?? new ExpandedNavigationSelectItem[] { })
            {
                var expandItems = new List<ExpandedNavigationSelectItem>();
                GetEntirePath(expandItem, ref expandItems);
                var expandItemNames = expandItems.SelectMany(i => i.PathToNavigationProperty.Select(i => i.Identifier)).ToArray();
                var result = string.Join(".", expandItemNames);
                finalResult.Add(result);
            }

            return finalResult.ToArray();
        }
    }
}
