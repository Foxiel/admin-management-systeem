//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Account
{
    [Key]
    public int Id { get; set; }
    public Customer Klant { get; set; }
    public string Gebruikersnaam { get; set; } = string.Empty;
    public string WachtwoordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}