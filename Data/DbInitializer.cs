using Microsoft.AspNetCore.Identity;

namespace JobCareerPlatform.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles =
            {
                "JobSeeker",
                "Employer",
                "CareerAdvisor",
                "SystemAdmin"
            };

            // Create roles
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            // Get Admin details from User Secrets
            var adminEmail = configuration["SeedAdmin:Email"];
            var adminPassword = configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) ||
                string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Admin email or password is missing from User Secrets.");
            }

            var adminUser =
                await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,

                    FullName = "System Administrator",
                    UserRole = "SystemAdmin",
                    AccountStatus = "Active"
                };

                var result =
                    await userManager.CreateAsync(
                        adminUser,
                        adminPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Unable to create admin account: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(
                adminUser,
                "SystemAdmin"))
            {
                await userManager.AddToRoleAsync(
                    adminUser,
                    "SystemAdmin");
            }
        }
    }
}