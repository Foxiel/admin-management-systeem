//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald
//Aanpasstingen door Tristan, meer velden toegevoegd

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Product
{
    [Key]
    public int ProductId { get; set; }
    public string EAN { get; set; } = string.Empty;
    public int LeverancierId { get; set; }
    public int LocatieId { get; set; }
    public Manufacturer? Leverancier { get; set; }
    public Location? Locatie { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string Beschrijving { get; set; } = string.Empty;
    public decimal Prijs { get; set; }
    public decimal Gewicht { get; set; }
    public string Garantie { get; set; } = string.Empty;
    public int HuidigeVoorraad { get; set; }
    public int MinimumVoorraad { get; set; }
    public string Status { get; set; } = string.Empty;
}