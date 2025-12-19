using System.ComponentModel.DataAnnotations;

namespace RealEstateAdmin.Models
{
    public class Utilisateur
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [Display(Name = "Nom")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        [StringLength(200, ErrorMessage = "L'email ne peut pas dépasser 200 caractères")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [Display(Name = "Mot de passe")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        public string MotDePasse { get; set; } = string.Empty;
    }
}

