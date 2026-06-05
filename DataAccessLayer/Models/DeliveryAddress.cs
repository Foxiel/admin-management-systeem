//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class DeliveryAddress
{
    [Key]
    public int Id { get; set; }
    public Customer Klant { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string Woonplaats { get; set; } = string.Empty;
    public string Land { get; set; } = string.Empty;
}