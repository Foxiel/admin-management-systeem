//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    public Customer Klant { get; set; }
    public List<Product> Producten { get; set; }
    public DateTime BestelDatum { get; set; }
    public string BestelStatus { get; set; }
}
