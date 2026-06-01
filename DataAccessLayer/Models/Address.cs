//Gemaakt door Tristan

namespace DataAccessLayer.Models;

public class Address
{
    public int AddressId { get; set; }
    public int CustomerId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string AddressType { get; set; } = string.Empty;
}