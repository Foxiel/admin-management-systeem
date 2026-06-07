//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald
//verder aangepast door Jesse

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Naam { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; }
    [Required]
    public string Telefoonnr { get; set; }
    [Required]
    public string Adres { get; set; }
    [Required]
    public string Postcode { get; set; }
    [Required]
    public string Woonplaats { get; set; }
    [Required]
    public string Land { get; set; }
    
    public int AantalBestellingen { get; set; }
}