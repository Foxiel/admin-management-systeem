//Gemaakt door Fabian

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class ProductStock
{
    [Key]
    public int EAN { get; set; }
    public Product Product { get; set; }
    public int Huidige_Voorraad { get; set; }
    public int Minimum_Voorraad { get; set; }
    public string Status { get; set; }
}