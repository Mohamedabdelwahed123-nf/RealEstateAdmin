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

            // Données pour le graphique de répartition des biens par prix (tranches dynamiques)
            var prixList = await biensQuery.Select(b => b.Prix).ToListAsync();
            var biensParPrix = BuildPriceBuckets(prixList);

            // Statistiques par zone
            var zoneSource = await biensQuery
                .Select(b => new { b.Prix, b.Surface, b.Adresse })
                .ToListAsync();

            var zoneStats = zoneSource
                .GroupBy(b => ExtractZone(b.Adresse))
                .Select(g => new
                {
                    Zone = g.Key,
                    Count = g.Count(),
                    AvgPrice = g.Average(x => x.Prix),
                    AvgPricePerM2 = g.Where(x => x.Surface.HasValue && x.Surface.Value > 0)
                        .Select(x => x.Prix / x.Surface!.Value)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .OrderByDescending(z => z.Count)
                .ToList();

            var topZone = zoneStats.FirstOrDefault();

            ViewBag.TotalBiens = totalBiens;
            ViewBag.TotalUtilisateurs = totalUtilisateurs;
            ViewBag.TotalMessages = totalMessages;
            ViewBag.BiensParPrix = biensParPrix;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.ZoneStats = zoneStats;
            ViewBag.TopZone = topZone?.Zone ?? "-";
            ViewBag.AvgPrice = prixList.Count > 0 ? $"{FormatMoney(prixList.Average())} DT" : "-";
            ViewBag.AvgPricePerM2 = zoneStats.Any() && zoneStats.Any(z => z.AvgPricePerM2 > 0)
                ? $"{FormatMoney((decimal)zoneStats.Where(z => z.AvgPricePerM2 > 0).Average(z => z.AvgPricePerM2))} DT/m²"
                : "-";
            ViewBag.ZoneCount = zoneStats.Count;

            return View();
        }

        private static List<object> BuildPriceBuckets(List<decimal> prices)
        {
            var buckets = new List<object>();
            if (prices == null || prices.Count == 0)
            {
                return buckets;
            }

            prices.Sort();
            var min = prices.First();
            var max = prices.Last();
            if (min == max)
            {
                buckets.Add(new { Categorie = $"{FormatMoney(min)} DT", Count = prices.Count });
                return buckets;
            }

            var binCount = Math.Clamp((int)Math.Ceiling(Math.Sqrt(prices.Count)), 3, 6);
            var step = NiceStep(max - min, binCount);
            if (step <= 0)
            {
                step = 1;
            }

            var minEdge = Math.Floor(min / step) * step;
            var maxEdge = Math.Ceiling(max / step) * step;
            if (maxEdge == minEdge)
            {
                maxEdge = minEdge + step;
            }

            var binTotal = (int)Math.Ceiling((maxEdge - minEdge) / step);
            var counts = new int[binTotal];

            foreach (var price in prices)
            {
                var idx = (int)Math.Floor((price - minEdge) / step);
                if (idx < 0) idx = 0;
                if (idx >= binTotal) idx = binTotal - 1;
                counts[idx]++;
            }

            for (var i = 0; i < binTotal; i++)
            {
                var start = minEdge + (i * step);
                var end = start + step;
                var label = $"{FormatMoney(start)} - {FormatMoney(end)} DT";
                buckets.Add(new { Categorie = label, Count = counts[i] });
            }

            return buckets;
        }

        private static decimal NiceStep(decimal range, int bins)
        {
            if (range <= 0 || bins <= 0)
            {
                return 1;
            }

            var raw = (double)(range / bins);
            var exponent = Math.Floor(Math.Log10(raw));
            var power = Math.Pow(10, exponent);
            var fraction = raw / power;
            var niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
            return (decimal)(niceFraction * power);
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("N0");
        }

        private static readonly HashSet<string> KnownZones = new(StringComparer.OrdinalIgnoreCase)
        {
            "Tunis",
            "Ariana",
            "La Marsa",
            "Sousse",
            "Monastir",
            "Sfax",
            "Hammamet",
            "Bizerte",
            "Nabeul",
            "Gabès",
            "Gafsa",
            "Kairouan",
            "Tozeur",
            "Mahdia",
            "Kasserine",
            "Sidi Bouzid",
            "Jendouba",
            "El Kef",
            "Médenine",
            "Djerba",
            "Zaghouan",
            "Béja",
            "Siliana",
            "Tatouine",
            "Manouba",
            "Ben Arous"
        };

        private static string ExtractZone(string? adresse)
        {
            if (string.IsNullOrWhiteSpace(adresse))
            {
                return "Autre";
            }

            var parts = adresse.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = parts.Length - 1; i >= 0; i--)
            {
                var part = parts[i];
                if (KnownZones.Contains(part))
                {
                    return part;
                }
            }

            var fallback = parts.Length > 0 ? parts[^1] : adresse;
            return string.IsNullOrWhiteSpace(fallback) ? "Autre" : fallback;
        }

    }
}

