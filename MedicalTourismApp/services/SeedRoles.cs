using Microsoft.AspNetCore.Identity;

public static class SeedRoles 
{
    public static async Task Initialize(RoleManager<IdentityRole> roleManager)
    {
        // Define the roles you want to create
        string[] roleNames = { "Admin", "Patient", "ServiceProvider","Doctor","Clinic","Guide","Hospital","Freelance","Transportation","Hotel","Apartment"};

        foreach (var roleName in roleNames)
        {
            // Check if the role already exists
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                // Create the role if it doesn't exist
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}