using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var totalBiens = await _context.Biens.CountAsync();
            var totalUtilisateurs = await _context.Utilisateurs.CountAsync();
            var totalMessages = await _context.Messages.CountAsync();

            // Données pour le graphique de répartition des biens par prix
            var biens = await _context.Biens.ToListAsync();
            var biensParPrix = biens
                .GroupBy(b => b.Prix < 100000 ? "Moins de 100k€" :
                             b.Prix < 200000 ? "100k€ - 200k€" :
                             b.Prix < 300000 ? "200k€ - 300k€" :
                             b.Prix < 500000 ? "300k€ - 500k€" :
                             "Plus de 500k€")
                .Select(g => new { Categorie = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.TotalBiens = totalBiens;
            ViewBag.TotalUtilisateurs = totalUtilisateurs;
            ViewBag.TotalMessages = totalMessages;
            ViewBag.BiensParPrix = biensParPrix;

            return View();
        }
    }
}

