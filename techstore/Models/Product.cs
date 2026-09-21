using System.ComponentModel.DataAnnotations;

namespace techstore.Models;

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(60)]
    [Display(Name = "Category")]
    public string Type { get; set; } = string.Empty;
    [Range(0.01, 100000000, ErrorMessage = "Enter a price between 0.01 and 100,000,000.")]
    public double Price { get; set; }
}
