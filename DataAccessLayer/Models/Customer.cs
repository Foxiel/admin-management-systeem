//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }
    
    public string Naam { get; set; } = string.Empty;
    
    public string Email { get; set; }
    public string? Telefoonnr { get; set; }
}