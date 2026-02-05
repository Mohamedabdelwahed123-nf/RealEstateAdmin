using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateAdmin.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        public int BienId { get; set; }

        [ForeignKey("BienId")]
        public BienImmobilier? Bien { get; set; }

        [Required]
        [StringLength(450)]
        public string BuyerId { get; set; } = string.Empty;

        [ForeignKey("BuyerId")]
        public ApplicationUser? Buyer { get; set; }

        [StringLength(450)]
        public string? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public ApplicationUser? Seller { get; set; }

        [Required]
        public decimal Prix { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Validée";

        [StringLength(50)]
        public string? PaymentMethod { get; set; } = "Non défini";

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "En attente";

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
