//Gemaakt door Tristan

namespace DataAccessLayer.Models;

public class Product
{
    public string ProductEAN { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string? ProductSpecification { get; set; }
    public decimal ProductPrice { get; set; }
    public int ProductStock { get; set; }
    public decimal ProductWeightKg { get; set; }
    public int ProductWarrantyMonths { get; set; }
    public DateTime ProductReleaseDate { get; set; }
    public string ManufacturerId { get; set; } = string.Empty;
    public string SubcategoryId { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }


}