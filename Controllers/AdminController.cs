using expense_classification.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace expense_classification.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult UnapprovedUsers()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpPost]
        // Action to toggle user approval status
        public async Task<IActionResult> ToggleApproval(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApproved = !user.IsApproved; // Toggle the approval status
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("UnapprovedUsers");
        }
    }
}
