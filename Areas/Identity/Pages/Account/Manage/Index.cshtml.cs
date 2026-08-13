using JobCareerPlatform.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [StringLength(100)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username =
                await _userManager.GetUserNameAsync(user)
                ?? string.Empty;

            Email =
                await _userManager.GetEmailAsync(user)
                ?? string.Empty;

            UserRole =
                user.UserRole;

            Input = new InputModel
            {
                FullName = user.FullName
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound(
                    $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound(
                    $"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            user.FullName = Input.FullName;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                StatusMessage =
                    "Unexpected error when updating profile.";

                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);

            StatusMessage =
                "Your profile has been updated.";

            return RedirectToPage();
        }
    }
}