using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;
using System.Globalization;

namespace RealEstateAdmin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, string? buyerId, string? sellerId)
        {
            var query = _context.Sales
                .Include(s => s.Bien)
                .Include(s => s.Buyer)
                .Include(s => s.Seller)
                .AsQueryable();

            if (dateFrom.HasValue)
            {
                query = query.Where(s => s.CreatedAt >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.CreatedAt <= end);
            }

            if (!string.IsNullOrWhiteSpace(buyerId))
            {
                query = query.Where(s => s.BuyerId == buyerId);
            }

            if (!string.IsNullOrWhiteSpace(sellerId))
            {
                query = query.Where(s => s.SellerId == sellerId);
            }

            var sales = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var viewModel = sales.Select(s => new SaleViewModel
            {
                SaleId = s.Id,
                BienId = s.BienId,
                BienTitre = s.Bien?.Titre ?? "(Bien supprimé)",
                Adresse = s.Bien?.Adresse,
                Prix = s.Prix,
                Status = s.Status,
                PaymentMethod = s.PaymentMethod,
                PaymentStatus = s.PaymentStatus,
                PaidAt = s.PaidAt,
                AcheteurNom = s.Buyer?.Nom ?? s.Buyer?.UserName,
                AcheteurEmail = s.Buyer?.Email,
                VendeurNom = s.Seller?.Nom ?? s.Seller?.UserName,
                VendeurEmail = s.Seller?.Email,
                DateVente = s.CreatedAt,
                Details = null
            }).ToList();

            var buyerIds = await _context.Sales
                .Select(s => s.BuyerId)
                .Distinct()
                .ToListAsync();

            var sellerIds = await _context.Sales
                .Where(s => s.SellerId != null)
                .Select(s => s.SellerId!)
                .Distinct()
                .ToListAsync();

            var userIds = buyerIds.Concat(sellerIds).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .OrderBy(u => u.Nom ?? u.UserName)
                .ToListAsync();

            ViewBag.Buyers = users.Where(u => buyerIds.Contains(u.Id)).ToList();
            ViewBag.Sellers = users.Where(u => sellerIds.Contains(u.Id)).ToList();
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.BuyerId = buyerId;
            ViewBag.SellerId = sellerId;

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackfillFromAuditLogs()
        {
            var logs = await _context.AuditLogs
                .Where(l => l.Action == "Buy" && l.EntityType == "BienImmobilier")
                .OrderBy(l => l.CreatedAt)
                .ToListAsync();

            var bienIds = logs.Where(l => l.EntityId.HasValue).Select(l => l.EntityId!.Value).Distinct().ToList();
            var biens = await _context.Biens.Where(b => bienIds.Contains(b.Id)).ToListAsync();
            var bienById = biens.ToDictionary(b => b.Id, b => b);

            var created = 0;
            foreach (var log in logs)
            {
                if (log.EntityId == null || string.IsNullOrWhiteSpace(log.UserId))
                {
                    continue;
                }

                var exists = await _context.Sales.AnyAsync(s =>
                    s.BienId == log.EntityId.Value &&
                    s.BuyerId == log.UserId &&
                    s.CreatedAt == log.CreatedAt);

                if (exists)
                {
                    continue;
                }

                var sellerId = ParseDetailValue(log.Details, "VendeurId");
                var prix = ParseDetailDecimal(log.Details, "Prix");
                if (!prix.HasValue && bienById.TryGetValue(log.EntityId.Value, out var bien))
                {
                    prix = bien.Prix;
                }

                _context.Sales.Add(new Sale
                {
                    BienId = log.EntityId.Value,
                    BuyerId = log.UserId,
                    SellerId = string.IsNullOrWhiteSpace(sellerId) ? null : sellerId,
                    Prix = prix ?? 0,
                    Status = "Validée",
                    PaymentMethod = "Non défini",
                    PaymentStatus = "En attente",
                    CreatedAt = log.CreatedAt
                });
                created++;
            }

            if (created > 0)
            {
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Backfill terminé. {created} vente(s) ajoutée(s).";
            return RedirectToAction(nameof(Index));
        }

        private static string? ParseDetailValue(string? details, string key)
        {
            if (string.IsNullOrWhiteSpace(details))
            {
                return null;
            }

            var token = key + ":";
            var index = details.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return null;
            }

            var start = index + token.Length;
            var end = details.IndexOf('|', start);
            var value = end >= 0 ? details.Substring(start, end - start) : details.Substring(start);
            value = value.Trim();

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static decimal? ParseDetailDecimal(string? details, string key)
        {
            var raw = ParseDetailValue(details, key);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }

            return null;
        }
    }
}
