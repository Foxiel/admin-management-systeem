//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Product
{
    [Key]
    public string EAN { get; set; } = string.Empty;
    public Manufacturer Leverancier { get; set; }
    public Location Locatie { get; set; }
    public string Naam { get; set; }
    public string Beschrijving { get; set; }
    public decimal Prijs { get; set; }
    public decimal Gewicht { get; set; }
    public string Garantie { get; set; }
}