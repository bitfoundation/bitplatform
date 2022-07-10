using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminPanelTemplate.Shared.Dtos.Dashboard;
public class ProductPercentagePerCategoryDto
{
    public string CategoryName { get; set; }

    public string CategoryColor { get; set; }

    public float ProductPercentage { get; set; }
}
