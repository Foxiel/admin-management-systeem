using System.ComponentModel.DataAnnotations;

namespace KE03_INTDEV_SE_2_Base.Models
{
    public class LeverancierModel
    {
        [Key]
        public int Leverancier_id { get; set; }
        [Required]
        public string? Leverancier_naam { get; set; }
    }
}
