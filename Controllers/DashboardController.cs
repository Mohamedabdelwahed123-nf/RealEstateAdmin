using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            // Filtrer les données par utilisateur si ce n'est pas un admin
            var biensQuery = _context.Biens.AsQueryable();
            if (!isAdmin && currentUser != null)
            {
                biensQuery = biensQuery.Where(b => b.UserId == currentUser.Id);
            }

            var totalBiens = await biensQuery.CountAsync();
            var totalUtilisateurs = isAdmin ? await _userManager.Users.CountAsync() : 0;
            var totalMessages = isAdmin ? await _context.Messages.CountAsync() : 0;

            // Données pour le graphique de répartition des biens par prix
            var biens = await biensQuery.ToListAsync();
            var biensParPrix = biens
                .GroupBy(b => b.Prix < 100000 ? "Moins de 100k DT" :
                             b.Prix < 200000 ? "100k DT - 200k DT" :
                             b.Prix < 300000 ? "200k DT - 300k DT" :
                             b.Prix < 500000 ? "300k DT - 500k DT" :
                             "Plus de 500k DT")
                .Select(g => new { Categorie = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.TotalBiens = totalBiens;
            ViewBag.TotalUtilisateurs = totalUtilisateurs;
            ViewBag.TotalMessages = totalMessages;
            ViewBag.BiensParPrix = biensParPrix;
            ViewBag.IsAdmin = isAdmin;

            return View();
        }
    }
}

