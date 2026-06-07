using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        // aantal per regel
        public int Aantal { get; set; }

        // gekoppeld Product (gevuld door repository)
        public Product? Product { get; set; }
    }
}
