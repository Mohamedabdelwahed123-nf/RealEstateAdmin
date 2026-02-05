using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UtilisateurController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UtilisateurController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Utilisateur
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserRoleViewModel>();

            var bienStats = await _context.Biens
                .Where(b => b.UserId != null)
                .GroupBy(b => b.UserId!)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Count(),
                    Publies = g.Count(b => b.PublicationStatus == "Publié"),
                    EnAttente = g.Count(b => b.PublicationStatus == "En attente"),
                    Vendus = g.Count(b => b.TypeTransaction == "Acheté")
                })
                .ToListAsync();

            var statsByUser = bienStats.ToDictionary(x => x.UserId, x => x);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                statsByUser.TryGetValue(user.Id, out var stats);
                userList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.Nom ?? user.UserName ?? "",
                    Email = user.Email ?? "",
                    CurrentRole = roles.FirstOrDefault() ?? "Aucun",
                    BienTotal = stats?.Total ?? 0,
                    BienPublies = stats?.Publies ?? 0,
                    BienEnAttente = stats?.EnAttente ?? 0,
                    BienVendus = stats?.Vendus ?? 0
                });
            }

            return View(userList);
        }

        // GET: Utilisateur/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var bienStats = await _context.Biens
                .Where(b => b.UserId == user.Id)
                .GroupBy(b => b.UserId!)
                .Select(g => new
                {
                    Total = g.Count(),
                    Publies = g.Count(b => b.PublicationStatus == "Publié"),
                    EnAttente = g.Count(b => b.PublicationStatus == "En attente"),
                    Vendus = g.Count(b => b.TypeTransaction == "Acheté")
                })
                .FirstOrDefaultAsync();

            var biens = await _context.Biens
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.Id)
                .Take(10)
                .ToListAsync();

            var userViewModel = new UserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.Nom ?? user.UserName ?? "",
                Email = user.Email ?? "",
                CurrentRole = roles.FirstOrDefault() ?? "Aucun",
                BienTotal = bienStats?.Total ?? 0,
                BienPublies = bienStats?.Publies ?? 0,
                BienEnAttente = bienStats?.EnAttente ?? 0,
                BienVendus = bienStats?.Vendus ?? 0,
                Biens = biens.Select(b => new UserBienItemViewModel
                {
                    Id = b.Id,
                    Titre = b.Titre,
                    Prix = b.Prix,
                    TypeTransaction = b.TypeTransaction,
                    PublicationStatus = b.PublicationStatus
                }).ToList()
            };

            return View(userViewModel);
        }
    }
}

