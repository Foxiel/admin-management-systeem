//Gemaakt door Tristan

namespace DataAccessLayer.Models;

public class OrderLine
{
    public int OrderId { get; set; }
    public string ProductEAN { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
}