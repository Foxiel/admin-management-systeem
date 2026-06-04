//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Review
{
    [Key]
    public int Id { get; set; }
    public Customer Klant { get; set; }
    public Product Product { get; set; }
    public int Beoordeling { get; set; }
    public string Beschrijving { get; set; }
}