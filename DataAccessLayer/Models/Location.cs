//Gemaakt door Fabian

namespace DataAccessLayer.Models;

public class Location
{
    public int Id { get; set; }
    public string Gang { get; set; } = string.Empty;
    public string Schap { get; set; } = string.Empty;
    public string Vak { get; set; } = string.Empty;
}