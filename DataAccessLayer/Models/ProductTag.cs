//Gemaakt door Tristan

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class ProductTag
{
    [Key]
    public int Id { get; set; }
    public string Naam { get; set; }
    public Product Product { get; set; }
}