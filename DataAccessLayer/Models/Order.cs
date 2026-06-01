//Gemaakt door Tristan

namespace DataAccessLayer.Models;

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public int OrderStatusId { get; set; }
    public int PaymentStatusId { get; set; }
    public decimal ShippingCosts { get; set; }
    public int CustomerId { get; set; }
    public int ShippingAddressId { get; set; }
}
