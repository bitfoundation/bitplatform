﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminPanel.Shared.Dtos.Dashboard;
public class ProductsCountPerCategoryDto
{
    public string CategoryName { get; set; }

    public string CategoryColor { get; set; }

    public int ProductCount { get; set; }


}
