﻿using System.Diagnostics.CodeAnalysis;
using AdminPanel.Api.Models.Categories;

namespace AdminPanel.Api.Models.Products;

[Table("Products")]
public class Product
{
    public Product()
    {
        this.CreateDate = DateTime.Now;
    }

    public int Id { get; set; }

    [MaxLength(64)]
    public string Name { get; set; }

    [Column(TypeName = "money")]
    [Range(0, Double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(512)]
    public string? Desc { get; set; }

    [NotNull]
    [Column(TypeName ="Date")]
    public DateTime CreateDate { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; }

    public int CategoryId { get; set; }


}
