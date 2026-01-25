using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using RealEstateAdmin.Models;

namespace RealEstateAdmin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UtilisateurController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UtilisateurController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Utilisateur
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.Nom ?? user.UserName ?? "",
                    Email = user.Email ?? "",
                    CurrentRole = roles.FirstOrDefault() ?? "Aucun"
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
            var userViewModel = new UserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.Nom ?? user.UserName ?? "",
                Email = user.Email ?? "",
                CurrentRole = roles.FirstOrDefault() ?? "Aucun"
            };

            return View(userViewModel);
        }
    }
}

