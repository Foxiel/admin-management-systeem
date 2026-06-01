//Gemaakt door Tristan

namespace DataAccessLayer.Models;

public class Review
{
    public int ReviewId { get; set; }
    public int CustomerId { get; set; }
    public string ProductEAN { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
}