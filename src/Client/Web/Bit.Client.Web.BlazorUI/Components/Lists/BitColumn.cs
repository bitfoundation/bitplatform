using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    //TODO: columnActionsMode and other props
    public class BitColumn
    {
        /// <summary>
        /// A unique key for identifying the column.
        /// </summary>
        public string Key { get; set; } = "col1";

        /// <summary>
        /// Name to render on the column header.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The field to pull the text value from for the column.
        /// Can be unset if a custom `onRender` method is provided.
        /// </summary>
        public string? FieldName { get; set; }


        /// <summary>
        /// If specified, the width of the column is a portion of the available space equal to this value divided by the sum
        /// of all proportional column widths in the list.For example, if there is a list with two proportional columns that
        /// have widths of 1 and 3, they will respectively occupy (1/4) = 25% and(3/4) = 75% of the remaining space.Note that
        /// this relies on viewport measures and will not work well with skipViewportMeasures.
        /// </summary>
        public int? FlexGrow { get; set; }

        /// <summary>
        /// Class name to apply to the column cell within each row.
        /// </summary>
        public string? ClassName { get; set; }

        /// <summary>
        /// Class name for the icon within the header.
        /// </summary>
        public string? IconClassName { get; set; }


        /// <summary>
        /// Accessible label for the column. The column name will still be used as the primary label,
        /// but this text (if specified) will be used as the column description.
        /// WARNING: grid column descriptions are often ignored by screen readers, so any necessary information
        /// should go directly in the column content
        /// </summary>
        public string? AriaLabel { get; set; }

        /// <summary>
        /// Custom icon to use in the column header.
        /// </summary>
        public string? IconName { get; set; }

        /// <summary>
        /// Minimum width for the column.
        /// </summary>
        public int MinWidth { get; set; }

        /// <summary>
        /// Maximum width for the column, if stretching is allowed in justified scenarios.
        /// </summary>
        public int? MaxWidth { get; set; }


        /// <summary>
        /// Internal only value. 
        /// </summary>
        public int? CalculatedWidth
        {
            get
            {
                if (IsIconOnly is true)
                    return 36;
                if (IsRowHeader is true)
                    return 286;
                return 140;
            }
        }


        /// <summary>
        /// If specified, the width of the column is a portion of the available space equal to this value divided by the sum
        /// of all proportional column widths in the list.For example, if there is a list with two proportional columns that
        /// have widths of 1 and 3, they will respectively occupy (1/4) = 25% and(2/4) = 75% of the remaining space.Note that
        /// this relies on viewport measures and will not work well with skipViewportMeasures.
        /// </summary>
        public int? TargetWidthProportion { get; set; }

        /// <summary>
        /// If true, allow the column to be collapsed when rendered in justified layout.
        /// </summary>
        public bool? IsCollapsible { get; set; }

        /// <summary>
        /// Determines if the column can render multi-line text.
        /// </summary>
        public bool? IsMultiline { get; set; }

        /// <summary>
        /// Whether the list is filtered by this column. If true, shows a filter icon next to this column's name. 
        /// </summary>
        public bool? IsFiltered { get; set; }

        /// <summary>
        /// Whether the list is grouped by this column. If true, shows a grouped icon next to this column's name.
        /// </summary>
        public bool? IsGrouped { get; set; }

        /// <summary>
        /// If true, add additional LTR padding-right to column and cells.
        /// </summary>
        public bool IsPadded { get; set; }

        /// <summary>
        /// Whether only the icon should be displayed in the column header.
        /// If true, the column name and dropdown chevron will not be displayed.
        /// </summary>
        public bool? IsIconOnly { get; set; }

        /// <summary>
        /// Whether the column is a header for the given row. There should be only one column with this set to true.
        /// </summary>
        public bool? IsRowHeader { get; set; }

        /// <summary>
        /// Determines if the column can be resized.
        /// </summary>
        public bool? IsResizable { get; set; }

        /// <summary>
        /// Whether a dropdown menu is open so that the appropriate ARIA attributes are rendered.
        /// </summary>
        public bool? IsMenuOpen { get; set; }

        /// <summary>
        /// Determines if the column is currently sorted. Renders a sort arrow in the column header.
        /// </summary>
        public bool? IsSorted { get; set; }

        /// <summary>
        /// Determines if the sort arrow is pointed down (descending) or up.
        /// </summary>
        public bool IsSortedDescending { get; set; }

        /// <summary>
        /// Accessible label for indicating that the list is sorted by this column in ascending order.
        /// This will be read after the main column header label.
        /// </summary>
        public string? SortAscendingAriaLabel { get; set; }

        /// <summary>
        /// Accessible label for the status of this column when grouped.
        /// </summary>
        public string? GroupAriaLabel { get; set; }

        /// <summary>
        /// Accessible label for the status of this column when filtered.
        /// </summary>
        public string? FilterAriaLabel { get; set; }

        /// <summary>
        /// Accessible label for indicating that the list is sorted by this column in descending order.
        /// This will be read after the main column header label.
        /// </summary>
        public string? SortDescendingAriaLabel { get; set; }

        /// <summary>
        /// Column data type
        /// string
        /// int
        /// bool
        /// </summary>
        public string? DataType { get; set; }


        /// <summary>
        /// Custom renderer for cell content, instead of the default text rendering.
        /// </summary>
        public RenderFragment<BitDocument>? OnRender { get; set; }
    }
}
