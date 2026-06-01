//Gemaakt door Tristan

namespace DataAccessLayer.Models;

public class ProductImage
{
    public int ImageId { get; set; }
    public string ProductEAN { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMainImage { get; set; }
}