//Gemaakt door Tristan
//Aangepast door Fabian, velden aangepast en vertaald

using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

public class ProductImage
{
    [Key]
    public int ImageId { get; set; }
    public Product Product { get; set; }
    public string Pad { get; set; } = string.Empty;
    public int Volgorde { get; set; }
}