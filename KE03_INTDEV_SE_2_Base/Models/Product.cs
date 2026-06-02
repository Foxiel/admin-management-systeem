using DataAccessLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace KE03_INTDEV_SE_2_Base.Models
{
    public class Product
    {
        public string ProductId { get; set; } = string.Empty;

        public string? ProductEAN { get; set; }

        [Required]
        public string ProductNaam { get; set; } = string.Empty;

        public string? ProductBeschrijving { get; set; }

        public string? ProductSpecificatie { get; set; }

        public decimal ProductPrijs { get; set; }

        public string? ProductGewicht { get; set; }

        public string? ProductGarantie { get; set; }

        public DateTime? ProductReleaseDatum { get; set; }

        public string? ProductAfbeelding { get; set; }

        public string? LeverancierId { get; set; }

        public string? CategorieId { get; set; }

        public string? ProductLocatie { get; set; }


    }
}
