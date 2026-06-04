//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Category
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Naam { get; set; } = string.Empty;
}