namespace RealEstateAdmin.Models
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        public int BienTotal { get; set; }
        public int BienPublies { get; set; }
        public int BienEnAttente { get; set; }
        public int BienVendus { get; set; }
        public List<UserBienItemViewModel> Biens { get; set; } = new();
    }

    public class UserBienItemViewModel
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public string TypeTransaction { get; set; } = string.Empty;
        public string PublicationStatus { get; set; } = string.Empty;
    }
}

