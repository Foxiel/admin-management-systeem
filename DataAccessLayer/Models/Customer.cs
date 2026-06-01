//Gemaakt door Tristan

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Customer
{
    [Key]
    [Required]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    [Required]
    public string Email { get; set; }
    public string? Telefoonnr { get; set; }
}