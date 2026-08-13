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
                serviceProvider.GetRequiredService<
                    RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<
                    UserManager<ApplicationUser>>();


            // =========================================
            // CREATE SYSTEM ROLES
            // =========================================
            string[] roles =
            {
                "JobSeeker",
                "Employer",
                "CareerAdvisor",
                "SystemAdmin"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult =
                        await roleManager.CreateAsync(
                            new IdentityRole(role));

                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(
                            ", ",
                            roleResult.Errors
                                .Select(e => e.Description));

                        throw new InvalidOperationException(
                            $"Unable to create role '{role}': {errors}");
                    }
                }
            }


            // =========================================
            // CHECK IF A SYSTEM ADMIN ALREADY EXISTS
            // =========================================
            var existingAdmins =
                await userManager.GetUsersInRoleAsync(
                    "SystemAdmin");

            if (existingAdmins.Any())
            {
                // A SystemAdmin already exists in the database.
                // SeedAdmin configuration is no longer required.
                return;
            }


            // =========================================
            // FIRST-TIME SYSTEM ADMIN SETUP ONLY
            // =========================================
            var adminEmail =
                configuration["SeedAdmin:Email"];

            var adminPassword =
                configuration["SeedAdmin:Password"];


            // SeedAdmin is only required when the
            // database has no SystemAdmin at all.
            if (string.IsNullOrWhiteSpace(adminEmail) ||
                string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "No SystemAdmin account exists in the database. " +
                    "SeedAdmin email and password are required only " +
                    "for the first-time system setup.");
            }


            // =========================================
            // FIND OR CREATE FIRST ADMIN
            // =========================================
            var adminUser =
                await userManager.FindByEmailAsync(
                    adminEmail);


            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,

                    FullName =
                        "System Administrator",

                    UserRole =
                        "SystemAdmin",

                    AccountStatus =
                        "Active",

                    CreatedAt =
                        DateTime.UtcNow
                };


                var createResult =
                    await userManager.CreateAsync(
                        adminUser,
                        adminPassword);


                if (!createResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        createResult.Errors
                            .Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Unable to create initial " +
                        $"SystemAdmin account: {errors}");
                }
            }
            else
            {
                // Account already exists but was not
                // previously configured as SystemAdmin.
                adminUser.UserRole =
                    "SystemAdmin";

                adminUser.AccountStatus =
                    "Active";

                adminUser.EmailConfirmed =
                    true;


                var updateResult =
                    await userManager.UpdateAsync(
                        adminUser);


                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        updateResult.Errors
                            .Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Unable to update initial " +
                        $"SystemAdmin account: {errors}");
                }
            }


            // =========================================
            // ADD IDENTITY SYSTEMADMIN ROLE
            // =========================================
            if (!await userManager.IsInRoleAsync(
                adminUser,
                "SystemAdmin"))
            {
                var roleResult =
                    await userManager.AddToRoleAsync(
                        adminUser,
                        "SystemAdmin");


                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        roleResult.Errors
                            .Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Unable to assign SystemAdmin " +
                        $"role: {errors}");
                }
            }
        }
    }
}
