//Gemaakt door Fabian

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class Log
{
    [Key]
    public int  Id { get; set; }
    public Account Account { get; set; }
    public string Actie { get; set; }
    public DateTime Datum { get; set; }
    public string Omschrijving { get; set; }
}