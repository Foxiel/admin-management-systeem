using System.ComponentModel.DataAnnotations;

namespace KE03_INTDEV_SE_2_Base.Models
{
    public class OrderModel
    {
        [Key]
        public int OrderId { get; set; }

        public DateTime OrderDatum { get; set; }

        public string? OrderStatus { get; set; }

        public string? BetaalStatus { get; set; }

        public decimal Verzendkosten { get; set; }

        public decimal TotaalBedrag { get; set; }

        public int KlantId { get; set; }
        //kopeltabel
        //public int OrderId { get; set; }

        public string ProductId { get; set; } = string.Empty;

        public int Aantal { get; set; }
    }
}
