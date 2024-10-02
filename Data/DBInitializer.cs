using System;
using expense_classification.Models;
using Microsoft.AspNetCore.Identity;

namespace expense_classification.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure the database is created
        context.Database.EnsureCreated();

        // Check if roles exist
        if (!context.Roles.Any())
        {
            // Create roles
            await roleManager.CreateAsync(new IdentityRole("admin"));
            await roleManager.CreateAsync(new IdentityRole("member"));
        }

                // Check if admin user exists
        var adminUser = await userManager.FindByEmailAsync("aa@aa.aa");
        if (adminUser == null)
        {
            // Create admin user
            adminUser = new ApplicationUser { UserName = "aa@aa.aa", Email = "aa@aa.aa", IsApproved = true };
            await userManager.CreateAsync(adminUser, "P@$$w0rd");
            await userManager.AddToRoleAsync(adminUser, "admin");
            adminUser.EmailConfirmed = true;
            await userManager.UpdateAsync(adminUser);
        }
        else
        {
            // Ensure admin user has IsApproved = true
            if (!adminUser.IsApproved)
            {
                adminUser.IsApproved = true;
                await userManager.UpdateAsync(adminUser);
            }
        }

        // Check if member user exists
        var memberUser = await userManager.FindByEmailAsync("mm@mm.mm");
        if (memberUser == null)
        {
            // Create member user (unapproved by default)
            memberUser = new ApplicationUser { UserName = "mm@mm.mm", Email = "mm@mm.mm" };
            await userManager.CreateAsync(memberUser, "P@$$w0rd");
            await userManager.AddToRoleAsync(memberUser, "member");
            memberUser.EmailConfirmed = true;
            await userManager.UpdateAsync(memberUser);
        }

        // // Check if users exist
        // if (!context.Users.Any())
        // {
        //     // Create admin user
        //     var adminUser = new ApplicationUser { UserName = "aa@aa.aa", Email = "aa@aa.aa" };
        //     await userManager.CreateAsync(adminUser, "P@$$w0rd");
        //     await userManager.AddToRoleAsync(adminUser, "admin");
        //     // Set IsApproved and EmailConfirmed
        //     adminUser.IsApproved = true;
        //     adminUser.EmailConfirmed = true;
        //     await userManager.UpdateAsync(adminUser);

        //     // Create member user
        //     var memberUser = new ApplicationUser { UserName = "mm@mm.mm", Email = "mm@mm.mm" };
        //     await userManager.CreateAsync(memberUser, "P@$$w0rd");
        //     await userManager.AddToRoleAsync(memberUser, "member");
        //     memberUser.EmailConfirmed = true;
        //     await userManager.UpdateAsync(memberUser);
        // }
    }
}
