using System;

namespace RealEstateAdmin.Models
{
    public class SaleViewModel
    {
        public int SaleId { get; set; }
        public int? BienId { get; set; }
        public string BienTitre { get; set; } = string.Empty;
        public string? Adresse { get; set; }
        public decimal? Prix { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? AcheteurNom { get; set; }
        public string? AcheteurEmail { get; set; }
        public string? VendeurNom { get; set; }
        public string? VendeurEmail { get; set; }
        public DateTime DateVente { get; set; }
        public string? Details { get; set; }
    }
}
